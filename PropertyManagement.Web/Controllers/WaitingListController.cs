using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PropertyManagement.Application.Common;
using PropertyManagement.Application.DTOs;
using PropertyManagement.Application.Services;
using PropertyManagement.Web.Controllers;
using PropertyManagement.Web.ViewModels;

namespace PropertyManagement.Web.Controllers;

[Authorize(Roles = "Manager")]
public class WaitingListController : BaseController
{
    private readonly IWaitingListApplicationService _waitingListApplicationService;
    private readonly IRoomApplicationService _roomApplicationService;
    private readonly IMapper _mapper;

    public WaitingListController(
        IWaitingListApplicationService waitingListApplicationService,
        IRoomApplicationService roomApplicationService,
        IMapper mapper)
    {
        _waitingListApplicationService = waitingListApplicationService;
        _roomApplicationService = roomApplicationService;
        _mapper = mapper;
    }

    // GET: /WaitingList
    public async Task<IActionResult> Index()
    {
        var result = await _waitingListApplicationService.GetAllWaitingListEntriesAsync();
        if (!result.IsSuccess)
        {
            SetErrorMessage(result.ErrorMessage);
            return View(new WaitingListManagementViewModel());
        }

        var summaryResult = await _waitingListApplicationService.GetWaitingListSummaryAsync();
        var summary = summaryResult.IsSuccess ? _mapper.Map<WaitingListSummaryViewModel>(summaryResult.Data) : new WaitingListSummaryViewModel();

        var viewModel = new WaitingListManagementViewModel
        {
            Entries = _mapper.Map<List<WaitingListEntryViewModel>>(result.Data),
            Summary = summary,
            TotalCount = result.Data.Count()
        };

        return View(viewModel);
    }

    // GET: /WaitingList/WaitingListForm/{id?}
    [HttpGet]
    public async Task<IActionResult> WaitingListForm(int? id)
    {
        var viewModel = new WaitingListEntryViewModel();
        
        if (id.HasValue)
        {
            var result = await _waitingListApplicationService.GetWaitingListEntryByIdAsync(id.Value);
            if (!result.IsSuccess)
            {
                return NotFound();
            }
            viewModel = _mapper.Map<WaitingListEntryViewModel>(result.Data);
        }

        return PartialView("_WaitingListForm", viewModel);
    }

    // POST: /WaitingList/WaitingListForm/{id?}
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> WaitingListForm(int? id, WaitingListEntryViewModel model)
    {

        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList();
            return Json(new { success = false, errors = errors });
        }

        ServiceResult<WaitingListEntryDto> result;

        if (id.HasValue)
        {
            // Update existing entry
            var updateDto = _mapper.Map<UpdateWaitingListEntryDto>(model);
            result = await _waitingListApplicationService.UpdateWaitingListEntryAsync(id.Value, updateDto);
        }
        else
        {
            // Create new entry
            var createDto = _mapper.Map<CreateWaitingListEntryDto>(model);
            result = await _waitingListApplicationService.CreateWaitingListEntryAsync(createDto);
        }

        if (!result.IsSuccess)
        {
            return Json(new { success = false, message = result.ErrorMessage });
        }

