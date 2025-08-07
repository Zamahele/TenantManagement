using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using PropertyManagement.Application.DTOs;
using PropertyManagement.Application.Services;
using PropertyManagement.Web.Controllers;
using PropertyManagement.Web.ViewModels;

[Authorize]
[Authorize(Roles = "Manager")]
public class RoomsController : BaseController
{
  private readonly IRoomApplicationService _roomApplicationService;
  private readonly IBookingRequestApplicationService _bookingRequestApplicationService;
  private readonly ITenantApplicationService _tenantApplicationService;
  private readonly IMaintenanceRequestApplicationService _maintenanceApplicationService;
  private readonly IMapper _mapper;

  public RoomsController(
    IRoomApplicationService roomApplicationService,
    IBookingRequestApplicationService bookingRequestApplicationService,
    ITenantApplicationService tenantApplicationService,
    IMaintenanceRequestApplicationService maintenanceApplicationService,
    IMapper mapper)
  {
    _roomApplicationService = roomApplicationService;
    _bookingRequestApplicationService = bookingRequestApplicationService;
    _tenantApplicationService = tenantApplicationService;
    _maintenanceApplicationService = maintenanceApplicationService;
    _mapper = mapper;
  }

  // GET: /Rooms
  public async Task<IActionResult> Index()
  {
    var allRoomsResult = await _roomApplicationService.GetAllRoomsWithTenantsAsync();
    if (!allRoomsResult.IsSuccess)
    {
      SetErrorMessage(allRoomsResult.ErrorMessage);
      return View(new RoomsTabViewModel());
    }

    var occupiedRoomsResult = await _roomApplicationService.GetOccupiedRoomsAsync();
    var availableRoomsResult = await _roomApplicationService.GetAvailableRoomsAsync();
    var pendingRequestsResult = await _bookingRequestApplicationService.GetPendingBookingRequestsAsync();
    var confirmedBookingsResult = await _bookingRequestApplicationService.GetConfirmedBookingRequestsAsync();

    var allRooms = allRoomsResult.Data.ToList();
    var confirmedRoomIds = confirmedBookingsResult.IsSuccess ? 
        confirmedBookingsResult.Data.Select(b => b.RoomId).ToHashSet() : 
        new HashSet<int>();

    // Available rooms excluding confirmed bookings
    var availableRooms = allRooms
        .Where(r => r.Status == "Available" && !confirmedRoomIds.Contains(r.RoomId))
        .ToList();

    var maintenanceRooms = allRooms
        .Where(r => r.Status == "Under Maintenance")
        .ToList();

    var model = new RoomsTabViewModel
    {
      AllRooms = _mapper.Map<List<RoomViewModel>>(allRooms),
      OccupiedRooms = occupiedRoomsResult.IsSuccess ? _mapper.Map<List<RoomViewModel>>(occupiedRoomsResult.Data) : new List<RoomViewModel>(),
      VacantRooms = _mapper.Map<List<RoomViewModel>>(availableRooms),
      MaintenanceRooms = _mapper.Map<List<RoomViewModel>>(maintenanceRooms),
      PendingBookingRequests = pendingRequestsResult.IsSuccess ? _mapper.Map<List<BookingRequestViewModel>>(pendingRequestsResult.Data) : new List<BookingRequestViewModel>(),
      PendingRequestCount = pendingRequestsResult.IsSuccess ? pendingRequestsResult.Data.Count() : 0,
      StatusOptions = GetStatusOptions().Where(s => s.Value == "Available").ToList(),
      RoomTypes = new List<SelectListItem>
      {
        new SelectListItem { Value = "Single", Text = "Single" },
        new SelectListItem { Value = "Double", Text = "Double" }
      }
    };
    
    // Set sidebar counts
    await SetSidebarCountsAsync();
    
    return View(model);
  }

  // GET: /Rooms/Create
  public IActionResult Create()
  {
    var model = new RoomFormViewModel
    {
      StatusOptions = GetStatusOptions().Where(s => s.Value == "Available").ToList()
    };
    return View("CreateOrEdit", model);
  }

  // GET: /Rooms/Edit/5
  public async Task<IActionResult> Edit(int id)
  {
    var result = await _roomApplicationService.GetRoomByIdAsync(id);
    if (!result.IsSuccess)
    {
      SetErrorMessage(result.ErrorMessage);
      return NotFound();
    }

    var model = _mapper.Map<RoomFormViewModel>(result.Data);
    model.StatusOptions = GetStatusOptions();
    return View("CreateOrEdit", model);
  }

