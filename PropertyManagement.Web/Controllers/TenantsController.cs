using AutoMapper;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PropertyManagement.Application.Common;
using PropertyManagement.Application.DTOs;
using PropertyManagement.Application.Services;
using PropertyManagement.Web.Controllers;
using PropertyManagement.Web.ViewModels;
using System.Security.Claims;

namespace PropertyManagement.Web.Controllers;

[Authorize]
public class TenantsController : BaseController
{
  private readonly ITenantApplicationService _tenantApplicationService;
  private readonly IRoomApplicationService _roomApplicationService;
  private readonly IMaintenanceRequestApplicationService _maintenanceApplicationService;
  private readonly IMapper _mapper;

  public TenantsController(
      ITenantApplicationService tenantApplicationService,
      IRoomApplicationService roomApplicationService,
      IMaintenanceRequestApplicationService maintenanceApplicationService,
      IMapper mapper)
  {
    _tenantApplicationService = tenantApplicationService;
    _roomApplicationService = roomApplicationService;
    _maintenanceApplicationService = maintenanceApplicationService;
    _mapper = mapper;
  }

  // GET: /Tenants
  [Authorize(Roles = "Manager")]
  public async Task<IActionResult> Index()
  {
    var result = await _tenantApplicationService.GetAllTenantsAsync();
    if (!result.IsSuccess)
    {
      SetErrorMessage(result.ErrorMessage);
      return View(new List<TenantViewModel>());
    }

    var tenantVms = _mapper.Map<List<TenantViewModel>>(result.Data);
    
    // Set sidebar counts
    await SetSidebarCountsAsync();
    
    return View(tenantVms);
  }

  // GET: /Tenants/Profile
  public async Task<IActionResult> Profile(int? id)
  {
    var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
    if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
    {
      SetErrorMessage("Invalid user session.");
      return RedirectToAction("Login");
    }

    var role = User.FindFirstValue(ClaimTypes.Role);

    ServiceResult<TenantDto> result;
    if (role == "Manager" && id.HasValue)
    {
      result = await _tenantApplicationService.GetTenantByIdAsync(id.Value);
    }
    else
    {
      result = await _tenantApplicationService.GetTenantByUserIdAsync(userId);
    }

    if (!result.IsSuccess)
    {
      SetErrorMessage(result.ErrorMessage);
      return role == "Manager" ? RedirectToAction("Index") : RedirectToAction("Login");
    }

    var profileVm = _mapper.Map<TenantViewModel>(result.Data);
    return View("Profile", profileVm);
  }

  // GET: /Tenants/EditProfile
  public async Task<IActionResult> EditProfile(int? id)
  {
    var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
    if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
    {
      SetErrorMessage("Invalid user session.");
      return RedirectToAction("Login");
    }

    var role = User.FindFirstValue(ClaimTypes.Role);

    ServiceResult<TenantDto> result;
    if (role == "Manager" && id.HasValue)
    {
      result = await _tenantApplicationService.GetTenantByIdAsync(id.Value);
    }
    else
    {
      result = await _tenantApplicationService.GetTenantByUserIdAsync(userId);
    }

    if (!result.IsSuccess)
    {
      SetErrorMessage(result.ErrorMessage);
      return RedirectToAction(nameof(Profile));
    }

    var tenantVm = _mapper.Map<TenantViewModel>(result.Data);
    return PartialView("_EditProfileModal", tenantVm);
  }