        var successMessage = id.HasValue ? "Waiting list entry updated successfully!" : "Waiting list entry created successfully!";
        return Json(new { success = true, message = successMessage });
    }

    // GET: /WaitingList/QuickAdd
    [HttpGet]
    public IActionResult QuickAdd()
    {
        var viewModel = new QuickAddWaitingListViewModel();
        return PartialView("_QuickAddForm", viewModel);
    }

    // POST: /WaitingList/QuickAdd
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> QuickAdd(QuickAddWaitingListViewModel model)
    {

        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList();
            return Json(new { success = false, errors = errors });
        }

        var createDto = _mapper.Map<CreateWaitingListEntryDto>(model);
        var result = await _waitingListApplicationService.CreateWaitingListEntryAsync(createDto);

        if (!result.IsSuccess)
        {
            return Json(new { success = false, message = result.ErrorMessage });
        }

        return Json(new { success = true, message = "Contact added to waiting list successfully!" });
    }

    // POST: /WaitingList/Delete
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _waitingListApplicationService.DeleteWaitingListEntryAsync(id);
        
        if (!result.IsSuccess)
        {
            SetErrorMessage(result.ErrorMessage);
        }
        else
        {
            SetSuccessMessage("Waiting list entry deleted successfully!");
        }

        return RedirectToAction(nameof(Index));
    }

    // GET: /WaitingList/Details/{id}
    public async Task<IActionResult> Details(int id)
    {
        var result = await _waitingListApplicationService.GetWaitingListEntryByIdAsync(id);
        if (!result.IsSuccess)
        {
            SetErrorMessage(result.ErrorMessage);
            return RedirectToAction(nameof(Index));
        }

        var viewModel = _mapper.Map<WaitingListEntryViewModel>(result.Data);
        return View(viewModel);
    }

    // GET: /WaitingList/Notifications/{id?}
    public async Task<IActionResult> Notifications(int? id)
    {
        ServiceResult<IEnumerable<WaitingListNotificationDto>> result;
        
        if (id.HasValue)
        {
            result = await _waitingListApplicationService.GetNotificationHistoryAsync(id.Value);
        }
        else
        {
            result = await _waitingListApplicationService.GetAllNotificationsAsync();
        }

        if (!result.IsSuccess)
        {
            SetErrorMessage(result.ErrorMessage);
            return View(new List<WaitingListNotificationViewModel>());
        }

        var viewModel = _mapper.Map<List<WaitingListNotificationViewModel>>(result.Data);
        return View(viewModel);
    }

    // POST: /WaitingList/SendNotification
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SendNotification(int waitingListId, string message, int? roomId = null)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            SetErrorMessage("Message content is required");
            return RedirectToAction(nameof(Details), new { id = waitingListId });
        }

        var result = await _waitingListApplicationService.SendNotificationAsync(waitingListId, message, roomId);
        
        if (!result.IsSuccess)
        {
            SetErrorMessage(result.ErrorMessage);
        }
        else
        {
            SetSuccessMessage("Notification sent successfully!");
        }

        return RedirectToAction(nameof(Details), new { id = waitingListId });
    }

    // POST: /WaitingList/SendBulkNotification
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SendBulkNotification(List<int> selectedIds, string message, int? roomId = null)
    {
        if (!selectedIds.Any())
        {
            SetErrorMessage("No entries selected");
            return RedirectToAction(nameof(Index));
        }

        if (string.IsNullOrWhiteSpace(message))
        {
            SetErrorMessage("Message content is required");
            return RedirectToAction(nameof(Index));
        }

        var result = await _waitingListApplicationService.SendBulkNotificationAsync(selectedIds, message, roomId);
        
        if (!result.IsSuccess)
        {
            SetErrorMessage(result.ErrorMessage);
        }
        else
        {
            SetSuccessMessage($"Bulk notification sent to {selectedIds.Count} entries successfully!");
        }

        return RedirectToAction(nameof(Index));
    }

    // GET: /WaitingList/FindMatches/{roomId}
    public async Task<IActionResult> FindMatches(int roomId)
    {
        var result = await _waitingListApplicationService.FindMatchingEntriesForRoomAsync(roomId);
        
        if (!result.IsSuccess)
        {
            SetErrorMessage(result.ErrorMessage);
            return RedirectToAction(nameof(Index));
        }

        var viewModel = _mapper.Map<List<WaitingListEntryViewModel>>(result.Data);
        
        // Get room details for context
        var roomResult = await _roomApplicationService.GetRoomByIdAsync(roomId);
        if (roomResult.IsSuccess)
        {
            ViewBag.RoomDetails = roomResult.Data;
        }

        return View("MatchingEntries", viewModel);
    }

    // GET: /WaitingList/Analytics
    public async Task<IActionResult> Analytics()
    {
        var summaryResult = await _waitingListApplicationService.GetWaitingListSummaryAsync();
        if (!summaryResult.IsSuccess)
        {
            SetErrorMessage(summaryResult.ErrorMessage);
            return View(new WaitingListSummaryViewModel());
        }

        var recentResult = await _waitingListApplicationService.GetRecentRegistrationsAsync(30);
        var recentEntries = recentResult.IsSuccess ? _mapper.Map<List<WaitingListEntryViewModel>>(recentResult.Data) : new List<WaitingListEntryViewModel>();

        var viewModel = _mapper.Map<WaitingListSummaryViewModel>(summaryResult.Data);
        ViewBag.RecentEntries = recentEntries;

        return View(viewModel);
    }

    // GET: /WaitingList/Export
    public async Task<IActionResult> Export(string format = "csv")
    {
        var result = await _waitingListApplicationService.GetAllWaitingListEntriesAsync();
        if (!result.IsSuccess)
        {
            SetErrorMessage(result.ErrorMessage);
            return RedirectToAction(nameof(Index));
        }

        var entries = _mapper.Map<List<WaitingListEntryViewModel>>(result.Data);

        // TODO: Implement actual export functionality
        // For now, return a simple CSV content
        var csvContent = "Phone Number,Full Name,Email,Room Type,Budget,Status,Registered Date\n";
        foreach (var entry in entries)
        {
            csvContent += $"\"{entry.PhoneNumber}\",\"{entry.FullName}\",\"{entry.Email}\",\"{entry.RoomTypeDisplay}\",\"{entry.BudgetFormatted}\",\"{entry.Status}\",\"{entry.RegisteredDateFormatted}\"\n";
        }

        var fileName = $"waiting-list-export-{DateTime.Now:yyyy-MM-dd}.csv";
        return File(System.Text.Encoding.UTF8.GetBytes(csvContent), "text/csv", fileName);
    }

    // POST: /WaitingList/UpdateStatus
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(int id, string status)
    {
        var getResult = await _waitingListApplicationService.GetWaitingListEntryByIdAsync(id);
        if (!getResult.IsSuccess)
        {
            SetErrorMessage("Waiting list entry not found");
            return RedirectToAction(nameof(Index));
        }

        var updateDto = _mapper.Map<UpdateWaitingListEntryDto>(getResult.Data);
        updateDto.Status = status;

        var result = await _waitingListApplicationService.UpdateWaitingListEntryAsync(id, updateDto);
        
        if (!result.IsSuccess)
        {
            SetErrorMessage(result.ErrorMessage);
        }
        else
        {
            SetSuccessMessage($"Status updated to {status} successfully!");
        }

        return RedirectToAction(nameof(Index));
    }

    // API endpoint for AJAX calls
    // GET: /WaitingList/api/entries
    [HttpGet("WaitingList/api/entries")]
    public async Task<IActionResult> GetEntries(string status = "", string roomType = "", int page = 1, int pageSize = 15)
    {
        ServiceResult<IEnumerable<WaitingListEntryDto>> result;

        if (!string.IsNullOrEmpty(status) && status != "All")
        {
            result = await _waitingListApplicationService.GetWaitingListEntriesByStatusAsync(status);
        }
        else if (!string.IsNullOrEmpty(roomType) && roomType != "All")
        {
            result = await _waitingListApplicationService.GetWaitingListEntriesByRoomTypeAsync(roomType);
        }
        else
        {
            result = await _waitingListApplicationService.GetAllWaitingListEntriesAsync();
        }

        if (!result.IsSuccess)
        {
            return Json(new { success = false, message = result.ErrorMessage });
        }

        var entries = _mapper.Map<List<WaitingListEntryViewModel>>(result.Data);
        
        // Apply pagination
        var pagedEntries = entries
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return Json(new 
        { 
            success = true, 
            data = pagedEntries,
            totalCount = entries.Count,
            currentPage = page,
            totalPages = (int)Math.Ceiling((double)entries.Count / pageSize)
        });
    }
}