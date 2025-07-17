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
    private readonly IMapper _mapper;

    public TenantsController(
        ITenantApplicationService tenantApplicationService,
        IMapper mapper)
    {
        _tenantApplicationService = tenantApplicationService;
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
        TenantViewModel tenantVm;
        
        if (id.HasValue)
        {
            var result = await _tenantApplicationService.GetTenantByIdAsync(id.Value);
            if (!result.IsSuccess)
            {
                SetErrorMessage(result.ErrorMessage);
                return PartialView("_TenantForm", new TenantViewModel());
            }
            tenantVm = _mapper.Map<TenantViewModel>(result.Data);
        }
        else
        {
            tenantVm = new TenantViewModel();
        }

        return PartialView("_TenantForm", tenantVm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Manager")]
    public async Task<IActionResult> CreateOrEdit(TenantViewModel tenantVm, string username, string plainTextPassword)
    {
        // Remove user-related validation for this form
        ModelState.Remove("User.PasswordHash");
        ModelState.Remove("User.Username");
        ModelState.Remove("User.Role");
        ModelState.Remove("User");
        ModelState.Remove("plainTextPassword");

        if (!ModelState.IsValid)
        {
            SetErrorMessage("Please correct the errors in the form.");
            return PartialView("_TenantForm", tenantVm);
        }

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
                SetErrorMessage(result.ErrorMessage);
                return PartialView("_TenantForm", tenantVm);
            }

            SetSuccessMessage("Tenant created successfully.");
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
                SetErrorMessage(result.ErrorMessage);
                return PartialView("_TenantForm", tenantVm);
            }

            SetSuccessMessage("Tenant updated successfully.");
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
        var result = await _tenantApplicationService.DeleteTenantAsync(id);
        if (!result.IsSuccess)
        {
            SetErrorMessage(result.ErrorMessage);
        }
        else
        {
            SetSuccessMessage("Tenant deleted successfully.");
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
            SetErrorMessage(result.ErrorMessage);
            return View(tenantVm);
        }

        SetSuccessMessage("Account created successfully. Please log in.");
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
}