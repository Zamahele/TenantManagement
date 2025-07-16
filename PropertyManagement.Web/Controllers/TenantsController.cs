using AutoMapper;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PropertyManagement.Domain.Entities;
using PropertyManagement.Infrastructure.Repositories;
using PropertyManagement.Web.Controllers;
using PropertyManagement.Web.ViewModels;
using System.Security.Claims;
using System.Threading.Tasks;

[Authorize]
public class TenantsController : BaseController
{
  private readonly IGenericRepository<Tenant> _tenantRepository;
  private readonly IGenericRepository<User> _userRepository;
  private readonly IGenericRepository<Room> _roomRepository;
  private readonly IMapper _mapper;

  public TenantsController(
      IGenericRepository<Tenant> tenantRepository,
      IGenericRepository<User> userRepository,
      IGenericRepository<Room> roomRepository,
      IMapper mapper)
  {
    _tenantRepository = tenantRepository;
    _userRepository = userRepository;
    _roomRepository = roomRepository;
    _mapper = mapper;
  }

  // GET: /Tenants
  [Authorize(Roles = "Manager")]
  public async Task<IActionResult> Index()
  {
    var rooms = await _roomRepository.GetAllAsync();
    ViewBag.Rooms = _mapper.Map<List<RoomViewModel>>(rooms);

    var tenants = await _tenantRepository.GetAllAsync();
    var tenantVms = _mapper.Map<List<TenantViewModel>>(tenants);
    return View(tenantVms);
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
      var tenants = await _tenantRepository.GetAllAsync();
      var tenant = tenants.FirstOrDefault(t => t.UserId == userId);
      if (tenant == null)
      {
        SetErrorMessage("Tenant not found.");
        return RedirectToAction("Login");
      }
      tenantId = tenant.TenantId;
    }

    var allTenants = await _tenantRepository.GetAllAsync();
    var profile = allTenants.FirstOrDefault(t => t.TenantId == tenantId);

    if (profile == null)
    {
      SetErrorMessage("Tenant not found.");
      return RedirectToAction("Index");
    }