  // POST: /Tenants/EditProfile
  [HttpPost]
  [ValidateAntiForgeryToken]
  public async Task<IActionResult> EditProfile(TenantViewModel tenantVm)
  {
    var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
    if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
    {
      SetErrorMessage("Invalid user session.");
      return RedirectToAction("Login");
    }

    var role = User.FindFirstValue(ClaimTypes.Role);

    // Business rule: Non-managers can only edit their own profile
    if (role != "Manager")
    {
      var currentTenantResult = await _tenantApplicationService.GetTenantByUserIdAsync(userId);
      if (!currentTenantResult.IsSuccess || tenantVm.TenantId != currentTenantResult.Data.TenantId)
      {
        SetErrorMessage("Unauthorized update attempt.");
        return RedirectToAction(nameof(Profile));
      }
    }

    // Remove user-related validation for profile updates
    ModelState.Remove("User.PasswordHash");
    ModelState.Remove("User.Username");
    ModelState.Remove("User.Role");
    ModelState.Remove("User");
    ModelState.Remove("plainTextPassword");

    if (!ModelState.IsValid)
    {
      SetErrorMessage("Please correct the errors in the form.");
      return PartialView("_EditProfileModal", tenantVm);
    }

    var updateProfileDto = new UpdateProfileDto
    {
      FullName = tenantVm.FullName,
      Contact = tenantVm.Contact,
      EmergencyContactName = tenantVm.EmergencyContactName,
      EmergencyContactNumber = tenantVm.EmergencyContactNumber
    };

    var result = await _tenantApplicationService.UpdateProfileAsync(tenantVm.TenantId, updateProfileDto);
    if (!result.IsSuccess)
    {
      SetErrorMessage(result.ErrorMessage);
      return PartialView("_EditProfileModal", tenantVm);
    }

    SetSuccessMessage("Profile updated successfully.");
    return RedirectToAction(nameof(Profile), new { id = (role == "Manager" ? tenantVm.TenantId : (int?)null) });
  }

  [HttpGet]
  [Authorize(Roles = "Manager")]
  public async Task<IActionResult> TenantForm(int? id)
  {
    // Load available rooms for the dropdown
    var roomsResult = await _roomApplicationService.GetAvailableRoomsAsync();
    var roomList = new List<RoomViewModel>();
    
    if (roomsResult.IsSuccess)
    {
      roomList = _mapper.Map<List<RoomViewModel>>(roomsResult.Data);
    }

    TenantViewModel tenantVm;

    if (id.HasValue)
    {
      // Editing existing tenant
      var result = await _tenantApplicationService.GetTenantByIdAsync(id.Value);
      if (!result.IsSuccess)
      {
        SetErrorMessage(result.ErrorMessage);
        ViewBag.Rooms = roomList;
        return PartialView("_TenantForm", new TenantViewModel());
      }
      
      tenantVm = _mapper.Map<TenantViewModel>(result.Data);
      
      // If tenant has a room assigned, make sure it's included in the dropdown
      if (tenantVm.RoomId > 0 && roomList.All(r => r.RoomId != tenantVm.RoomId))
      {
        // Get the currently assigned room and add it to the list
        var currentRoomResult = await _roomApplicationService.GetRoomByIdAsync(tenantVm.RoomId);
        if (currentRoomResult.IsSuccess)
        {
          var currentRoom = _mapper.Map<RoomViewModel>(currentRoomResult.Data);
          roomList.Add(currentRoom);
          // Sort the list by room number for better UX
          roomList = roomList.OrderBy(r => r.Number).ToList();
        }
      }
    }
    else
    {
      // Creating new tenant
      tenantVm = new TenantViewModel();
    }

    ViewBag.Rooms = roomList;
    if (!roomsResult.IsSuccess && roomList.Count == 0)
    {
      SetErrorMessage("Unable to load available rooms.");
    }

    return PartialView("_TenantForm", tenantVm);
  }

