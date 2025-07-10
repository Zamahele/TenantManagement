using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PropertyManagement.Domain.Entities;
using PropertyManagement.Infrastructure.Data;
using PropertyManagement.Web.Controllers;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using System.Linq;
using System.Threading.Tasks;
using PropertyManagement.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;

[Authorize]
public class TenantsController : BaseController
{
  private readonly ApplicationDbContext _context;

  public TenantsController(ApplicationDbContext context)
  {
    _context = context;
  }

  // GET: /Tenants
  [Authorize(Roles = "Manager")]
  public async Task<IActionResult> Index()
  {
    ViewBag.Rooms = await _context.Rooms.ToListAsync();
    var tenants = await _context.Tenants.Include(t => t.Room).ToListAsync();
    return View(tenants);
  }

  // GET: /Tenants/Profile
  public async Task<IActionResult> Profile(int? id)
  {
    var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
    var role = User.FindFirstValue(ClaimTypes.Role);

    int tenantId;
    if (role == "Manager" && id.HasValue)
    {
      tenantId = id.Value;
    }
    else
    {
      // Find tenant by UserId
      var tenant = await _context.Tenants.FirstOrDefaultAsync(t => t.UserId == userId);
      if (tenant == null)
      {
        SetErrorMessage("Tenant not found.");
        return RedirectToAction("Login");
      }
      tenantId = tenant.TenantId;
    }

    var profile = await _context.Tenants
        .Include(t => t.Room)
        .Include(t => t.User)
        .Include(t => t.Payments)
        .Include(t => t.LeaseAgreements)    
        .ThenInclude(l => l.Room)
        .FirstOrDefaultAsync(t => t.TenantId == tenantId);

    if (profile == null)
    {
      SetErrorMessage("Tenant not found.");
      return RedirectToAction("Index");
    }

    return View("Profile", profile);
  }

  // GET: /Tenants/EditProfile
  public async Task<IActionResult> EditProfile(int? id)
  {
    var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
    var role = User.FindFirstValue(ClaimTypes.Role);

    Tenant tenant;
    if (role == "Manager" && id.HasValue)
    {
      tenant = await _context.Tenants.FindAsync(id.Value);
    }
    else
    {
      tenant = await _context.Tenants.Include(t =>t.User).FirstOrDefaultAsync(t => t.UserId == userId);
    }

    ViewBag.Rooms = await _context.Rooms.ToListAsync();
    if (tenant == null)
    {
      SetErrorMessage("Tenant not found.");
      return RedirectToAction(nameof(Profile));
    }
    return PartialView("_EditProfileModal", tenant);
  }

  // POST: /Tenants/EditProfile
  [HttpPost]
  public async Task<IActionResult> EditProfile(Tenant tenant)
  {
    var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
    var role = User.FindFirstValue(ClaimTypes.Role);

    // Only allow manager or the profile owner to update
    if (role != "Manager")
    {
      var currentTenant = await _context.Tenants.FirstOrDefaultAsync(t => t.UserId == userId);
      if (currentTenant == null || tenant.TenantId != currentTenant.TenantId)
      {
        SetErrorMessage("Unauthorized update attempt.");
        return RedirectToAction(nameof(Profile));
      }
    }

    if (!ModelState.IsValid)
    {
      SetErrorMessage("Please correct the errors in the form.");
      return PartialView("_TenantForm", tenant);
    }

    var dbTenant = await _context.Tenants
        .Include(t => t.User)
        .FirstOrDefaultAsync(t => t.TenantId == tenant.TenantId);

    if (dbTenant == null)
    {
        SetErrorMessage("Tenant not found.");
        return RedirectToAction(nameof(Profile));
    }

    // Update only the allowed fields
    dbTenant.FullName = tenant.FullName;
    dbTenant.Contact = tenant.Contact;
    dbTenant.EmergencyContactName = tenant.EmergencyContactName;
    dbTenant.EmergencyContactNumber = tenant.EmergencyContactNumber;
    // RoomId is not editable by tenant, so skip or update only if allowed

    await _context.SaveChangesAsync();
    SetSuccessMessage("Profile updated successfully.");
    return RedirectToAction(nameof(Profile), new { id = (role == "Manager" ? tenant.TenantId : (int?)null) });
  }

  [HttpGet]
  public async Task<IActionResult> TenantForm(int? id)
  {
    Tenant? model = id.HasValue
        ? await _context.Tenants.FindAsync(id.Value)
        : new Tenant();

    ViewBag.Rooms = await _context.Rooms.ToListAsync();
    return PartialView("_TenantForm", model);
  }

