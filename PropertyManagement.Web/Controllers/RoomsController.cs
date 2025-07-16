using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PropertyManagement.Domain.Entities;
using PropertyManagement.Infrastructure.Data;
using PropertyManagement.Web.Controllers;
using PropertyManagement.Web.ViewModels;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

[Authorize]
[Authorize(Roles = "Manager")]
public class RoomsController : BaseController
{
  private readonly ApplicationDbContext _context;
  private readonly IMapper _mapper;

  public RoomsController(ApplicationDbContext context, IMapper mapper)
  {
    _context = context;
    _mapper = mapper;
  }

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

    var model = _mapper.Map<RoomFormViewModel>(room);
    model.StatusOptions = GetStatusOptions();
    return View("CreateOrEdit", model);
  }

  // POST: /Rooms/CreateOrEdit
  [HttpPost]
  [ValidateAntiForgeryToken]
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
      room = _mapper.Map<Room>(model);
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
      _mapper.Map(model, room);
      SetSuccessMessage("Room updated successfully.");
    }

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

    return Json(new
    {
      roomId = room.RoomId,
      number = room.Number,
      type = room.Type,
      status = room.Status,
      cottageId = room.CottageId
    });
  }

  // POST: /Rooms/DeleteBookingRequest
  [HttpPost]
  [ValidateAntiForgeryToken]
  public async Task<IActionResult> DeleteBookingRequest(int bookingRequestId)
  {
    var booking = await _context.BookingRequests.FindAsync(bookingRequestId);
    if (booking == null)
    {
      SetErrorMessage("Booking request not found.");
      return NotFound();
    }

    _context.BookingRequests.Remove(booking);
    await _context.SaveChangesAsync();
    SetSuccessMessage("Booking request deleted successfully.");
    return RedirectToAction(nameof(Index));
  }
}