  // POST: /Rooms/CreateOrEdit
  [HttpPost]
  [ValidateAntiForgeryToken]
  [Authorize(Roles = "Manager")]
  public async Task<IActionResult> CreateOrEdit(RoomFormViewModel model)
  {
    // Add RoomTypes options for the dropdown
    model.RoomTypes = new List<SelectListItem>
    {
        new SelectListItem { Value = "Single", Text = "Single" },
        new SelectListItem { Value = "Double", Text = "Double" },
        new SelectListItem { Value = "Suite", Text = "Suite" }
    };

    if (!ModelState.IsValid)
    {
      model.StatusOptions = GetStatusOptions();
      
      // Create detailed error message based on which fields failed validation
      var errorMessages = new List<string>();
      
      if (ModelState["Number"]?.Errors.Any() == true)
      {
        var numberErrors = ModelState["Number"].Errors.Select(e => e.ErrorMessage);
        errorMessages.AddRange(numberErrors);
      }
      
      if (ModelState["Type"]?.Errors.Any() == true)
      {
        var typeErrors = ModelState["Type"].Errors.Select(e => e.ErrorMessage);
        errorMessages.AddRange(typeErrors);
      }
      
      if (ModelState["Status"]?.Errors.Any() == true)
      {
        var statusErrors = ModelState["Status"].Errors.Select(e => e.ErrorMessage);
        errorMessages.AddRange(statusErrors);
      }
      
      // Set a comprehensive error message
      if (errorMessages.Any())
      {
        SetErrorMessage($"Validation failed: {string.Join(" ", errorMessages)}");
      }
      else
      {
        SetErrorMessage("Please correct the errors in the form before saving.");
      }
      
      // Handle AJAX requests for validation errors
      if (Request.Headers.ContainsKey("X-Requested-With") && Request.Headers["X-Requested-With"] == "XMLHttpRequest")
      {
        return Json(new { success = false, errors = errorMessages.Any() ? errorMessages : new List<string> { "Please correct the errors in the form before saving." } });
      }
      
      // Log the validation errors for debugging
      foreach (var modelState in ModelState)
      {
        foreach (var error in modelState.Value.Errors)
        {
          // This can help with debugging in development
          System.Diagnostics.Debug.WriteLine($"Validation error for {modelState.Key}: {error.ErrorMessage}");
        }
      }
      
      return View("_RoomModal", model);
    }

    try
    {
      if (model.RoomId == 0)
      {
        var createRoomDto = _mapper.Map<CreateRoomDto>(model);
        var result = await _roomApplicationService.CreateRoomAsync(createRoomDto);
        
        if (!result.IsSuccess)
        {
          model.StatusOptions = GetStatusOptions();
          
          // Check if the error is related to duplicate room number
          if (result.ErrorMessage.Contains("duplicate") || result.ErrorMessage.Contains("already exists"))
          {
            ModelState.AddModelError("Number", $"Room number '{model.Number}' is already taken - please choose a different number");
            SetErrorMessage($"Cannot create room: Room number '{model.Number}' already exists. Please use a different room number.");
          }
          else
          {
            SetErrorMessage($"Failed to create room: {result.ErrorMessage}");
          }
          
          // Handle AJAX requests
          if (Request.Headers.ContainsKey("X-Requested-With") && Request.Headers["X-Requested-With"] == "XMLHttpRequest")
          {
            var errors = ModelState
              .Where(x => x.Value.Errors.Count > 0)
              .SelectMany(x => x.Value.Errors)
              .Select(x => x.ErrorMessage)
              .ToList();
            
            if (!errors.Any())
              errors.Add(result.ErrorMessage);
              
            return Json(new { success = false, errors = errors });
          }
          
          return View("_RoomModal", model);
        }
        
        SetSuccessMessage($"Room '{model.Number}' created successfully!");
      }
      else
      {
        var updateRoomDto = _mapper.Map<UpdateRoomDto>(model);
        var result = await _roomApplicationService.UpdateRoomAsync(model.RoomId, updateRoomDto);
        
        if (!result.IsSuccess)
        {
          model.StatusOptions = GetStatusOptions();
          
          // Check if the error is related to duplicate room number
          if (result.ErrorMessage.Contains("duplicate") || result.ErrorMessage.Contains("already exists"))
          {
            ModelState.AddModelError("Number", $"Room number '{model.Number}' is already used by another room - please choose a different number");
            SetErrorMessage($"Cannot update room: Room number '{model.Number}' is already in use. Please choose a different room number.");
          }
          else
          {
            SetErrorMessage($"Failed to update room: {result.ErrorMessage}");
          }
          
          // Handle AJAX requests for update errors
          if (Request.Headers.ContainsKey("X-Requested-With") && Request.Headers["X-Requested-With"] == "XMLHttpRequest")
          {
            var errors = ModelState
              .Where(x => x.Value.Errors.Count > 0)
              .SelectMany(x => x.Value.Errors)
              .Select(x => x.ErrorMessage)
              .ToList();
            
            if (!errors.Any())
              errors.Add(result.ErrorMessage);
              
            return Json(new { success = false, errors = errors });
          }
          
          return View("_RoomModal", model);
        }
        
        SetSuccessMessage($"Room '{model.Number}' updated successfully!");
      }
      
      // Handle AJAX requests for success
      if (Request.Headers.ContainsKey("X-Requested-With") && Request.Headers["X-Requested-With"] == "XMLHttpRequest")
      {
        return Json(new { success = true, message = TempData["SuccessMessage"]?.ToString() ?? "Operation completed successfully." });
      }
    }
    catch (Exception ex)
    {
      model.StatusOptions = GetStatusOptions();
      SetErrorMessage($"An unexpected error occurred: {ex.Message}");
      
      // Handle AJAX requests for exceptions
      if (Request.Headers.ContainsKey("X-Requested-With") && Request.Headers["X-Requested-With"] == "XMLHttpRequest")
      {
        return Json(new { success = false, errors = new List<string> { ex.Message } });
      }
      
      return View("_RoomModal", model);
    }
    
    return RedirectToAction(nameof(Index));
  }