  [HttpPost]
  [ValidateAntiForgeryToken]
  [Authorize(Roles = "Manager")]
  public async Task<IActionResult> CreateOrEdit(TenantViewModel tenantVm, string username, string plainTextPassword)
  {
    bool isAjax = Request.Headers["X-Requested-With"] == "XMLHttpRequest";
    
    // Add available rooms for dropdown in case of validation error
    var roomsResult = await _roomApplicationService.GetAvailableRoomsAsync();
    var roomList = new List<RoomViewModel>();
    
    if (roomsResult.IsSuccess)
    {
      roomList = _mapper.Map<List<RoomViewModel>>(roomsResult.Data);
    }
    
    // If editing and tenant has a room assigned, make sure it's included in the dropdown
    if (tenantVm.TenantId > 0 && tenantVm.RoomId > 0 && roomList.All(r => r.RoomId != tenantVm.RoomId))
    {
      var currentRoomResult = await _roomApplicationService.GetRoomByIdAsync(tenantVm.RoomId);
      if (currentRoomResult.IsSuccess)
      {
        var currentRoom = _mapper.Map<RoomViewModel>(currentRoomResult.Data);
        roomList.Add(currentRoom);
        roomList = roomList.OrderBy(r => r.Number).ToList();
      }
    }
    
    ViewBag.Rooms = roomList;

    // Remove user-related validation for this form
    ModelState.Remove("User.PasswordHash");
    ModelState.Remove("User.Username");
    ModelState.Remove("User.Role");
    ModelState.Remove("User");
    ModelState.Remove("plainTextPassword");

    if (!ModelState.IsValid)
    {
      if (isAjax)
      {
        var errors = ModelState.Values
            .SelectMany(v => v.Errors)
            .Select(e => e.ErrorMessage)
            .ToList();
        return Json(new { success = false, message = "Please correct the form errors.", errors = errors });
      }
      
      SetErrorMessage("Please correct the errors in the form.");
      return PartialView("_TenantForm", tenantVm);
    }

    try
    {
      if (tenantVm.TenantId == 0)
      {
        // Create new tenant
        var createTenantDto = new CreateTenantDto
        {
          FullName = tenantVm.FullName,
          Contact = tenantVm.Contact,
          EmergencyContactName = tenantVm.EmergencyContactName,
          EmergencyContactNumber = tenantVm.EmergencyContactNumber,
          RoomId = tenantVm.RoomId,
          Username = username,
          Password = plainTextPassword
        };

        var result = await _tenantApplicationService.CreateTenantAsync(createTenantDto);
        if (!result.IsSuccess)
        {
          if (isAjax)
          {
            return Json(new { success = false, message = result.ErrorMessage });
          }
          
          // Provide specific error messages based on the failure reason
          if (result.ErrorMessage.Contains("not available") || result.ErrorMessage.Contains("already has a tenant"))
          {
            ModelState.AddModelError("RoomId", result.ErrorMessage);
            SetErrorMessage($"❌ Room Assignment Failed: {result.ErrorMessage}");
          }
          else if (result.ErrorMessage.Contains("Username already exists"))
          {
            ModelState.AddModelError("Username", result.ErrorMessage);
            SetErrorMessage($"❌ Username Error: {result.ErrorMessage}");
          }
          else if (result.ErrorMessage.Contains("Contact number already exists"))
          {
            ModelState.AddModelError("Contact", result.ErrorMessage);
            SetErrorMessage($"❌ Contact Error: {result.ErrorMessage}");
          }
          else if (result.ErrorMessage.Contains("Password"))
          {
            ModelState.AddModelError("Password", result.ErrorMessage);
            SetErrorMessage($"❌ Password Error: {result.ErrorMessage}");
          }
          else
          {
            SetErrorMessage($"❌ Failed to create tenant: {result.ErrorMessage}");
          }
          
          return PartialView("_TenantForm", tenantVm);
        }

        if (isAjax)
        {
          return Json(new { success = true, message = $"Tenant '{tenantVm.FullName}' created successfully!" });
        }

        SetSuccessMessage($"✅ Tenant '{tenantVm.FullName}' created successfully and assigned to room!");
      }
      else
      {
        // Update existing tenant
        var updateTenantDto = new UpdateTenantDto
        {
          FullName = tenantVm.FullName,
          Contact = tenantVm.Contact,
          EmergencyContactName = tenantVm.EmergencyContactName,
          EmergencyContactNumber = tenantVm.EmergencyContactNumber,
          RoomId = tenantVm.RoomId,
          Username = username,
          Password = plainTextPassword
        };

        var result = await _tenantApplicationService.UpdateTenantAsync(tenantVm.TenantId, updateTenantDto);
        if (!result.IsSuccess)
        {
          if (isAjax)
          {
            return Json(new { success = false, message = result.ErrorMessage });
          }
          
          // Provide specific error messages based on the failure reason
          if (result.ErrorMessage.Contains("not available") || result.ErrorMessage.Contains("already has a tenant"))
          {
            ModelState.AddModelError("RoomId", result.ErrorMessage);
            SetErrorMessage($"❌ Room Assignment Failed: {result.ErrorMessage}");
          }
          else if (result.ErrorMessage.Contains("Username already exists"))
          {
            ModelState.AddModelError("Username", result.ErrorMessage);
            SetErrorMessage($"❌ Username Error: {result.ErrorMessage}");
          }
          else if (result.ErrorMessage.Contains("Contact number already exists"))
          {
            ModelState.AddModelError("Contact", result.ErrorMessage);
            SetErrorMessage($"❌ Contact Error: {result.ErrorMessage}");
          }
          else if (result.ErrorMessage.Contains("Password"))
          {
            ModelState.AddModelError("Password", result.ErrorMessage);
            SetErrorMessage($"❌ Password Error: {result.ErrorMessage}");
          }
          else
          {
            SetErrorMessage($"❌ Failed to update tenant: {result.ErrorMessage}");
          }
          
          return PartialView("_TenantForm", tenantVm);
        }

        if (isAjax)
        {
          return Json(new { success = true, message = $"Tenant '{tenantVm.FullName}' updated successfully!" });
        }

        SetSuccessMessage($"✅ Tenant '{tenantVm.FullName}' updated successfully with room assignment!");
      }
    }
    catch (Exception ex)
    {
      if (isAjax)
      {
        return Json(new { success = false, message = $"An unexpected error occurred: {ex.Message}" });
      }
      
      SetErrorMessage($"❌ An unexpected error occurred: {ex.Message}");
      return PartialView("_TenantForm", tenantVm);
    }

    return RedirectToAction(nameof(Index));
  }