    var profileVm = _mapper.Map<TenantViewModel>(profile);
    return View("Profile", profileVm);
  }

  // GET: /Tenants/EditProfile
  public async Task<IActionResult> EditProfile(int? id)
  {
    var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
    var role = User.FindFirstValue(ClaimTypes.Role);

    Tenant tenant;
    if (role == "Manager" && id.HasValue)
    {
      tenant = await _tenantRepository.GetByIdAsync(id.Value);
    }
    else
    {
      var tenants = await _tenantRepository.GetAllAsync();
      tenant = tenants.FirstOrDefault(t => t.UserId == userId);
    }

    var rooms = await _roomRepository.GetAllAsync();
    ViewBag.Rooms = _mapper.Map<List<RoomViewModel>>(rooms);

    if (tenant == null)
    {
      SetErrorMessage("Tenant not found.");
      return RedirectToAction(nameof(Profile));
    }
    var tenantVm = _mapper.Map<TenantViewModel>(tenant);
    return PartialView("_EditProfileModal", tenantVm);
  }

  // POST: /Tenants/EditProfile
  [HttpPost]
  public async Task<IActionResult> EditProfile(TenantViewModel tenantVm)
  {
    var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
    var role = User.FindFirstValue(ClaimTypes.Role);

    if (role != "Manager")
    {
      var tenants = await _tenantRepository.GetAllAsync();
      var currentTenant = tenants.FirstOrDefault(t => t.UserId == userId);
      if (currentTenant == null || tenantVm.TenantId != currentTenant.TenantId)
      {
        SetErrorMessage("Unauthorized update attempt.");
        return RedirectToAction(nameof(Profile));
      }
    }

    if (!ModelState.IsValid)
    {
      SetErrorMessage("Please correct the errors in the form.");
      return PartialView("_TenantForm", tenantVm);
    }

    var dbTenant = await _tenantRepository.GetByIdAsync(tenantVm.TenantId);

    if (dbTenant == null)
    {
      SetErrorMessage("Tenant not found.");
      return RedirectToAction(nameof(Profile));
    }

    _mapper.Map(tenantVm, dbTenant);
    await _tenantRepository.UpdateAsync(dbTenant);

    SetSuccessMessage("Profile updated successfully.");
    return RedirectToAction(nameof(Profile), new { id = (role == "Manager" ? tenantVm.TenantId : (int?)null) });
  }

  [HttpGet]
  public async Task<IActionResult> TenantForm(int? id)
  {
    Tenant? model = id.HasValue
        ? await _tenantRepository.GetByIdAsync(id.Value)
        : new Tenant();

    var rooms = await _roomRepository.GetAllAsync();
    ViewBag.Rooms = _mapper.Map<List<RoomViewModel>>(rooms);
    var tenantVm = _mapper.Map<TenantViewModel>(model);
    return PartialView("_TenantForm", tenantVm);
  }

  [HttpPost]
  [Authorize(Roles = "Manager")]
  public async Task<IActionResult> CreateOrEdit(TenantViewModel tenantVm, string username, string plainTextPassword)
  {
    if (tenantVm.TenantId == 0)
    {
      var users = await _userRepository.GetAllAsync();
      if (users.Any(u => u.Username == username))
      {
        var rooms = await _roomRepository.GetAllAsync();
        SetErrorMessage("Username already exists.");
        ViewBag.Rooms = _mapper.Map<List<RoomViewModel>>(rooms);
        return PartialView("_TenantForm", tenantVm);
      }
      var user = new User
      {
        Username = username,
        PasswordHash = BCrypt.Net.BCrypt.HashPassword(plainTextPassword),
        Role = "Tenant"
      };
      await _userRepository.AddAsync(user);
      tenantVm.UserId = user.UserId;
      var tenant = _mapper.Map<Tenant>(tenantVm);
      await _tenantRepository.AddAsync(tenant);
      SetSuccessMessage("Tenant created successfully.");
    }
    else
    {
      var existingTenant = await _tenantRepository.GetByIdAsync(tenantVm.TenantId);
      if (existingTenant == null)
      {
        SetErrorMessage("Tenant not found.");
        return NotFound();
      }
      _mapper.Map(tenantVm, existingTenant);

      // Update password if provided
      var user = await _userRepository.GetByIdAsync(existingTenant.UserId);
      if (user != null && !string.IsNullOrWhiteSpace(plainTextPassword))
      {
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(plainTextPassword);
        await _userRepository.UpdateAsync(user);
      }
      await _tenantRepository.UpdateAsync(existingTenant);
      SetSuccessMessage("Tenant updated successfully.");
    }
    return RedirectToAction(nameof(Index));
  }

  [Authorize(Roles = "Manager")]
  public async Task<IActionResult> GetTenant(int id)
  {
    var tenant = await _tenantRepository.GetByIdAsync(id);

    if (tenant == null)
    {
      SetErrorMessage("Tenant not found.");
      return NotFound();
    }
    var tenantVm = _mapper.Map<TenantViewModel>(tenant);
    return Json(tenantVm);
  }

  [HttpPost]
  [Authorize(Roles = "Manager")]
  public async Task<IActionResult> Delete(int id)
  {
    var tenant = await _tenantRepository.GetByIdAsync(id);

    if (tenant == null)
    {
      SetErrorMessage("Tenant not found.");
      return RedirectToAction("Index");
    }

    // For associated payments or leases, you may need to check via context or extend repository
    // Here, assume you have logic to check before delete

    await _tenantRepository.DeleteAsync(tenant);
    SetSuccessMessage("Tenant deleted successfully.");
    return RedirectToAction("Index");
  }

  [AllowAnonymous]
  [HttpGet]
  public async Task<IActionResult> Login()
  {
    return View();
  }

  [HttpPost]
  [AllowAnonymous]
  public async Task<IActionResult> Login(TenantLoginViewModel model)
  {
    if (!ModelState.IsValid)
      return View(model);

    var users = await _userRepository.GetAllAsync();
    var user = users.FirstOrDefault(u => u.Username == model.Username);
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

    if (user.Role == "Manager")
      return RedirectToAction("Index", "Tenants");
    else
      return RedirectToAction("Profile", "Tenants");
  }

  [HttpPost]
  [ValidateAntiForgeryToken]
  public async Task<IActionResult> Logout()
  {
    await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return RedirectToAction("Login");
  }

  [AllowAnonymous]
  [HttpGet]
  public async Task<IActionResult> Register()
  {
    var rooms = await _roomRepository.GetAllAsync();
    ViewBag.Rooms = _mapper.Map<List<RoomViewModel>>(rooms);
    return View();
  }

  [HttpPost]
  [AllowAnonymous]
  public async Task<IActionResult> Register(TenantViewModel tenantVm, string plainTextPassword)
  {
    ModelState.Remove("PasswordHash");
    if (!ModelState.IsValid)
      return View(tenantVm);

    var users = await _userRepository.GetAllAsync();
    if (users.Any(u => u.Username == tenantVm.User.Username))
    {
      SetErrorMessage("Username already exists.");
      return View(tenantVm);
    }

    var user = new User
    {
      Username = tenantVm.User?.Username,
      PasswordHash = BCrypt.Net.BCrypt.HashPassword(plainTextPassword),
      Role = "Tenant"
    };
    await _userRepository.AddAsync(user);

    tenantVm.UserId = user.UserId;
    var tenant = _mapper.Map<Tenant>(tenantVm);
    await _tenantRepository.AddAsync(tenant);

    SetSuccessMessage("Account created. Please log in.");
    return RedirectToAction("Login");
  }

  [AllowAnonymous]
  public IActionResult AccessDenied()
  {
    TempData["ErrorMessage"] = "You do not have permission to view this page.";
    return RedirectToAction("Profile");
  }

  private int GetCurrentUserId()
  {
    if (User.Identity?.IsAuthenticated == true)
      return int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
    return 0;
  }
}