  private IEnumerable<SelectListItem> GetStatusOptions()
  {
    return new List<SelectListItem>
        {
            new SelectListItem { Value = "Occupied", Text = "Occupied" },
            new SelectListItem { Value = "Available", Text = "Available" },
            new SelectListItem { Value = "Under Maintenance", Text = "Under Maintenance" }
        };
  }

  // GET: /Rooms/GetRoom/5
  public async Task<IActionResult> GetRoom(int id)
  {
    var result = await _roomApplicationService.GetRoomByIdAsync(id);
    if (!result.IsSuccess)
      return NotFound();

    var room = result.Data;
    return Json(new
    {
      roomId = room.RoomId,
      number = room.Number,
      type = room.Type,
      status = room.Status,
      cottageId = room.CottageId
    });
  }

  // POST: /Rooms/Delete/5
  [HttpPost]
  [ValidateAntiForgeryToken]
  [Authorize(Roles = "Manager")]
  public async Task<IActionResult> Delete(int id)
  {
    var result = await _roomApplicationService.DeleteRoomAsync(id);
    if (!result.IsSuccess)
    {
      SetErrorMessage(result.ErrorMessage);
      return RedirectToAction(nameof(Index));
    }

    SetSuccessMessage("Room deleted successfully.");
    return RedirectToAction(nameof(Index));
  }

  // GET: /Rooms/BookRoom
  [Authorize(Roles = "Manager")]
  public async Task<IActionResult> BookRoom(int roomId)
  {
    var roomResult = await _roomApplicationService.GetRoomByIdAsync(roomId);
    if (!roomResult.IsSuccess) return NotFound();

    var availableRoomsResult = await _roomApplicationService.GetAvailableRoomsAsync();
    var roomOptions = availableRoomsResult.IsSuccess ? 
      availableRoomsResult.Data.Select(r => new SelectListItem
      {
        Value = r.RoomId.ToString(),
        Text = $"Room {r.Number} - {r.Type}",
        Selected = r.RoomId == roomId
      }).ToList() : new List<SelectListItem>();

    var model = new BookingRequestViewModel
    {
      RoomId = roomId,
      RoomOptions = roomOptions
    };

    return PartialView("_BookingModal", model);
  }

