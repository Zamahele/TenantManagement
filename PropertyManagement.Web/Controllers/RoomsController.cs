using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PropertyManagement.Domain.Entities;
using PropertyManagement.Infrastructure.Data;
using PropertyManagement.Web.Controllers;
using PropertyManagement.Web.Models;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Twilio.TwiML.Voice;
using Room = PropertyManagement.Domain.Entities.Room;

[Authorize]
[Authorize(Roles = "Manager")]
public class RoomsController : BaseController
{
  private readonly ApplicationDbContext _context;

  public RoomsController(ApplicationDbContext context) => _context = context;

  // GET: /Rooms
  public async Task<IActionResult> Index()
  {
    var allRooms = await _context.Rooms.Include(x => x.Tenants).ToListAsync();
    var confirmedRoomIds = await _context.BookingRequests
        .Where(b => b.Status == "Confirmed")
        .Select(b => b.RoomId)
        .ToListAsync();

    var availableRooms = allRooms
        .Where(r => r.Status == "Available" && !confirmedRoomIds.Contains(r.RoomId))
        .ToList();

    var occupiedRooms = allRooms.Where(r => r.Status == "Occupied").ToList();
    var maintenanceRooms = allRooms.Where(r => r.Status == "Under Maintenance").ToList();

    // Get pending booking requests
    var pendingRequests = await _context.BookingRequests
        .Include(b => b.Room)
        .Where(b => b.Status == "Pending")
        .ToListAsync();

    var model = new RoomsTabViewModel
    {
        AllRooms = allRooms,
        OccupiedRooms = occupiedRooms,
        VacantRooms = availableRooms,
        MaintenanceRooms = maintenanceRooms,
        PendingBookingRequests = pendingRequests, 
        PendingRequestCount = pendingRequests.Count
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
    var room = await _context.Rooms.FindAsync(id);
    if (room == null) return NotFound();

    var model = new RoomFormViewModel
    {
      RoomId = room.RoomId,
      Number = room.Number,
      Type = room.Type,
      Status = room.Status,
      StatusOptions = GetStatusOptions()
    };
    return View("CreateOrEdit", model);
  }

  // POST: /Rooms/CreateOrEdit
  [HttpPost]
  public async Task<IActionResult> CreateOrEdit(RoomFormViewModel model)
  {
    if (!ModelState.IsValid)
    {
      model.StatusOptions = GetStatusOptions();
      SetErrorMessage("Please correct the errors in the form.");
      return View("CreateOrEdit", model);
    }

    Room room;
    if (model.RoomId == 0)
    {
      room = new Room();
      _context.Rooms.Add(room);
      SetSuccessMessage("Room created successfully.");
    }
    else
    {
      room = await _context.Rooms.FindAsync(model.RoomId);
      if (room == null)
      {
        SetErrorMessage("Room not found.");
        return NotFound();
      }
      SetSuccessMessage("Room updated successfully.");
    }

    room.Number = model.Number;
    room.Type = model.Type;
    room.Status = model.Status;

    await _context.SaveChangesAsync();
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
    var room = await _context.Rooms
        .Include(x => x.Tenants)
        .FirstOrDefaultAsync(i => i.RoomId == id);
    if (room == null) return NotFound();

    // Only return the properties needed for editing
    return Json(new
    {
      roomId = room.RoomId,
      number = room.Number,
      type = room.Type,
      status = room.Status,
      cottageId = room.CottageId
    });
  }

  // GET: /Rooms/BookRoom?roomId=5
  [HttpGet]
  public async Task<IActionResult> BookRoom(int? roomId)
  {
    var availableRooms = await _context.Rooms
        .Where(r => r.Status == "Available")
        .ToListAsync();

    var viewModel = new BookingRequestViewModel
    {
        RoomId = roomId ?? 0,
        RoomOptions = availableRooms.Select(r => new SelectListItem
        {
            Value = r.RoomId.ToString(),
            Text = $"{r.Number} ({r.Type})",
            Selected = roomId.HasValue && r.RoomId == roomId.Value
        })
    };

    return PartialView("_BookingModal", viewModel);
  }

  // POST: /Rooms/BookRoom
  [HttpPost]
  public async Task<IActionResult> BookRoom(
    [Bind("RoomId,FullName,Contact,Note")] BookingRequestViewModel model,
    IFormFile? ProofOfPayment = null)
  {

    if (!ModelState.IsValid)
    {
      // Repopulate RoomOptions if validation fails
      var availableRooms = await _context.Rooms
          .Where(r => r.Status == "Available")
          .ToListAsync();
      model.RoomOptions = availableRooms.Select(r => new SelectListItem
      {
        Value = r.RoomId.ToString(),
        Text = $"{r.Number} ({r.Type})",
        Selected = r.RoomId == model.RoomId
      });
      SetErrorMessage("Please correct the errors in the booking form.");
      return PartialView("_BookingModal", model);
    }

    var booking = new BookingRequest
    {
      RoomId = model.RoomId,
      FullName = model.FullName,
      Contact = model.Contact,
      Note = model.Note,
      ProofOfPaymentPath = null // Set below if file uploaded
    };

    if (ProofOfPayment != null && ProofOfPayment.Length > 0)
    {
      var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "proofs");
      Directory.CreateDirectory(uploadsFolder);
      var fileName = $"{Guid.NewGuid()}_{Path.GetFileName(ProofOfPayment.FileName)}";
      var filePath = Path.Combine(uploadsFolder, fileName);

      using (var stream = new FileStream(filePath, FileMode.Create))
      {
        await ProofOfPayment.CopyToAsync(stream);
      }

      booking.ProofOfPaymentPath = "/uploads/proofs/" + fileName;
    }

    booking.RequestDate = DateTime.Now;
    booking.Status = "Pending";
    booking.DepositPaid = false;
    _context.BookingRequests.Add(booking);
    await _context.SaveChangesAsync();
    SetSuccessMessage("Your booking request has been submitted!");
    return RedirectToAction(nameof(Index));
  }
  [HttpPost]
  public async Task<IActionResult> ConfirmBooking(int bookingRequestId)
  {
    var booking = await _context.BookingRequests.Include(b => b.Room).FirstOrDefaultAsync(b => b.BookingRequestId == bookingRequestId);
    if (booking == null)
    {
      SetErrorMessage("Booking not found.");
      return NotFound();
    }

    booking.Status = "Confirmed";
    booking.DepositPaid = true;
    if (booking.Room != null)
      booking.Room.Status = "Occupied";

    await _context.SaveChangesAsync();
    SetSuccessMessage("Booking confirmed and room marked as unavailable.");
    return RedirectToAction(nameof(Index));
  }