  [HttpPost]
  [Authorize(Roles = "Manager")]
  public async Task<IActionResult> CreateOrEdit(Tenant tenant, string username, string plainTextPassword)
  {
    if (tenant.TenantId == 0)
    {
      if (await _context.Users.AnyAsync(u => u.Username == username))
      {
        SetErrorMessage("Username already exists.");
        ViewBag.Rooms = await _context.Rooms.ToListAsync();
        return PartialView("_TenantForm", tenant);
      }
      var user = new User
      {
        Username = username,
        PasswordHash = BCrypt.Net.BCrypt.HashPassword(plainTextPassword),
        Role = "Tenant"
      };
      _context.Users.Add(user);
      await _context.SaveChangesAsync();
      tenant.UserId = user.UserId;
      _context.Tenants.Add(tenant);
      SetSuccessMessage("Tenant created successfully.");
    }
    else
    {
      var existingTenant = await _context.Tenants.Include(t => t.User).FirstOrDefaultAsync(t => t.TenantId == tenant.TenantId);
      if (existingTenant == null)
      {
        SetErrorMessage("Tenant not found.");
        return NotFound();
      }
      // Update tenant fields
      existingTenant.FullName = tenant.FullName;
      existingTenant.Contact = tenant.Contact;
      existingTenant.RoomId = tenant.RoomId;
      existingTenant.EmergencyContactName = tenant.EmergencyContactName;
      existingTenant.EmergencyContactNumber = tenant.EmergencyContactNumber;
      // Update password if provided
      if (!string.IsNullOrWhiteSpace(plainTextPassword))
      {
        existingTenant.User.PasswordHash = BCrypt.Net.BCrypt.HashPassword(plainTextPassword);
      }
      _context.Tenants.Update(existingTenant);
      SetSuccessMessage("Tenant updated successfully.");
    }
    await _context.SaveChangesAsync();
    return RedirectToAction(nameof(Index));
  }
  // GET: /Tenants/GetTenant/5

  [Authorize(Roles = "Manager")]
  public async Task<IActionResult> GetTenant(int id)
  {
    var tenant = await _context.Tenants
        .Include(t => t.Room)
        .Include(t => t.LeaseAgreements)
        .FirstOrDefaultAsync(t => t.TenantId == id);

    if (tenant == null)
    {
      SetErrorMessage("Tenant not found.");
      return NotFound();
    }
    return Json(tenant);
  }

  // POST: /Tenants/Delete/5
  [HttpPost]
  [Authorize(Roles = "Manager")]
  public async Task<IActionResult> Delete(int id)
  {
    var tenant = await _context.Tenants.FindAsync(id);
    if (tenant != null)
    {
      _context.Tenants.Remove(tenant);
      await _context.SaveChangesAsync();
      SetSuccessMessage("Tenant deleted successfully.");
    }
    else
    {
      SetErrorMessage("Tenant not found.");
    }
    return RedirectToAction(nameof(Index));
  }

  // GET: /Tenants/Login
  [AllowAnonymous]
  [HttpGet]
  public async Task<IActionResult> Login()
  {
    return View();
  }

  // POST: /Tenants/Login
  [HttpPost]
  [AllowAnonymous]
  public async Task<IActionResult> Login(TenantLoginViewModel model)
  {
    if (!ModelState.IsValid)
      return View(model);

    var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == model.Username);
    if (user == null || !BCrypt.Net.BCrypt.Verify(model.Password, user.PasswordHash))
    {
      SetErrorMessage("Invalid username or password.");
      return View(model);
    }

    var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Role, user.Role)
        };

    var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
    var authProperties = new AuthenticationProperties { IsPersistent = true };

    await HttpContext.SignInAsync(
        CookieAuthenticationDefaults.AuthenticationScheme,
        new ClaimsPrincipal(claimsIdentity),
        authProperties);

    // Redirect based on role
    if (user.Role == "Manager")
      return RedirectToAction("Index", "Tenants");
    else
      return RedirectToAction("Profile", "Tenants");
  }

  // POST: /Tenants/Logout
  [HttpPost]
  [ValidateAntiForgeryToken]
  public async Task<IActionResult> Logout()
  {
    await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return RedirectToAction("Login");
  }

  // GET: /Tenants/Register
  [AllowAnonymous]
  [HttpGet]
  public async Task<IActionResult> Register()
  {
    ViewBag.Rooms = await _context.Rooms.ToListAsync();
    return View();
  }

  // POST: /Tenants/Register
  [HttpPost]
  [AllowAnonymous]
  public async Task<IActionResult> Register(Tenant tenant, string plainTextPassword)
  {
    ModelState.Remove("PasswordHash");
    if (!ModelState.IsValid)
      return View(tenant);

    // Check if username exists in Users
    if (await _context.Users.AnyAsync(u => u.Username == tenant.User.Username))
    {
      SetErrorMessage("Username already exists.");
      return View(tenant);
    }

    var user = new User
    {
      Username = tenant.User.Username,
      PasswordHash = BCrypt.Net.BCrypt.HashPassword(plainTextPassword),
      Role = "Tenant"
    };
    _context.Users.Add(user);
    await _context.SaveChangesAsync();

    tenant.UserId = user.UserId;
    _context.Tenants.Add(tenant);
    await _context.SaveChangesAsync();

    SetSuccessMessage("Account created. Please log in.");
    return RedirectToAction("Login");
  }

  [AllowAnonymous]
  public IActionResult AccessDenied()
  {
    TempData["ErrorMessage"] = "You do not have permission to view this page.";
    return RedirectToAction("Profile");
  }
  // Helper: Get current user's UserId
  private int GetCurrentUserId()
  {
    if (User.Identity?.IsAuthenticated == true)
      return int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
    return 0;
  }
}