using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using PropertyManagement.Application.DTOs;
using PropertyManagement.Application.Services;
using PropertyManagement.Web.Controllers;
using PropertyManagement.Web.ViewModels;
using System.Security.Claims;

[Authorize]
public class MaintenanceController : BaseController
{
    private readonly IMaintenanceRequestApplicationService _maintenanceApplicationService;
    private readonly IRoomApplicationService _roomApplicationService;
    private readonly ITenantApplicationService _tenantApplicationService;
    private readonly IMapper _mapper;

    public MaintenanceController(
        IMaintenanceRequestApplicationService maintenanceApplicationService,
        IRoomApplicationService roomApplicationService,
        ITenantApplicationService tenantApplicationService,
        IMapper mapper)
    {
        _maintenanceApplicationService = maintenanceApplicationService;
        _roomApplicationService = roomApplicationService;
        _tenantApplicationService = tenantApplicationService;
        _mapper = mapper;
    }

    // GET: /Maintenance
    public async Task<IActionResult> Index()
    {
        if (User.IsInRole("Manager"))
        {
            ViewBag.IsManager = true;
            
            var result = await _maintenanceApplicationService.GetAllMaintenanceRequestsAsync();
            if (!result.IsSuccess)
            {
                SetErrorMessage(result.ErrorMessage);
                return View(new List<MaintenanceRequestViewModel>());
            }

            var maintenanceVms = _mapper.Map<List<MaintenanceRequestViewModel>>(result.Data);
            
            // Set sidebar counts
            await SetSidebarCountsAsync();
            
            return View(maintenanceVms);
        }
        else if (User.IsInRole("Tenant"))
        {
            ViewBag.IsManager = false;
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            
            var tenantResult = await _tenantApplicationService.GetTenantByUserIdAsync(userId);
            if (!tenantResult.IsSuccess)
            {
                SetErrorMessage("Tenant record not found.");
                return View(new List<MaintenanceRequestViewModel>());
            }

            var requestsResult = await _maintenanceApplicationService.GetAllMaintenanceRequestsAsync();
            if (!requestsResult.IsSuccess)
            {
                SetErrorMessage(requestsResult.ErrorMessage);
                return View(new List<MaintenanceRequestViewModel>());
            }

            // Filter requests for this tenant
            var tenantRequests = requestsResult.Data.Where(r => r.TenantId == tenantResult.Data.TenantId.ToString());
            var maintenanceVms = _mapper.Map<List<MaintenanceRequestViewModel>>(tenantRequests);
            return View(maintenanceVms);
        }
        else
        {
            return Forbid();
        }
    }

    [HttpGet]
    public async Task<IActionResult> MaintenanceRequestForm(int? id)
    {
        // Load only rooms that have tenants assigned (for maintenance requests)
        var roomsResult = await _roomApplicationService.GetAllRoomsAsync();
        var tenantsResult = await _tenantApplicationService.GetAllTenantsAsync();
        var roomList = new List<SelectListItem>();
        
        if (roomsResult.IsSuccess && tenantsResult.IsSuccess)
        {
            // Get list of room IDs that have tenants
            var roomsWithTenants = tenantsResult.Data.Select(t => t.RoomId).ToHashSet();
            
            // Filter rooms to only include those with tenants
            roomList = roomsResult.Data
                .Where(r => roomsWithTenants.Contains(r.RoomId))
                .Select(r => new SelectListItem
                {
                    Value = r.RoomId.ToString(),
                    Text = $"Room {r.Number} ({r.Type})"
                }).ToList();
        }

        MaintenanceRequestFormViewModel maintenanceVm;

        if (id.HasValue)
        {
            // Editing existing maintenance request
            var result = await _maintenanceApplicationService.GetMaintenanceRequestByIdAsync(id.Value);
            if (!result.IsSuccess)
            {
                SetErrorMessage(result.ErrorMessage);
                return PartialView("_MaintenanceRequestForm", new MaintenanceRequestFormViewModel 
                { 
                    RoomOptions = roomList
                });
            }
            
            maintenanceVm = _mapper.Map<MaintenanceRequestFormViewModel>(result.Data);
        }
        else
        {
            // Creating new maintenance request
            maintenanceVm = new MaintenanceRequestFormViewModel
            {
                RequestDate = DateTime.Today,
                Status = "Pending",
                AssignedTo = "Manager"
            };
        }
        
        maintenanceVm.RoomOptions = roomList;

        return PartialView("_MaintenanceRequestForm", maintenanceVm);
    }

    // GET: /Maintenance/SubmitTenantRequest
    [Authorize(Roles = "Tenant")]
    public async Task<IActionResult> SubmitTenantRequest()
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
        var tenantResult = await _tenantApplicationService.GetTenantByUserIdAsync(userId);
        if (!tenantResult.IsSuccess)
        {
            SetErrorMessage("Tenant record not found.");
            return RedirectToAction("Index");
        }