  // GET: /Rooms/EditBookingRequest?bookingRequestId=5
  [HttpGet]
  public async Task<IActionResult> EditBookingRequest(int bookingRequestId)
  {
    var booking = await _context.BookingRequests
        .Include(b => b.Room)
        .FirstOrDefaultAsync(b => b.BookingRequestId == bookingRequestId);

    if (booking == null) return NotFound();

    var availableRooms = await _context.Rooms
        .Where(r => r.Status == "Available" || r.RoomId == booking.RoomId)
        .ToListAsync();

    var viewModel = new BookingRequestViewModel
    {
        BookingRequestId = booking.BookingRequestId,
        RoomId = booking.RoomId,
        FullName = booking.FullName,
        Contact = booking.Contact,
        Note = booking.Note,
        ProofOfPaymentPath = booking.ProofOfPaymentPath,
        RoomOptions = availableRooms.Select(r => new SelectListItem
        {
            Value = r.RoomId.ToString(),
            Text = $"{r.Number} ({r.Type})",
            Selected = r.RoomId == booking.RoomId
        })
    };

    return PartialView("_BookingModal", viewModel);
  }

  // POST: /Rooms/EditBookingRequest
  [HttpPost]
  public async Task<IActionResult> EditBookingRequest(
      [Bind("BookingRequestId,RoomId,FullName,Contact,Note")] BookingRequestViewModel model,
      IFormFile? ProofOfPayment = null)
  {
    if (!ModelState.IsValid)
    {
        var availableRooms = await _context.Rooms
            .Where(r => r.Status == "Available" || r.RoomId == model.RoomId)
            .ToListAsync();
        model.RoomOptions = availableRooms.Select(r => new SelectListItem
        {
            Value = r.RoomId.ToString(),
            Text = $"{r.Number} ({r.Type})",
            Selected = r.RoomId == model.RoomId
        });
        SetErrorMessage("Please correct the errors in the booking form.");
        return PartialView("_BookingModal", model);
    }

    var booking = await _context.BookingRequests.FindAsync(model.BookingRequestId);
    if (booking == null)
    {
      SetErrorMessage("Booking not found.");
      return NotFound();
    }

    booking.RoomId = model.RoomId;
    booking.FullName = model.FullName;
    booking.Contact = model.Contact;
    booking.Note = model.Note;

    if (ProofOfPayment != null && ProofOfPayment.Length > 0)
    {
        var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "proofs");
        Directory.CreateDirectory(uploadsFolder);
        var fileName = $"{Guid.NewGuid()}_{Path.GetFileName(ProofOfPayment.FileName)}";
        var filePath = Path.Combine(uploadsFolder, fileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await ProofOfPayment.CopyToAsync(stream);
        }

        booking.ProofOfPaymentPath = "/uploads/proofs/" + fileName;
    }

    await _context.SaveChangesAsync();
    SetSuccessMessage("Booking request updated!");
    return RedirectToAction(nameof(Index));
  }

  [HttpPost]
  public async Task<IActionResult> DeleteBookingRequest(int bookingRequestId)
  {
    var booking = await _context.BookingRequests.FindAsync(bookingRequestId);
    if (booking == null)
    {
      SetErrorMessage("Booking not found.");
      return NotFound();
    }

    _context.BookingRequests.Remove(booking);
    await _context.SaveChangesAsync();
    SetSuccessMessage("Booking request deleted!");
    return RedirectToAction(nameof(Index));
  }

  // GET: /Rooms/History/5
  public async Task<IActionResult> History(int id)
  {
    var room = await _context.Rooms
        .Include(r => r.MaintenanceRequests)
        .FirstOrDefaultAsync(r => r.RoomId == id);

    if (room == null) return NotFound();

    var requests = await _context.MaintenanceRequests
        .Where(r => r.RoomId == id)
        .OrderByDescending(r => r.RequestDate)
        .ToListAsync();

    ViewBag.Room = room;
    return View(requests);
  }

  private void SetSuccessMessage(string message)
  {
    TempData["Success"] = message;
  }

  private void SetErrorMessage(string message)
  {
    TempData["Error"] = message;
  }
}