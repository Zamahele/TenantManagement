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
    try
    {
      var rooms = await _roomRepository.GetAllAsync();
      ViewBag.Rooms = _mapper.Map<List<RoomViewModel>>(rooms);

      var tenants = await _tenantRepository.GetAllAsync(null, t => t.Room, t => t.User);
      var tenantVms = _mapper.Map<List<TenantViewModel>>(tenants);
      return View(tenantVms);
    }
    catch (Exception ex)
    {
      SetErrorMessage("Error loading tenants: " + ex.Message);
      return View(new List<TenantViewModel>());
    }
  }

  // GET: /Tenants/Profile
  public async Task<IActionResult> Profile(int? id)
  {
    try
    {
      var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
      if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
      {
        SetErrorMessage("Invalid user session.");
        return RedirectToAction("Login");
      }

      var role = User.FindFirstValue(ClaimTypes.Role);
      
      Tenant profile;
      if (role == "Manager" && id.HasValue)
      {
        profile = await _tenantRepository.GetByIdAsync(id.Value);
      }
      else
      {
        var tenants = await _tenantRepository.GetAllAsync(t => t.UserId == userId, t => t.Room, t => t.User);
        profile = tenants.FirstOrDefault();
      }

      if (profile == null)
      {
        SetErrorMessage("Tenant not found.");
        return role == "Manager" ? RedirectToAction("Index") : RedirectToAction("Login");
      }

      var profileVm = _mapper.Map<TenantViewModel>(profile);
      return View("Profile", profileVm);
    }
    catch (Exception ex)
    {
      SetErrorMessage("Error loading profile: " + ex.Message);
      return RedirectToAction("Login");
    }
  }

  // GET: /Tenants/EditProfile
  public async Task<IActionResult> EditProfile(int? id)
  {
    try
    {
      var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
      if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
      {
        SetErrorMessage("Invalid user session.");
        return RedirectToAction("Login");
      }

      var role = User.FindFirstValue(ClaimTypes.Role);

      Tenant tenant;
      if (role == "Manager" && id.HasValue)
      {
        tenant = await _tenantRepository.GetByIdAsync(id.Value);
      }
      else
      {
        var tenants = await _tenantRepository.GetAllAsync(t => t.UserId == userId);
        tenant = tenants.FirstOrDefault();
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
    catch (Exception ex)
    {
      SetErrorMessage("Error loading edit profile: " + ex.Message);
      return RedirectToAction(nameof(Profile));
    }
  }

  // POST: /Tenants/EditProfile
  [HttpPost]
  [ValidateAntiForgeryToken]
  public async Task<IActionResult> EditProfile(TenantViewModel tenantVm)
  {
    try
    {
      var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
      if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
      {
        SetErrorMessage("Invalid user session.");
        return RedirectToAction("Login");
      }

      var role = User.FindFirstValue(ClaimTypes.Role);

      if (role != "Manager")
      {
        var tenants = await _tenantRepository.GetAllAsync(t => t.UserId == userId);
        var currentTenant = tenants.FirstOrDefault();
        if (currentTenant == null || tenantVm.TenantId != currentTenant.TenantId)
        {
          SetErrorMessage("Unauthorized update attempt.");
          return RedirectToAction(nameof(Profile));
        }
      }

      // Remove password and user-related validation for profile updates
      ModelState.Remove("User.PasswordHash");
      ModelState.Remove("User.Username");
      ModelState.Remove("User.Role");
      ModelState.Remove("User");
      ModelState.Remove("plainTextPassword");
      
      if (!ModelState.IsValid)
      {
        SetErrorMessage("Please correct the errors in the form.");
        var rooms = await _roomRepository.GetAllAsync();
        ViewBag.Rooms = _mapper.Map<List<RoomViewModel>>(rooms);
        return PartialView("_EditProfileModal", tenantVm);
      }

      var dbTenant = await _tenantRepository.GetByIdAsync(tenantVm.TenantId);

      if (dbTenant == null)
      {
        SetErrorMessage("Tenant not found.");
        return RedirectToAction(nameof(Profile));
      }

      // Only update profile fields, not user credentials
      dbTenant.FullName = tenantVm.FullName;
      dbTenant.Contact = tenantVm.Contact;
      dbTenant.EmergencyContactName = tenantVm.EmergencyContactName;
      dbTenant.EmergencyContactNumber = tenantVm.EmergencyContactNumber;
      // Note: RoomId, UserId, and User are not updated during profile edit
      
      await _tenantRepository.UpdateAsync(dbTenant);

      SetSuccessMessage("Profile updated successfully.");
      return RedirectToAction(nameof(Profile), new { id = (role == "Manager" ? tenantVm.TenantId : (int?)null) });
    }
    catch (Exception ex)
    {
      SetErrorMessage("Error updating profile: " + ex.Message);
      return RedirectToAction(nameof(Profile));
    }
  }

  [HttpGet]
  [Authorize(Roles = "Manager")]
  public async Task<IActionResult> TenantForm(int? id)
  {
    try
    {
      Tenant? model = id.HasValue
          ? await _tenantRepository.GetByIdAsync(id.Value)
          : new Tenant();

      var rooms = await _roomRepository.GetAllAsync();
      ViewBag.Rooms = _mapper.Map<List<RoomViewModel>>(rooms);
      var tenantVm = _mapper.Map<TenantViewModel>(model);
      return PartialView("_TenantForm", tenantVm);
    }
    catch (Exception ex)
    {
      SetErrorMessage("Error loading tenant form: " + ex.Message);
      return PartialView("_TenantForm", new TenantViewModel());
    }
  }

  [HttpPost]
  [ValidateAntiForgeryToken]
  [Authorize(Roles = "Manager")]
  public async Task<IActionResult> CreateOrEdit(TenantViewModel tenantVm, string username, string plainTextPassword)
  {
    try
    {
        // Remove user-related validation for this form
        ModelState.Remove("User.PasswordHash");
        ModelState.Remove("User.Username");
        ModelState.Remove("User.Role");
        ModelState.Remove("User");
        ModelState.Remove("plainTextPassword");

        if (!ModelState.IsValid)
        {
            var rooms = await _roomRepository.GetAllAsync();
            ViewBag.Rooms = _mapper.Map<List<RoomViewModel>>(rooms);
            SetErrorMessage("Please correct the errors in the form.");
            return PartialView("_TenantForm", tenantVm);
        }

        // Validate password strength - required for create, optional for edit
        if (tenantVm.TenantId == 0) // Create mode
        {
            if (string.IsNullOrWhiteSpace(plainTextPassword) || plainTextPassword.Length < 8)
            {
                var rooms = await _roomRepository.GetAllAsync();
                ViewBag.Rooms = _mapper.Map<List<RoomViewModel>>(rooms);
                SetErrorMessage("Password must be at least 8 characters long.");
                return PartialView("_TenantForm", tenantVm);
            }
        }
        else // Edit mode
        {
            if (!string.IsNullOrWhiteSpace(plainTextPassword) && plainTextPassword.Length < 8)
            {
                var rooms = await _roomRepository.GetAllAsync();
                ViewBag.Rooms = _mapper.Map<List<RoomViewModel>>(rooms);
                SetErrorMessage("Password must be at least 8 characters long.");
                return PartialView("_TenantForm", tenantVm);
            }
        }

        // Validate RoomId exists
        var room = await _roomRepository.GetByIdAsync(tenantVm.RoomId);
        if (room == null)
        {
            var rooms = await _roomRepository.GetAllAsync();
            ViewBag.Rooms = _mapper.Map<List<RoomViewModel>>(rooms);
            SetErrorMessage("Selected room does not exist.");
            return PartialView("_TenantForm", tenantVm);
        }

        if (tenantVm.TenantId == 0)
        {
            // Check for duplicate username
            var existingUsers = await _userRepository.GetAllAsync(u => u.Username == username);
            if (existingUsers.Any())
            {
                var rooms = await _roomRepository.GetAllAsync();
                SetErrorMessage("Username already exists.");
                ViewBag.Rooms = _mapper.Map<List<RoomViewModel>>(rooms);
                return PartialView("_TenantForm", tenantVm);
            }

            // Check for duplicate contact
            var existingTenants = await _tenantRepository.GetAllAsync(t => t.Contact == tenantVm.Contact);
            if (existingTenants.Any())
            {
                var rooms = await _roomRepository.GetAllAsync();
                SetErrorMessage("Contact number already exists.");
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

            // Check for duplicate contact (excluding current tenant)
            var duplicateContact = await _tenantRepository.GetAllAsync(t => t.Contact == tenantVm.Contact && t.TenantId != tenantVm.TenantId);
            if (duplicateContact.Any())
            {
                var rooms = await _roomRepository.GetAllAsync();
                SetErrorMessage("Contact number already exists.");
                ViewBag.Rooms = _mapper.Map<List<RoomViewModel>>(rooms);
                return PartialView("_TenantForm", tenantVm);
            }

            // Update tenant fields except UserId and navigation properties
            existingTenant.FullName = tenantVm.FullName;
            existingTenant.Contact = tenantVm.Contact;
            existingTenant.RoomId = tenantVm.RoomId;
            existingTenant.EmergencyContactName = tenantVm.EmergencyContactName;
            existingTenant.EmergencyContactNumber = tenantVm.EmergencyContactNumber;

            // Update username if changed
            var user = await _userRepository.GetByIdAsync(existingTenant.UserId);
            if (user != null)
            {
                if (!string.IsNullOrWhiteSpace(username) && user.Username != username)
                {
                    // Check for duplicate username
                    var duplicateUser = await _userRepository.GetAllAsync(u => u.Username == username && u.UserId != user.UserId);
                    if (duplicateUser.Any())
                    {
                        var rooms = await _roomRepository.GetAllAsync();
                        SetErrorMessage("Username already exists.");
                        ViewBag.Rooms = _mapper.Map<List<RoomViewModel>>(rooms);
                        return PartialView("_TenantForm", tenantVm);
                    }
                    user.Username = username;
                }

                // Update password if provided
                if (!string.IsNullOrWhiteSpace(plainTextPassword))
                {
                    user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(plainTextPassword);
                }
                await _userRepository.UpdateAsync(user);
            }

            await _tenantRepository.UpdateAsync(existingTenant);
            SetSuccessMessage("Tenant updated successfully.");
        }
        return RedirectToAction(nameof(Index));
    }
    catch (Exception ex)
    {
        var rooms = await _roomRepository.GetAllAsync();
        ViewBag.Rooms = _mapper.Map<List<RoomViewModel>>(rooms);
        SetErrorMessage("Error saving tenant: " + ex.Message);
        return PartialView("_TenantForm", tenantVm);
    }
  }

  [Authorize(Roles = "Manager")]
  public async Task<IActionResult> GetTenant(int id)
  {
    try
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
    catch (Exception ex)
    {
      SetErrorMessage("Error retrieving tenant: " + ex.Message);
      return BadRequest();
    }
  }

  [HttpPost]
  [ValidateAntiForgeryToken]
  [Authorize(Roles = "Manager")]
  public async Task<IActionResult> Delete(int id)
  {
    try
    {
      var tenant = await _tenantRepository.GetByIdAsync(id);

      if (tenant == null)
      {
        SetErrorMessage("Tenant not found.");
        return RedirectToAction("Index");
      }

      // TODO: Add checks for associated payments, leases, etc.
      // This would require additional repository methods or context queries

      // Delete associated user account
      var user = await _userRepository.GetByIdAsync(tenant.UserId);
      if (user != null)
      {
        await _userRepository.DeleteAsync(user);
      }

      await _tenantRepository.DeleteAsync(tenant);
      SetSuccessMessage("Tenant deleted successfully.");
      return RedirectToAction("Index");
    }
    catch (Exception ex)
    {
      SetErrorMessage("Error deleting tenant: " + ex.Message);
      return RedirectToAction("Index");
    }
  }

  [AllowAnonymous]
  [HttpGet]
  public async Task<IActionResult> Login()
  {
    return View();
  }

  [HttpPost]
  [AllowAnonymous]
  [ValidateAntiForgeryToken]
  public async Task<IActionResult> Login(TenantLoginViewModel model)
  {
    try
    {
      if (!ModelState.IsValid)
        return View(model);

      // Input validation
      if (string.IsNullOrWhiteSpace(model.Username) || string.IsNullOrWhiteSpace(model.Password))
      {
        SetErrorMessage("Username and password are required.");
        return View(model);
      }

      var users = await _userRepository.GetAllAsync(u => u.Username == model.Username);
      var user = users.FirstOrDefault();
      
      if (user == null || !BCrypt.Net.BCrypt.Verify(model.Password, user.PasswordHash))
      {
        SetErrorMessage("Invalid username or password.");
        // Add delay to prevent brute force attacks
        await Task.Delay(1000);
        return View(model);
      }

      var claims = new List<Claim>
      {
          new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
          new Claim(ClaimTypes.Name, user.Username),
          new Claim(ClaimTypes.Role, user.Role)
      };

      var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
      var authProperties = new AuthenticationProperties 
      { 
        IsPersistent = true,
        ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8) // Session timeout
      };

      await HttpContext.SignInAsync(
          CookieAuthenticationDefaults.AuthenticationScheme,
          new ClaimsPrincipal(claimsIdentity),
          authProperties);

      if (user.Role == "Manager")
        return RedirectToAction("Index", "Tenants");
      else
        return RedirectToAction("Profile", "Tenants");
    }
    catch (Exception ex)
    {
      SetErrorMessage("Login failed: " + ex.Message);
      return View(model);
    }
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
    try
    {
      var rooms = await _roomRepository.GetAllAsync();
      ViewBag.Rooms = _mapper.Map<List<RoomViewModel>>(rooms);
      return View();
    }
    catch (Exception ex)
    {
      SetErrorMessage("Error loading registration form: " + ex.Message);
      return View();
    }
  }

  [HttpPost]
  [AllowAnonymous]
  [ValidateAntiForgeryToken]
  public async Task<IActionResult> Register(TenantViewModel tenantVm, string plainTextPassword)
  {
    try
    {
      ModelState.Remove("PasswordHash");
      if (!ModelState.IsValid)
      {
        var rooms = await _roomRepository.GetAllAsync();
        ViewBag.Rooms = _mapper.Map<List<RoomViewModel>>(rooms);
        return View(tenantVm);
      }

      // Validate password strength
      if (string.IsNullOrWhiteSpace(plainTextPassword) || plainTextPassword.Length < 8)
      {
        SetErrorMessage("Password must be at least 8 characters long.");
        var rooms = await _roomRepository.GetAllAsync();
        ViewBag.Rooms = _mapper.Map<List<RoomViewModel>>(rooms);
        return View(tenantVm);
      }

      var username = tenantVm.User?.Username;
      if (string.IsNullOrWhiteSpace(username))
      {
        SetErrorMessage("Username is required.");
        var rooms = await _roomRepository.GetAllAsync();
        ViewBag.Rooms = _mapper.Map<List<RoomViewModel>>(rooms);
        return View(tenantVm);
      }

      // Check for duplicate username
      var existingUsers = await _userRepository.GetAllAsync(u => u.Username == username);
      if (existingUsers.Any())
      {
        SetErrorMessage("Username already exists.");
        var rooms = await _roomRepository.GetAllAsync();
        ViewBag.Rooms = _mapper.Map<List<RoomViewModel>>(rooms);
        return View(tenantVm);
      }

      // Check for duplicate contact
      var existingTenants = await _tenantRepository.GetAllAsync(t => t.Contact == tenantVm.Contact);
      if (existingTenants.Any())
      {
        SetErrorMessage("Contact number already exists.");
        var rooms = await _roomRepository.GetAllAsync();
        ViewBag.Rooms = _mapper.Map<List<RoomViewModel>>(rooms);
        return View(tenantVm);
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

      SetSuccessMessage("Account created successfully. Please log in.");
      return RedirectToAction("Login");
    }
    catch (Exception ex)
    {
      SetErrorMessage("Registration failed: " + ex.Message);
      var rooms = await _roomRepository.GetAllAsync();
      ViewBag.Rooms = _mapper.Map<List<RoomViewModel>>(rooms);
      return View(tenantVm);
    }
  }

  [AllowAnonymous]
  public IActionResult AccessDenied()
  {
    TempData["ErrorMessage"] = "You do not have permission to view this page.";
    return RedirectToAction("Profile");
  }

  // GET: /Tenants/ChangePassword/5
  [Authorize(Roles = "Manager")]
  public async Task<IActionResult> ChangePassword(int id)
  {
    try
    {
      var tenant = await _tenantRepository.GetByIdAsync(id);
      if (tenant == null)
      {
        SetErrorMessage("Tenant not found.");
        return NotFound();
      }

      var model = new ChangePasswordViewModel
      {
        TenantId = tenant.TenantId,
        TenantName = tenant.FullName,
        Contact = tenant.Contact
      };

      return PartialView("_ChangePasswordModal", model);
    }
    catch (Exception ex)
    {
      SetErrorMessage("Error loading password change form: " + ex.Message);
      return PartialView("_ChangePasswordModal", new ChangePasswordViewModel());
    }
  }

  // POST: /Tenants/ChangePassword
  [HttpPost]
  [ValidateAntiForgeryToken]
  [Authorize(Roles = "Manager")]
  public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
  {
    try
    {
      if (!ModelState.IsValid)
      {
        SetErrorMessage("Please correct the errors in the form.");
        return PartialView("_ChangePasswordModal", model);
      }

      var tenant = await _tenantRepository.GetByIdAsync(model.TenantId);
      if (tenant == null)
      {
        SetErrorMessage("Tenant not found.");
        return NotFound();
      }

      var user = await _userRepository.GetByIdAsync(tenant.UserId);
      if (user == null)
      {
        SetErrorMessage("User account not found.");
        return NotFound();
      }

      // Verify current password
      if (!BCrypt.Net.BCrypt.Verify(model.CurrentPassword, user.PasswordHash))
      {
        SetErrorMessage("Current password is incorrect.");
        return PartialView("_ChangePasswordModal", model);
      }

      // Update password
      user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.NewPassword);
      await _userRepository.UpdateAsync(user);

      SetSuccessMessage($"Password updated successfully for {tenant.FullName}.");
      return RedirectToAction(nameof(Index));
    }
    catch (Exception ex)
    {
      SetErrorMessage("Error updating password: " + ex.Message);
      return PartialView("_ChangePasswordModal", model);
    }
  }

  private int GetCurrentUserId()
  {
    if (User.Identity?.IsAuthenticated == true)
      return int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
    return 0;
  }
}