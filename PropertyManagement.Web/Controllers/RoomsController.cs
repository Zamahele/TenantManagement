using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PropertyManagement.Domain.Entities;
using PropertyManagement.Infrastructure.Data;
using PropertyManagement.Infrastructure.Repositories;
using PropertyManagement.Web.Controllers;
using PropertyManagement.Web.ViewModels;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

[Authorize]
[Authorize(Roles = "Manager")]
public class RoomsController : BaseController
{
  private readonly IGenericRepository<Room> _roomRepository;
  private readonly IGenericRepository<BookingRequest> _bookingRepository;
  private readonly IGenericRepository<Tenant> _tenantRepository;
  private readonly ApplicationDbContext _context;
  private readonly IMapper _mapper;

  public RoomsController(
    IGenericRepository<Room> roomRepository,
    IGenericRepository<BookingRequest> bookingRepository,
    IGenericRepository<Tenant> tenantRepository,
    ApplicationDbContext context,
    IMapper mapper)
  {
    _roomRepository = roomRepository;
    _bookingRepository = bookingRepository;
    _tenantRepository = tenantRepository;
    _context = context;
    _mapper = mapper;
  }

  // GET: /Rooms
  public async Task<IActionResult> Index()
  {
    // Get all rooms with tenants in one query
    var allRooms = await _roomRepository.GetAllAsync(null, r => r.Tenants);
    
    // Get confirmed booking room IDs
    var confirmedBookings = await _bookingRepository.GetAllAsync(b => b.Status == "Confirmed");
    var confirmedRoomIds = confirmedBookings.Select(b => b.RoomId).ToHashSet();

    // Filter rooms by status at database level
    var occupiedRooms = await _roomRepository.GetAllAsync(r => r.Status == "Occupied", r => r.Tenants);
    var maintenanceRooms = await _roomRepository.GetAllAsync(r => r.Status == "Under Maintenance", r => r.Tenants);
    
    // Available rooms excluding confirmed bookings
    var availableRooms = allRooms
        .Where(r => r.Status == "Available" && !confirmedRoomIds.Contains(r.RoomId))
        .ToList();

    // Get pending booking requests
    var pendingRequests = await _bookingRepository.GetAllAsync(b => b.Status == "Pending", b => b.Room);

    var model = new RoomsTabViewModel
    {
      AllRooms = _mapper.Map<List<RoomViewModel>>(allRooms),
      OccupiedRooms = _mapper.Map<List<RoomViewModel>>(occupiedRooms),
      VacantRooms = _mapper.Map<List<RoomViewModel>>(availableRooms),
      MaintenanceRooms = _mapper.Map<List<RoomViewModel>>(maintenanceRooms),
      PendingBookingRequests = _mapper.Map<List<BookingRequestViewModel>>(pendingRequests.ToList()),
      PendingRequestCount = pendingRequests.Count()
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
    var room = await _roomRepository.GetByIdAsync(id);
    if (room == null) return NotFound();

    var model = _mapper.Map<RoomFormViewModel>(room);
    model.StatusOptions = GetStatusOptions();
    return View("CreateOrEdit", model);
  }

  // POST: /Rooms/CreateOrEdit
  [HttpPost]
  [ValidateAntiForgeryToken]
  [Authorize(Roles = "Manager")]
  public async Task<IActionResult> CreateOrEdit(RoomFormViewModel model)
  {
    try
    {
      if (!ModelState.IsValid)
      {
        model.StatusOptions = GetStatusOptions();
        SetErrorMessage("Please correct the errors in the form.");
        return View("_RoomModal", model);
      }

      // Check for duplicate room number
      var existingRooms = await _roomRepository.GetAllAsync(r => r.Number == model.Number && r.RoomId != model.RoomId);
      if (existingRooms.Any())
      {
        ModelState.AddModelError(nameof(model.Number), "A room with this number already exists.");
        model.StatusOptions = GetStatusOptions();
        SetErrorMessage("Room number already exists.");
        return View("_RoomModal", model);
      }

      Room room;
      if (model.RoomId == 0)
      {
        room = _mapper.Map<Room>(model);
        await _roomRepository.AddAsync(room);
        SetSuccessMessage("Room created successfully.");
      }
      else
      {
        room = await _roomRepository.GetByIdAsync(model.RoomId);
        if (room == null)
        {
          SetErrorMessage("Room not found.");
          return NotFound();
        }
        _mapper.Map(model, room);
        await _roomRepository.UpdateAsync(room);
        SetSuccessMessage("Room updated successfully.");
      }
      return RedirectToAction(nameof(Index));
    }
    catch (Exception ex)
    {
      model.StatusOptions = GetStatusOptions();
      SetErrorMessage("Error saving room: " + ex.Message);
      return View("_RoomModal", model);
    }
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
    var rooms = await _roomRepository.GetAllAsync(r => r.RoomId == id, r => r.Tenants);
    var room = rooms.FirstOrDefault();
    if (room == null) return NotFound();

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
    try
    {
      var room = await _context.Rooms.FindAsync(id);
      if (room == null)
      {
        SetErrorMessage("Room not found.");
        return NotFound();
      }

      // Check if room has active bookings or tenants
      var hasActiveBookings = await _context.BookingRequests
          .AnyAsync(b => b.RoomId == id && b.Status == "Confirmed");
      var hasTenants = await _context.Tenants
          .AnyAsync(t => t.RoomId == id);

      if (hasActiveBookings || hasTenants)
      {
        SetErrorMessage("Cannot delete room. It has active bookings or tenants.");
        return RedirectToAction(nameof(Index));
      }

      _context.Rooms.Remove(room);
      await _context.SaveChangesAsync();
      SetSuccessMessage("Room deleted successfully.");
    }
    catch (Exception ex)
    {
      SetErrorMessage("Error deleting room: " + ex.Message);
    }
    
    return RedirectToAction(nameof(Index));
  }

  // GET: /Rooms/BookRoom
  [Authorize(Roles = "Manager")]
  public async Task<IActionResult> BookRoom(int roomId)
  {
    var room = await _roomRepository.GetByIdAsync(roomId);
    if (room == null) return NotFound();

    var availableRooms = await _roomRepository.GetAllAsync(r => r.Status == "Available");
    var roomOptions = availableRooms.Select(r => new SelectListItem
    {
      Value = r.RoomId.ToString(),
      Text = $"Room {r.Number} - {r.Type}",
      Selected = r.RoomId == roomId
    }).ToList();

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
    try
    {
      if (!ModelState.IsValid)
      {
        var availableRooms = await _context.Rooms
            .Where(r => r.Status == "Available")
            .Select(r => new SelectListItem
            {
              Value = r.RoomId.ToString(),
              Text = $"Room {r.Number} - {r.Type}"
            })
            .ToListAsync();
        model.RoomOptions = availableRooms;
        
        SetErrorMessage("Please correct the errors in the form.");
        return PartialView("_BookingModal", model);
      }

      var bookingRequest = new BookingRequest
      {
        RoomId = model.RoomId,
        FullName = model.FullName,
        Contact = model.Contact,
        RequestDate = DateTime.Now,
        Status = "Pending",
        Note = model.Note,
        DepositPaid = model.DepositPaid
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
        
        bookingRequest.ProofOfPaymentPath = $"uploads/proofs/{fileName}";
      }

      _context.BookingRequests.Add(bookingRequest);
      await _context.SaveChangesAsync();
      
      SetSuccessMessage("Booking request submitted successfully.");
      return RedirectToAction(nameof(Index));
    }
    catch (Exception ex)
    {
      SetErrorMessage("Error submitting booking request: " + ex.Message);
      return RedirectToAction(nameof(Index));
    }
  }

  // GET: /Rooms/EditBookingRequest
  public async Task<IActionResult> EditBookingRequest(int bookingRequestId)
  {
    var bookingRequest = await _context.BookingRequests
        .Include(b => b.Room)
        .FirstOrDefaultAsync(b => b.BookingRequestId == bookingRequestId);
    
    if (bookingRequest == null) return NotFound();

    var availableRooms = await _context.Rooms
        .Where(r => r.Status == "Available")
        .Select(r => new SelectListItem
        {
          Value = r.RoomId.ToString(),
          Text = $"Room {r.Number} - {r.Type}",
          Selected = r.RoomId == bookingRequest.RoomId
        })
        .ToListAsync();

    var model = new BookingRequestViewModel
    {
      BookingRequestId = bookingRequest.BookingRequestId,
      RoomId = bookingRequest.RoomId,
      FullName = bookingRequest.FullName,
      Contact = bookingRequest.Contact,
      RequestDate = bookingRequest.RequestDate,
      Status = bookingRequest.Status,
      Note = bookingRequest.Note,
      DepositPaid = bookingRequest.DepositPaid,
      ProofOfPaymentPath = bookingRequest.ProofOfPaymentPath,
      RoomOptions = availableRooms
    };

    return PartialView("_BookingModal", model);
  }

  // POST: /Rooms/EditBookingRequest
  [HttpPost]
  [ValidateAntiForgeryToken]
  public async Task<IActionResult> EditBookingRequest(BookingRequestViewModel model, IFormFile? ProofOfPayment)
  {
    try
    {
      if (!ModelState.IsValid)
      {
        var availableRooms = await _context.Rooms
            .Where(r => r.Status == "Available")
            .Select(r => new SelectListItem
            {
              Value = r.RoomId.ToString(),
              Text = $"Room {r.Number} - {r.Type}"
            })
            .ToListAsync();
        model.RoomOptions = availableRooms;
        
        SetErrorMessage("Please correct the errors in the form.");
        return PartialView("_BookingModal", model);
      }

      var bookingRequest = await _context.BookingRequests.FindAsync(model.BookingRequestId);
      if (bookingRequest == null)
      {
        SetErrorMessage("Booking request not found.");
        return NotFound();
      }

      // Update booking request
      bookingRequest.RoomId = model.RoomId;
      bookingRequest.FullName = model.FullName;
      bookingRequest.Contact = model.Contact;
      bookingRequest.Note = model.Note;
      bookingRequest.DepositPaid = model.DepositPaid;

      // Handle file upload
      if (ProofOfPayment != null && ProofOfPayment.Length > 0)
      {
        // Delete old file if exists
        if (!string.IsNullOrEmpty(bookingRequest.ProofOfPaymentPath))
        {
          var oldFilePath = Path.Combine("wwwroot", bookingRequest.ProofOfPaymentPath);
          if (System.IO.File.Exists(oldFilePath))
          {
            System.IO.File.Delete(oldFilePath);
          }
        }

        var uploadsFolder = Path.Combine("wwwroot", "uploads", "proofs");
        Directory.CreateDirectory(uploadsFolder);
        
        var fileName = $"{Guid.NewGuid()}_{ProofOfPayment.FileName}";
        var filePath = Path.Combine(uploadsFolder, fileName);
        
        using var stream = new FileStream(filePath, FileMode.Create);
        await ProofOfPayment.CopyToAsync(stream);
        
        bookingRequest.ProofOfPaymentPath = $"uploads/proofs/{fileName}";
      }

      await _context.SaveChangesAsync();
      SetSuccessMessage("Booking request updated successfully.");
      return RedirectToAction(nameof(Index));
    }
    catch (Exception ex)
    {
      SetErrorMessage("Error updating booking request: " + ex.Message);
      return RedirectToAction(nameof(Index));
    }
  }

  // POST: /Rooms/DeleteBookingRequest
  [HttpPost]
  [ValidateAntiForgeryToken]
  public async Task<IActionResult> DeleteBookingRequest(int bookingRequestId)
  {
    try
    {
      var booking = await _context.BookingRequests.FindAsync(bookingRequestId);
      if (booking == null)
      {
        SetErrorMessage("Booking request not found.");
        return NotFound();
      }

      // Delete associated file if exists
      if (!string.IsNullOrEmpty(booking.ProofOfPaymentPath))
      {
        var filePath = Path.Combine("wwwroot", booking.ProofOfPaymentPath);
        if (System.IO.File.Exists(filePath))
        {
          System.IO.File.Delete(filePath);
        }
      }

      _context.BookingRequests.Remove(booking);
      await _context.SaveChangesAsync();
      SetSuccessMessage("Booking request deleted successfully.");
    }
    catch (Exception ex)
    {
      SetErrorMessage("Error deleting booking request: " + ex.Message);
    }
    
    return RedirectToAction(nameof(Index));
  }
}