using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using PropertyManagement.Application.DTOs;
using PropertyManagement.Application.Services;
using PropertyManagement.Web.Controllers;
using PropertyManagement.Web.ViewModels;

namespace PropertyManagement.Web.Controllers;

[Authorize]
[Authorize(Roles = "Manager")]
public class UtilityBillsController : BaseController
{
    private readonly IUtilityBillApplicationService _utilityBillApplicationService;
    private readonly IRoomApplicationService _roomApplicationService;
    private readonly ITenantApplicationService _tenantApplicationService;
    private readonly IMaintenanceRequestApplicationService _maintenanceApplicationService;
    private readonly IMapper _mapper;
    private readonly IConfiguration _configuration;

    public UtilityBillsController(
        IUtilityBillApplicationService utilityBillApplicationService,
        IRoomApplicationService roomApplicationService,
        ITenantApplicationService tenantApplicationService,
        IMaintenanceRequestApplicationService maintenanceApplicationService,
        IMapper mapper,
        IConfiguration configuration)
    {
        _utilityBillApplicationService = utilityBillApplicationService;
        _roomApplicationService = roomApplicationService;
        _tenantApplicationService = tenantApplicationService;
        _maintenanceApplicationService = maintenanceApplicationService;
        _mapper = mapper;
        _configuration = configuration;
    }

    // GET: /UtilityBills
    public async Task<IActionResult> Index()
    {
        var result = await _utilityBillApplicationService.GetAllUtilityBillsAsync();
        if (!result.IsSuccess)
        {
            SetErrorMessage(result.ErrorMessage);
            return View(new List<UtilityBillViewModel>());
        }

        var utilityBillVms = _mapper.Map<List<UtilityBillViewModel>>(result.Data);
        
        // Set utility rates for JavaScript calculations
        ViewBag.WaterRate = GetWaterRate();
        ViewBag.ElectricityRate = GetElectricityRate();
        
        // Set sidebar counts
        await SetSidebarCountsAsync();
        
        return View(utilityBillVms);
    }