        var model = new MaintenanceRequestViewModel
        {
            RoomId = tenantResult.Data.RoomId,
            TenantId = tenantResult.Data.TenantId.ToString()
        };

        ViewBag.RoomNumber = tenantResult.Data.Room?.Number;
        return View(model);
    }

    private List<SelectListItem> GetStatusOptions()
    {
        return new List<SelectListItem>
        {
            new SelectListItem { Value = "Pending", Text = "Pending" },
            new SelectListItem { Value = "In Progress", Text = "In Progress" },
            new SelectListItem { Value = "Completed", Text = "Completed" }
        };
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateOrEdit(MaintenanceRequestFormViewModel maintenanceVm)
    {
        bool isAjax = Request.Headers["X-Requested-With"] == "XMLHttpRequest";
        var roomsResult = await _roomApplicationService.GetAllRoomsAsync();
        var tenantsResult = await _tenantApplicationService.GetAllTenantsAsync();
        var roomList = new List<SelectListItem>();
        if (roomsResult.IsSuccess && tenantsResult.IsSuccess)
        {
            var roomsWithTenants = tenantsResult.Data.Select(t => t.RoomId).ToHashSet();
            roomList = roomsResult.Data
                .Where(r => roomsWithTenants.Contains(r.RoomId))
                .Select(r => new SelectListItem
                {
                    Value = r.RoomId.ToString(),
                    Text = $"Room {r.Number} ({r.Type})"
                }).ToList();
        }
        maintenanceVm.RoomOptions = roomList;
        maintenanceVm.StatusOptions = GetStatusOptions();
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
            return PartialView("_MaintenanceRequestForm", maintenanceVm);
        }
        try
        {
            if (maintenanceVm.MaintenanceRequestId == 0)
            {
                string tenantId = maintenanceVm.TenantId;
                if (string.IsNullOrEmpty(tenantId))
                {
                    var tenantLookupResult = await _tenantApplicationService.GetAllTenantsAsync();
                    if (tenantLookupResult.IsSuccess)
                    {
                        var tenant = tenantLookupResult.Data.FirstOrDefault(t => t.RoomId == maintenanceVm.RoomId);
                        tenantId = tenant != null ? tenant.TenantId.ToString() : "0";
                    }
                    else
                    {
                        tenantId = "0";
                    }
                }
                var createDto = new CreateMaintenanceRequestDto
                {
                    RoomId = maintenanceVm.RoomId,
                    TenantId = tenantId,
                    Description = maintenanceVm.Description,
                    AssignedTo = maintenanceVm.AssignedTo ?? "Manager"
                };
                var result = await _maintenanceApplicationService.CreateMaintenanceRequestAsync(createDto);
                if (!result.IsSuccess)
                {
                    if (isAjax)
                    {
                        return Json(new { success = false, message = result.ErrorMessage });
                    }
                    SetErrorMessage($"Failed to create maintenance request: {result.ErrorMessage}");
                    return PartialView("_MaintenanceRequestForm", maintenanceVm);
                }
                if (isAjax)
                {
                    return Json(new { success = true, message = "Maintenance request created successfully." });
                }
                SetSuccessMessage("Maintenance request created successfully.");
            }
            else
            {
                var updateDto = new UpdateMaintenanceRequestDto
                {
                    Description = maintenanceVm.Description,
                    Status = maintenanceVm.Status,
                    AssignedTo = maintenanceVm.AssignedTo,
                    CompletedDate = maintenanceVm.Status == "Completed" ? DateTime.UtcNow : null
                };
                var result = await _maintenanceApplicationService.UpdateMaintenanceRequestAsync(maintenanceVm.MaintenanceRequestId, updateDto);
                if (!result.IsSuccess)
                {
                    if (isAjax)
                    {
                        return Json(new { success = false, message = result.ErrorMessage });
                    }
                    SetErrorMessage($"Failed to update maintenance request: {result.ErrorMessage}");
                    return PartialView("_MaintenanceRequestForm", maintenanceVm);
                }
                if (isAjax)
                {
                    return Json(new { success = true, message = "Maintenance request updated successfully." });
                }
                SetSuccessMessage("Maintenance request updated successfully.");
            }
        }
        catch (Exception ex)
        {
            if (isAjax)
            {
                return Json(new { success = false, message = $"An unexpected error occurred: {ex.Message}" });
            }
            SetErrorMessage($"An unexpected error occurred: {ex.Message}");
            return PartialView("_MaintenanceRequestForm", maintenanceVm);
        }
        return SafeRedirectToAction(nameof(Index));
    }

    // POST: /Maintenance/SubmitTenantRequest
    [HttpPost]
    [Authorize(Roles = "Tenant")]
    public async Task<IActionResult> SubmitTenantRequest(MaintenanceRequestViewModel model)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
        var tenantResult = await _tenantApplicationService.GetTenantByUserIdAsync(userId);
        if (!tenantResult.IsSuccess)
        {
            SetErrorMessage("Tenant record not found.");
            return RedirectToAction("Index");
        }

        if (!ModelState.IsValid)
        {
            ViewBag.RoomNumber = tenantResult.Data.Room?.Number;
            return View(model);
        }

        var createDto = new CreateMaintenanceRequestDto
        {
            RoomId = tenantResult.Data.RoomId,
            TenantId = tenantResult.Data.TenantId.ToString(),
            Description = model.Description,
            AssignedTo = "Manager"
        };

        var result = await _maintenanceApplicationService.CreateMaintenanceRequestAsync(createDto);
        if (!result.IsSuccess)
        {
            SetErrorMessage($"Failed to submit maintenance request: {result.ErrorMessage}");
            ViewBag.RoomNumber = tenantResult.Data.Room?.Number;
            return View(model);
        }

        TempData["Success"] = $"Your maintenance request has been submitted. Reference Number: {result.Data.MaintenanceRequestId}";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> GetTenantForRoom(int roomId)
    {
        try
        {
            var tenantsResult = await _tenantApplicationService.GetAllTenantsAsync();
            if (tenantsResult.IsSuccess)
            {
                var allTenants = tenantsResult.Data.ToList();
                var tenant = allTenants.FirstOrDefault(t => t.RoomId == roomId);
                
                // Log for debugging
                Console.WriteLine($"[GetTenantForRoom] RoomId: {roomId}");
                Console.WriteLine($"[GetTenantForRoom] Total tenants: {allTenants.Count}");
                Console.WriteLine($"[GetTenantForRoom] Tenants with rooms: {string.Join(", ", allTenants.Select(t => $"Tenant {t.TenantId} -> Room {t.RoomId}"))}");
                
                if (tenant != null)
                {
                    Console.WriteLine($"[GetTenantForRoom] Found tenant: {tenant.FullName} (ID: {tenant.TenantId}) for Room {roomId}");
                    return Json(new { 
                        success = true, 
                        tenantId = tenant.TenantId.ToString(), 
                        tenantName = tenant.FullName,
                        roomId = tenant.RoomId
                    });
                }
                else
                {
                    Console.WriteLine($"[GetTenantForRoom] No tenant found for Room {roomId}");
                    return Json(new { 
                        success = true, 
                        tenantId = "0", 
                        tenantName = "No tenant assigned",
                        roomId = roomId,
                        availableTenants = allTenants.Select(t => new { t.TenantId, t.FullName, t.RoomId })
                    });
                }
            }
            
            return Json(new { success = false, message = "Failed to retrieve tenant information", error = tenantsResult.ErrorMessage });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[GetTenantForRoom] Exception: {ex.Message}");
            return Json(new { success = false, message = ex.Message });
        }
    }


    // POST: /Maintenance/Delete/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Manager")]
    public async Task<IActionResult> Delete(int id)
    {
        var maintenanceResult = await _maintenanceApplicationService.GetMaintenanceRequestByIdAsync(id);
        string maintenanceInfo = "Maintenance request";
        if (maintenanceResult.IsSuccess && maintenanceResult.Data != null)
        {
            var roomNumber = maintenanceResult.Data.Room?.Number ?? "Unknown";
            maintenanceInfo = $"Maintenance request for Room {roomNumber} (#{maintenanceResult.Data.MaintenanceRequestId})";
        }
        var result = await _maintenanceApplicationService.DeleteMaintenanceRequestAsync(id);
        if (!result.IsSuccess)
        {
            SetErrorMessage($"Failed to delete maintenance request: {result.ErrorMessage}");
            return SafeRedirectToAction("Index");
        }
        SetSuccessMessage("Maintenance request deleted successfully.");
        return SafeRedirectToAction("Index");
    }

    // Ensure TempData keys are always set before redirect
    private RedirectToActionResult SafeRedirectToAction(string action, string controller = null, object routeValues = null)
    {
        if (!TempData.ContainsKey("Success")) TempData["Success"] = null;
        if (!TempData.ContainsKey("Error")) TempData["Error"] = null;
        return controller == null ? base.RedirectToAction(action, routeValues) : base.RedirectToAction(action, controller, routeValues);
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

    private new void SetSuccessMessage(string message)
    {
        TempData["Success"] = message;
    }
    private new void SetErrorMessage(string message)
    {
        TempData["Error"] = message;
    }
}