  // POST: /Rooms/BookRoom
  [HttpPost]
  [ValidateAntiForgeryToken]
  [Authorize(Roles = "Manager")]
  public async Task<IActionResult> BookRoom(BookingRequestViewModel model, IFormFile? ProofOfPayment)
  {
    if (!ModelState.IsValid)
    {
      var availableRoomsResult = await _roomApplicationService.GetAvailableRoomsAsync();
      model.RoomOptions = availableRoomsResult.IsSuccess ? 
        availableRoomsResult.Data.Select(r => new SelectListItem
        {
          Value = r.RoomId.ToString(),
          Text = $"Room {r.Number} - {r.Type}"
        }).ToList() : new List<SelectListItem>();
      
      SetErrorMessage("Please correct the errors in the form.");
      return PartialView("_BookingModal", model);
    }

    var createBookingDto = new CreateBookingRequestDto
    {
      RoomId = model.RoomId,
      FullName = model.FullName,
      Contact = model.Contact,
      DepositPaid = model.DepositPaid,
      Note = model.Note
    };

    // Handle file upload
    if (ProofOfPayment != null && ProofOfPayment.Length > 0)
    {
      var uploadsFolder = Path.Combine("wwwroot", "uploads", "proofs");
      Directory.CreateDirectory(uploadsFolder);
      
      var fileName = $"{Guid.NewGuid()}_{ProofOfPayment.FileName}";
      var filePath = Path.Combine(uploadsFolder, fileName);
      
      using var stream = new FileStream(filePath, FileMode.Create);
      await ProofOfPayment.CopyToAsync(stream);
      
      createBookingDto.ProofOfPaymentPath = $"uploads/proofs/{fileName}";
    }

    var result = await _bookingRequestApplicationService.CreateBookingRequestAsync(createBookingDto);
    if (!result.IsSuccess)
    {
      SetErrorMessage(result.ErrorMessage);
      return RedirectToAction(nameof(Index));
    }
    
    SetSuccessMessage("Booking request submitted successfully.");
    return RedirectToAction(nameof(Index));
  }

  // GET: /Rooms/EditBookingRequest
  public async Task<IActionResult> EditBookingRequest(int bookingRequestId)
  {
    var bookingResult = await _bookingRequestApplicationService.GetBookingRequestByIdAsync(bookingRequestId);
    if (!bookingResult.IsSuccess) return NotFound();

    var availableRoomsResult = await _roomApplicationService.GetAvailableRoomsAsync();
    var roomOptions = availableRoomsResult.IsSuccess ? 
      availableRoomsResult.Data.Select(r => new SelectListItem
      {
        Value = r.RoomId.ToString(),
        Text = $"Room {r.Number} - {r.Type}",
        Selected = r.RoomId == bookingResult.Data.RoomId
      }).ToList() : new List<SelectListItem>();

    var model = _mapper.Map<BookingRequestViewModel>(bookingResult.Data);
    model.RoomOptions = roomOptions;

    return PartialView("_BookingModal", model);
  }

  // POST: /Rooms/EditBookingRequest
  [HttpPost]
  [ValidateAntiForgeryToken]
  public async Task<IActionResult> EditBookingRequest(BookingRequestViewModel model, IFormFile? ProofOfPayment)
  {
    if (!ModelState.IsValid)
    {
      var availableRoomsResult = await _roomApplicationService.GetAvailableRoomsAsync();
      model.RoomOptions = availableRoomsResult.IsSuccess ? 
        availableRoomsResult.Data.Select(r => new SelectListItem
        {
          Value = r.RoomId.ToString(),
          Text = $"Room {r.Number} - {r.Type}"
        }).ToList() : new List<SelectListItem>();
      
      SetErrorMessage("Please correct the errors in the form.");
      return PartialView("_BookingModal", model);
    }

    var updateBookingDto = new UpdateBookingRequestDto
    {
      RoomId = model.RoomId,
      FullName = model.FullName,
      Contact = model.Contact,
      DepositPaid = model.DepositPaid,
      Note = model.Note
    };

    // Handle file upload
    if (ProofOfPayment != null && ProofOfPayment.Length > 0)
    {
      var uploadsFolder = Path.Combine("wwwroot", "uploads", "proofs");
      Directory.CreateDirectory(uploadsFolder);
      
      var fileName = $"{Guid.NewGuid()}_{ProofOfPayment.FileName}";
      var filePath = Path.Combine(uploadsFolder, fileName);
      
      using var stream = new FileStream(filePath, FileMode.Create);
      await ProofOfPayment.CopyToAsync(stream);
      
      updateBookingDto.ProofOfPaymentPath = $"uploads/proofs/{fileName}";
    }

    var result = await _bookingRequestApplicationService.UpdateBookingRequestAsync(model.BookingRequestId, updateBookingDto);
    if (!result.IsSuccess)
    {
      SetErrorMessage(result.ErrorMessage);
      return RedirectToAction(nameof(Index));
    }
    
    SetSuccessMessage("Booking request updated successfully.");
    return RedirectToAction(nameof(Index));
  }

  // POST: /Rooms/DeleteBookingRequest
  [HttpPost]
  [ValidateAntiForgeryToken]
  public async Task<IActionResult> DeleteBookingRequest(int bookingRequestId)
  {
    var result = await _bookingRequestApplicationService.DeleteBookingRequestAsync(bookingRequestId);
    if (!result.IsSuccess)
    {
      SetErrorMessage(result.ErrorMessage);
      return RedirectToAction(nameof(Index));
    }
    
    SetSuccessMessage("Booking request deleted successfully.");
    return RedirectToAction(nameof(Index));
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