    [HttpGet]
    public async Task<IActionResult> UtilityBillForm(int? id)
    {
        // Load available rooms for the dropdown
        var roomsResult = await _roomApplicationService.GetAllRoomsAsync();
        var roomList = new List<SelectListItem>();
        
        if (roomsResult.IsSuccess)
        {
            roomList = roomsResult.Data.Select(r => new SelectListItem
            {
                Value = r.RoomId.ToString(),
                Text = $"Room {r.Number} ({r.Type})"
            }).ToList();
        }

        UtilityBillFormViewModel utilityBillVm;

        if (id.HasValue)
        {
            // Editing existing utility bill
            var result = await _utilityBillApplicationService.GetUtilityBillByIdAsync(id.Value);
            if (!result.IsSuccess)
            {
                SetErrorMessage(result.ErrorMessage);
                return PartialView("_UtilityBillForm", new UtilityBillFormViewModel 
                { 
                    RoomOptions = roomList,
                    WaterRate = GetWaterRate(),
                    ElectricityRate = GetElectricityRate()
                });
            }
            
            utilityBillVm = _mapper.Map<UtilityBillFormViewModel>(result.Data);
        }
        else
        {
            // Creating new utility bill
            utilityBillVm = new UtilityBillFormViewModel
            {
                BillingDate = DateTime.Today
            };
        }
        
        utilityBillVm.RoomOptions = roomList;
        utilityBillVm.WaterRate = GetWaterRate();
        utilityBillVm.ElectricityRate = GetElectricityRate();

        return PartialView("_UtilityBillForm", utilityBillVm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateOrEdit(UtilityBillFormViewModel utilityBillVm)
    {
        bool isAjax = Request.Headers["X-Requested-With"] == "XMLHttpRequest";
        
        // Calculate total amount based on usage and rates
        utilityBillVm.TotalAmount = (utilityBillVm.WaterUsage * GetWaterRate()) + 
                                    (utilityBillVm.ElectricityUsage * GetElectricityRate());
        
        // Add available rooms for dropdown in case of validation error
        var roomsResult = await _roomApplicationService.GetAllRoomsAsync();
        var roomList = new List<SelectListItem>();
        
        if (roomsResult.IsSuccess)
        {
            roomList = roomsResult.Data.Select(r => new SelectListItem
            {
                Value = r.RoomId.ToString(),
                Text = $"Room {r.Number} ({r.Type})"
            }).ToList();
        }
        
        utilityBillVm.RoomOptions = roomList;
        utilityBillVm.WaterRate = GetWaterRate();
        utilityBillVm.ElectricityRate = GetElectricityRate();

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
            return PartialView("_UtilityBillForm", utilityBillVm);
        }

        try
        {
            if (utilityBillVm.UtilityBillId == 0)
            {
                // Create new utility bill
                var createUtilityBillDto = new CreateUtilityBillDto
                {
                    RoomId = utilityBillVm.RoomId,
                    BillingDate = utilityBillVm.BillingDate,
                    WaterUsage = utilityBillVm.WaterUsage,
                    ElectricityUsage = utilityBillVm.ElectricityUsage,
                    TotalAmount = utilityBillVm.TotalAmount,
                    Notes = utilityBillVm.Notes
                };

                var result = await _utilityBillApplicationService.CreateUtilityBillAsync(createUtilityBillDto);
                if (!result.IsSuccess)
                {
                    if (isAjax)
                    {
                        return Json(new { success = false, message = result.ErrorMessage });
                    }
                    
                    SetErrorMessage($"Failed to create utility bill: {result.ErrorMessage}");
                    return PartialView("_UtilityBillForm", utilityBillVm);
                }

                if (isAjax)
                {
                    return Json(new { success = true, message = "Utility bill created successfully!" });
                }

                SetSuccessMessage("Utility bill created successfully!");
            }
            else
            {
                // Update existing utility bill
                var updateUtilityBillDto = new UpdateUtilityBillDto
                {
                    BillingDate = utilityBillVm.BillingDate,
                    WaterUsage = utilityBillVm.WaterUsage,
                    ElectricityUsage = utilityBillVm.ElectricityUsage,
                    TotalAmount = utilityBillVm.TotalAmount,
                    Notes = utilityBillVm.Notes
                };

                var result = await _utilityBillApplicationService.UpdateUtilityBillAsync(utilityBillVm.UtilityBillId, updateUtilityBillDto);
                if (!result.IsSuccess)
                {
                    if (isAjax)
                    {
                        return Json(new { success = false, message = result.ErrorMessage });
                    }
                    
                    SetErrorMessage($"Failed to update utility bill: {result.ErrorMessage}");
                    return PartialView("_UtilityBillForm", utilityBillVm);
                }

                if (isAjax)
                {
                    return Json(new { success = true, message = "Utility bill updated successfully!" });
                }

                SetSuccessMessage("Utility bill updated successfully!");
            }
        }
        catch (Exception ex)
        {
            if (isAjax)
            {
                return Json(new { success = false, message = $"An unexpected error occurred: {ex.Message}" });
            }
            
            SetErrorMessage($"An unexpected error occurred: {ex.Message}");
            return PartialView("_UtilityBillForm", utilityBillVm);
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        // Get utility bill details before deletion for better messaging
        var utilityBillResult = await _utilityBillApplicationService.GetUtilityBillByIdAsync(id);
        string utilityBillInfo = "Utility bill";
        
        if (utilityBillResult.IsSuccess && utilityBillResult.Data != null)
        {
            var roomNumber = utilityBillResult.Data.Room?.Number ?? "Unknown";
            var billingDate = utilityBillResult.Data.BillingDate.ToShortDateString();
            utilityBillInfo = $"Utility bill for Room {roomNumber} ({billingDate})";
        }

        var result = await _utilityBillApplicationService.DeleteUtilityBillAsync(id);
        if (!result.IsSuccess)
        {
            SetErrorMessage($"Failed to delete utility bill: {result.ErrorMessage}");
        }
        else
        {
            SetSuccessMessage($"{utilityBillInfo} deleted successfully.");
        }

        return RedirectToAction("Index");
    }

    private decimal GetWaterRate()
    {
        return _configuration.GetSection("UtilityRates").GetValue<decimal>("WaterPerLiter", 0.02m);
    }

    private decimal GetElectricityRate()
    {
        return _configuration.GetSection("UtilityRates").GetValue<decimal>("ElectricityPerKwh", 1.50m);
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