  [Authorize(Roles = "Manager")]
  public async Task<IActionResult> GetTenant(int id)
  {
    var result = await _tenantApplicationService.GetTenantByIdAsync(id);
    if (!result.IsSuccess)
    {
      SetErrorMessage(result.ErrorMessage);
      return NotFound();
    }

    var tenantVm = _mapper.Map<TenantViewModel>(result.Data);
    return Json(tenantVm);
  }

  [HttpPost]
  [ValidateAntiForgeryToken]
  [Authorize(Roles = "Manager")]
  public async Task<IActionResult> Delete(int id)
  {
    // Get tenant details before deletion for better messaging
    var tenantResult = await _tenantApplicationService.GetTenantByIdAsync(id);
    string tenantInfo = "Tenant";
    
    if (tenantResult.IsSuccess && tenantResult.Data != null)
    {
      var roomResult = await _roomApplicationService.GetRoomByIdAsync(tenantResult.Data.RoomId);
      var roomNumber = roomResult.IsSuccess ? roomResult.Data.Number : "Unknown";
      tenantInfo = $"{tenantResult.Data.FullName} (Room {roomNumber})";
    }

    var result = await _tenantApplicationService.DeleteTenantAsync(id);
    if (!result.IsSuccess)
    {
      SetErrorMessage($"❌ Failed to delete tenant: {result.ErrorMessage}");
    }
    else
    {
      SetSuccessMessage($"✅ {tenantInfo} deleted successfully. Room is now available for new assignments.");
    }

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
  [ValidateAntiForgeryToken]
  public async Task<IActionResult> Login(TenantLoginViewModel model)
  {
    if (!ModelState.IsValid)
      return View(model);

    var result = await _tenantApplicationService.AuthenticateAsync(model.Username, model.Password);
    if (!result.IsSuccess)
    {
      SetErrorMessage(result.ErrorMessage);
      return View(model);
    }

    var user = result.Data;
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
      ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
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
    return View();
  }

  [HttpPost]
  [AllowAnonymous]
  [ValidateAntiForgeryToken]
  public async Task<IActionResult> Register(TenantViewModel tenantVm, string plainTextPassword)
  {
    ModelState.Remove("PasswordHash");
    
    if (!ModelState.IsValid)
    {
      // Load available rooms for dropdown
      var roomsResult = await _roomApplicationService.GetAvailableRoomsAsync();
      ViewBag.Rooms = roomsResult.IsSuccess ? _mapper.Map<List<RoomViewModel>>(roomsResult.Data) : new List<RoomViewModel>();
      return View(tenantVm);
    }

    var registerTenantDto = new RegisterTenantDto
    {
      FullName = tenantVm.FullName,
      Contact = tenantVm.Contact,
      EmergencyContactName = tenantVm.EmergencyContactName,
      EmergencyContactNumber = tenantVm.EmergencyContactNumber,
      RoomId = tenantVm.RoomId,
      Username = tenantVm.User?.Username ?? "",
      Password = plainTextPassword
    };

    var result = await _tenantApplicationService.RegisterTenantAsync(registerTenantDto);
    if (!result.IsSuccess)
    {
      // Load available rooms for dropdown
      var roomsResult = await _roomApplicationService.GetAvailableRoomsAsync();
      ViewBag.Rooms = roomsResult.IsSuccess ? _mapper.Map<List<RoomViewModel>>(roomsResult.Data) : new List<RoomViewModel>();
      
      // Provide specific error messages based on the failure reason
      if (result.ErrorMessage.Contains("not available") || result.ErrorMessage.Contains("already has a tenant"))
      {
        ModelState.AddModelError("RoomId", result.ErrorMessage);
        SetErrorMessage($"❌ Room Selection Error: {result.ErrorMessage}");
      }
      else if (result.ErrorMessage.Contains("Username already exists"))
      {
        ModelState.AddModelError("User.Username", result.ErrorMessage);
        SetErrorMessage($"❌ Username Error: {result.ErrorMessage}");
      }
      else if (result.ErrorMessage.Contains("Contact number already exists"))
      {
        ModelState.AddModelError("Contact", result.ErrorMessage);
        SetErrorMessage($"❌ Contact Error: {result.ErrorMessage}");
      }
      else if (result.ErrorMessage.Contains("Password"))
      {
        SetErrorMessage($"❌ Password Error: {result.ErrorMessage}");
      }
      else
      {
        SetErrorMessage($"❌ Registration failed: {result.ErrorMessage}");
      }
      
      return View(tenantVm);
    }

    SetSuccessMessage("✅ Account created successfully! Your room has been reserved. Please log in.");
    return RedirectToAction("Login");
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
    var result = await _tenantApplicationService.GetTenantByIdAsync(id);
    if (!result.IsSuccess)
    {
      SetErrorMessage(result.ErrorMessage);
      return NotFound();
    }

    var tenant = result.Data;
    var model = new ChangePasswordViewModel
    {
      TenantId = tenant.TenantId,
      TenantName = tenant.FullName,
      Contact = tenant.Contact
    };

    return PartialView("_ChangePasswordModal", model);
  }

  // POST: /Tenants/ChangePassword
  [HttpPost]
  [ValidateAntiForgeryToken]
  [Authorize(Roles = "Manager")]
  public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
  {
    if (!ModelState.IsValid)
    {
      SetErrorMessage("Please correct the errors in the form.");
      return PartialView("_ChangePasswordModal", model);
    }

    var result = await _tenantApplicationService.ChangePasswordAsync(model.TenantId, model.CurrentPassword, model.NewPassword);
    if (!result.IsSuccess)
    {
      SetErrorMessage(result.ErrorMessage);
      return PartialView("_ChangePasswordModal", model);
    }

    SetSuccessMessage($"Password updated successfully for {model.TenantName}.");
    return RedirectToAction(nameof(Index));
  }

  private int GetCurrentUserId()
  {
    if (User.Identity?.IsAuthenticated == true)
      return int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
    return 0;
  }

  private async Task SetSidebarCountsAsync()
  {
    try
    {
      // Get tenant count
      var tenantsResult = await _tenantApplicationService.GetAllTenantsAsync();
      var tenantCount = tenantsResult.IsSuccess && tenantsResult.Data != null ? 
        tenantsResult.Data.Count() : 0;

      // Get room count
      var roomsResult = await _roomApplicationService.GetAllRoomsAsync();
      var roomCount = roomsResult.IsSuccess && roomsResult.Data != null ? 
        roomsResult.Data.Count() : 0;

      // Get pending maintenance count
      var maintenanceResult = await _maintenanceApplicationService.GetAllMaintenanceRequestsAsync();
      var pendingCount = 0;
      if (maintenanceResult.IsSuccess && maintenanceResult.Data != null)
      {
        pendingCount = maintenanceResult.Data.Count(m => 
          m.Status == "Pending" || m.Status == "In Progress");
      }

      // Set the ViewBag values
      ViewBag.TenantCount = tenantCount;
      ViewBag.RoomCount = roomCount;
      ViewBag.PendingMaintenanceCount = pendingCount;
    }
    catch
    {
      ViewBag.TenantCount = 0;
      ViewBag.RoomCount = 0;
      ViewBag.PendingMaintenanceCount = 0;
    }
  }
}