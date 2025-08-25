using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using PropertyManagement.Application.DTOs;
using PropertyManagement.Application.Services;
using PropertyManagement.Web.Controllers;
using PropertyManagement.Web.ViewModels;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PropertyManagement.Web.Controllers
{
  [Authorize]
  [Authorize(Roles = "Manager")]
  public class InspectionsController : BaseController
  {
    private readonly IInspectionApplicationService _inspectionApplicationService;
    private readonly IRoomApplicationService _roomApplicationService;
    private readonly IMapper _mapper;

    public InspectionsController(
        IInspectionApplicationService inspectionApplicationService,
        IRoomApplicationService roomApplicationService,
        IMapper mapper)
    {
      _inspectionApplicationService = inspectionApplicationService;
      _roomApplicationService = roomApplicationService;
      _mapper = mapper;
    }

    // GET: /Inspections
    public async Task<IActionResult> Index()
    {
      var result = await _inspectionApplicationService.GetAllInspectionsAsync();
      if (!result.IsSuccess)
      {
        SetErrorMessage(result.ErrorMessage);
        return View(new List<InspectionViewModel>());
      }

      var orderedInspections = result.Data.OrderByDescending(i => i.Date).ToList();
      var inspectionVms = _mapper.Map<List<InspectionViewModel>>(orderedInspections);
      return View(inspectionVms);
    }

    // GET: /Inspections/Create
    public async Task<IActionResult> Create()
    {
      var roomsResult = await _roomApplicationService.GetAllRoomsAsync();
      if (roomsResult.IsSuccess)
      {
        ViewBag.Rooms = new SelectList(roomsResult.Data, "RoomId", "Number");
      }
      else
      {
        ViewBag.Rooms = new SelectList(new List<RoomDto>(), "RoomId", "Number");
      }
      
      var model = new InspectionViewModel();
      return PartialView("_InspectionModal", model);
    }

    // GET: /Inspections/Edit/5
    public async Task<IActionResult> Edit(int id)
    {
      var inspectionResult = await _inspectionApplicationService.GetInspectionByIdAsync(id);
      if (!inspectionResult.IsSuccess)
      {
        SetErrorMessage(inspectionResult.ErrorMessage);
        return NotFound();
      }

      var roomsResult = await _roomApplicationService.GetAllRoomsAsync();
      if (roomsResult.IsSuccess)
      {
        ViewBag.Rooms = new SelectList(roomsResult.Data, "RoomId", "Number", inspectionResult.Data.RoomId);
      }
      else
      {
        ViewBag.Rooms = new SelectList(new List<RoomDto>(), "RoomId", "Number");
      }

      var model = _mapper.Map<InspectionViewModel>(inspectionResult.Data);
      return PartialView("_InspectionModal", model);
    }

    // GET: /Inspections/InspectionForm
    [HttpGet]
    public async Task<IActionResult> InspectionForm(int? id = null)
    {
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

      InspectionViewModel model;

      if (id.HasValue)
      {
        // Editing existing inspection
        var result = await _inspectionApplicationService.GetInspectionByIdAsync(id.Value);
        if (!result.IsSuccess)
        {
          SetErrorMessage(result.ErrorMessage);
          return PartialView("_InspectionForm", new InspectionViewModel { RoomOptions = roomList });
        }
        
        model = _mapper.Map<InspectionViewModel>(result.Data);
      }
      else
      {
        // Creating new inspection
        model = new InspectionViewModel
        {
          Date = DateTime.Today,
          Result = "Pending"
        };
      }
      
      model.RoomOptions = roomList;
      return PartialView("_InspectionForm", model);
    }

    // GET: Modal for Add/Edit (Legacy - keeping for compatibility)
    [HttpGet]
    public async Task<IActionResult> InspectionModal(int? id = null)
    {
      if (id.HasValue)
      {
        return await Edit(id.Value);
      }
      else
      {
        return await Create();
      }
    }

    // POST: /Inspections/CreateOrEdit
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateOrEdit(InspectionViewModel model)
    {
        // Ensure model is not null
        model ??= new InspectionViewModel();
        
        // Initialize RoomOptions to prevent null reference
        model.RoomOptions ??= new List<SelectListItem>();
        
        bool isAjax = false;
        try
        {
            var requestedWith = Request?.Headers?["X-Requested-With"];
            isAjax = requestedWith.HasValue && requestedWith.Value == "XMLHttpRequest";
        }
        catch
        {
            // If Request or Headers is null, default to false
            isAjax = false;
        }
        
        // Try to populate room options
        try
        {
            if (_roomApplicationService != null)
            {
                var roomsResult = await _roomApplicationService.GetAllRoomsAsync();
                if (roomsResult?.IsSuccess == true && roomsResult.Data != null)
                {
                    model.RoomOptions = roomsResult.Data
                        .Where(r => r != null)
                        .Select(r => new SelectListItem
                        {
                            Value = r.RoomId.ToString(),
                            Text = $"Room {r.Number ?? "Unknown"} ({r.Type ?? "Unknown"})"
                        }).ToList();
                }
            }
        }
        catch
        {
            // Ignore room service errors
        }
        
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
            return PartialView("_InspectionForm", model);
        }
        
        try
        {
            if (_mapper == null)
            {
                SetErrorMessage("Mapper service not available.");
                return PartialView("_InspectionForm", model);
            }
            
            if (_inspectionApplicationService == null)
            {
                SetErrorMessage("Inspection service not available.");
                return PartialView("_InspectionForm", model);
            }
            
            if (model.InspectionId == 0)
            {
                var createInspectionDto = _mapper.Map<CreateInspectionDto>(model);
                var result = await _inspectionApplicationService.CreateInspectionAsync(createInspectionDto);
                if (!result.IsSuccess)
                {
                    if (isAjax)
                    {
                        return Json(new { success = false, message = result.ErrorMessage });
                    }
                    SetErrorMessage($"Failed to create inspection: {result.ErrorMessage}");
                    return PartialView("_InspectionForm", model);
                }
                if (isAjax)
                {
                    return Json(new { success = true, message = "Inspection created successfully." });
                }
                SetSuccessMessage("Inspection created successfully.");
            }
            else
            {
                var updateInspectionDto = _mapper.Map<UpdateInspectionDto>(model);
                var result = await _inspectionApplicationService.UpdateInspectionAsync(model.InspectionId, updateInspectionDto);
                if (!result.IsSuccess)
                {
                    if (isAjax)
                    {
                        return Json(new { success = false, message = result.ErrorMessage });
                    }
                    SetErrorMessage($"Failed to update inspection: {result.ErrorMessage}");
                    return PartialView("_InspectionForm", model);
                }
                if (isAjax)
                {
                    return Json(new { success = true, message = "Inspection updated successfully." });
                }
                SetSuccessMessage("Inspection updated successfully.");
            }
        }
        catch (Exception ex)
        {
            if (isAjax)
            {
                return Json(new { success = false, message = $"An unexpected error occurred: {ex.Message}" });
            }
            SetErrorMessage($"An unexpected error occurred: {ex.Message}");
            return PartialView("_InspectionForm", model);
        }
        return RedirectToAction(nameof(Index));
    }

    // POST: Save Add/Edit (Legacy - keeping for compatibility)
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveInspection(InspectionViewModel model)
    {
      return await CreateOrEdit(model);
    }

    // POST: Delete
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
      try
      {
        var result = await _inspectionApplicationService.DeleteInspectionAsync(id);
        if (!result.IsSuccess)
        {
          SetErrorMessage(result.ErrorMessage);
          return RedirectToAction(nameof(Index));
        }

        SetSuccessMessage("Inspection deleted successfully.");
      }
      catch (Exception ex)
      {
        SetErrorMessage("Error deleting inspection: " + ex.Message);
      }
      return RedirectToAction(nameof(Index));
    }

    private async Task<PartialViewResult> PrepareInspectionModal(InspectionViewModel model)
    {
      var roomsResult = await _roomApplicationService.GetAllRoomsAsync();
      if (roomsResult.IsSuccess)
      {
        ViewBag.Rooms = new SelectList(roomsResult.Data, "RoomId", "Number", model.RoomId);
      }
      else
      {
        ViewBag.Rooms = new SelectList(new List<RoomDto>(), "RoomId", "Number");
      }
      
      return PartialView("_InspectionModal", model);
    }
  }
}