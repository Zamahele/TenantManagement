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
  private readonly IMapper _mapper;

  public RoomsController(
    IRoomApplicationService roomApplicationService,
    IBookingRequestApplicationService bookingRequestApplicationService,
    IMapper mapper)
  {
    _roomApplicationService = roomApplicationService;
    _bookingRequestApplicationService = bookingRequestApplicationService;
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
      PendingRequestCount = pendingRequestsResult.IsSuccess ? pendingRequestsResult.Data.Count() : 0
    };
    return View(model);
  }

  // GET: /Rooms/Create
  public IActionResult Create()
  {
    var model = new RoomFormViewModel
    {
      StatusOptions = GetStatusOptions()
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
    if (!ModelState.IsValid)
    {
      model.StatusOptions = GetStatusOptions();
      SetErrorMessage("Please correct the errors in the form.");
      return View("_RoomModal", model);
    }

    if (model.RoomId == 0)
    {
      var createRoomDto = _mapper.Map<CreateRoomDto>(model);
      var result = await _roomApplicationService.CreateRoomAsync(createRoomDto);
      
      if (!result.IsSuccess)
      {
        model.StatusOptions = GetStatusOptions();
        SetErrorMessage(result.ErrorMessage);
        return View("_RoomModal", model);
      }
      
      SetSuccessMessage("Room created successfully.");
    }
    else
    {
      var updateRoomDto = _mapper.Map<UpdateRoomDto>(model);
      var result = await _roomApplicationService.UpdateRoomAsync(model.RoomId, updateRoomDto);
      
      if (!result.IsSuccess)
      {
        model.StatusOptions = GetStatusOptions();
        SetErrorMessage(result.ErrorMessage);
        return View("_RoomModal", model);
      }
      
      SetSuccessMessage("Room updated successfully.");
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
}