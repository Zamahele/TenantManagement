using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PropertyManagement.Domain.Entities;
using PropertyManagement.Infrastructure.Data;
using PropertyManagement.Web.Controllers;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

[Authorize]
public class MaintenanceController : BaseController
{
  private readonly ApplicationDbContext _context;

  public MaintenanceController(ApplicationDbContext context)
  {
    _context = context;
  }

  // GET: /Maintenance
  public async Task<IActionResult> Index()
  {
    if (User.IsInRole("Manager"))
    {
      ViewBag.IsManager = true;
      ViewBag.Rooms = await _context.Rooms.ToListAsync();
      var requests = await _context.MaintenanceRequests
          .Include(r => r.Room)
          .OrderByDescending(r => r.RequestDate)
          .ToListAsync();
      return View(requests);
    }
    else if (User.IsInRole("Tenant"))
    {
      ViewBag.IsManager = false;
      var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
      var tenant = await _context.Tenants.FirstOrDefaultAsync(t => t.UserId == userId);
      if (tenant == null)
      {
        SetErrorMessage("Tenant record not found.");
        return View(Enumerable.Empty<MaintenanceRequest>());
      }
      var requests = await _context.MaintenanceRequests
          .Where(r => r.TenantId == tenant.TenantId.ToString())
          .Include(r => r.Room)
          .OrderByDescending(r => r.RequestDate)
          .ToListAsync();
      return View(requests);
    }
    else
    {
      return Forbid();
    }
  }

  // GET: /Maintenance/SubmitTenantRequest
  [Authorize(Roles = "Tenant")]
  public async Task<IActionResult> SubmitTenantRequest()
  {
    var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
    var tenant = await _context.Tenants.Include(t => t.Room).FirstOrDefaultAsync(t => t.UserId == userId);
    if (tenant == null)
    {
      SetErrorMessage("Tenant record not found.");
      return RedirectToAction("Index");
    }

    var model = new MaintenanceRequest
    {
      RoomId = tenant.RoomId,
      TenantId = tenant.TenantId.ToString()
    };

    ViewBag.RoomNumber = tenant.Room?.Number;
    return View(model);
  }

  // POST: /Maintenance/SubmitTenantRequest
  [HttpPost]
  [Authorize(Roles = "Tenant")]
  public async Task<IActionResult> SubmitTenantRequest(MaintenanceRequest model)
  {
    var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
    var tenant = await _context.Tenants.FirstOrDefaultAsync(t => t.UserId == userId);
    if (tenant == null)
    {
      SetErrorMessage("Tenant record not found.");
      return RedirectToAction("Index");
    }

    if (!ModelState.IsValid)
    {
      ViewBag.RoomNumber = (await _context.Rooms.FindAsync(tenant.RoomId))?.Number;
      return View(model);
    }

    model.TenantId = tenant.TenantId.ToString();
    model.RoomId = tenant.RoomId;
    model.Status = "Pending";
    model.RequestDate = DateTime.UtcNow;
    model.CompletedDate = null;
    model.AssignedTo = "Manager";

    _context.MaintenanceRequests.Add(model);

    // Set room status to "Under Maintenance"
    var room = await _context.Rooms.FindAsync(model.RoomId);
    if (room != null)
    {
      room.Status = "Under Maintenance";
      _context.Rooms.Update(room);
    }

    await _context.SaveChangesAsync();

    TempData["Success"] = $"Your maintenance request has been submitted. Reference Number: {model.MaintenanceRequestId}";
    return RedirectToAction(nameof(Index));
  }

  // MANAGER ACTIONS

  [Authorize(Roles = "Manager")]
  public async Task<IActionResult> Create()
  {
    ViewBag.Rooms = await _context.Rooms.ToListAsync();
    return View();
  }

  // GET: /Maintenance/RequestModal (for AJAX/modal)
  [HttpGet]
  [Authorize(Roles = "Manager")]
  public async Task<IActionResult> RequestModal(int? id = null)
  {
    MaintenanceRequest model = null;
    if (id.HasValue && id.Value > 0)
    {
      model = await _context.MaintenanceRequests.FindAsync(id.Value);
    }
    else
    {
      model = new MaintenanceRequest();
    }
    ViewBag.Rooms = await _context.Rooms.ToListAsync();
    return PartialView("_RequestModal", model);
  }

  // POST: /Maintenance/CreateOrEdit
  [HttpPost]
  [Authorize(Roles = "Manager")]
  public async Task<IActionResult> CreateOrEdit(MaintenanceRequest request)
  {
    var tenant = await _context.Tenants.FirstOrDefaultAsync(t => t.RoomId == request.RoomId);
    if (tenant != null)
    {
      request.TenantId = tenant.TenantId.ToString();
    }
    else
    {
      SetErrorMessage("No tenant found for the selected room.");
      ViewBag.Rooms = await _context.Rooms.ToListAsync();
      var requests = await _context.MaintenanceRequests.Include(r => r.Room).ToListAsync();
      return View("Index", requests);
    }

    if (request.MaintenanceRequestId == 0)
    {
      request.RequestDate = DateTime.UtcNow;
      request.CompletedDate = null;
      _context.MaintenanceRequests.Add(request);

      // Set room status to "Under Maintenance"
      var room = await _context.Rooms.FindAsync(request.RoomId);
      if (room != null)
      {
        room.Status = "Under Maintenance";
        _context.Rooms.Update(room);
      }

      await _context.SaveChangesAsync();
      SetSuccessMessage("Maintenance request created successfully.");
    }
    else
    {
      var existing = await _context.MaintenanceRequests.FindAsync(request.MaintenanceRequestId);
      if (existing == null)
      {
        SetErrorMessage("Maintenance request not found.");
        return NotFound();
      }

      existing.RoomId = request.RoomId;
      existing.TenantId = request.TenantId;
      existing.Description = request.Description;
      existing.Status = request.Status;
      existing.AssignedTo = request.AssignedTo;

      if (existing.Status == "Completed" && existing.CompletedDate == null)
      {
        existing.CompletedDate = DateTime.UtcNow;
      }
      else if (existing.Status != "Completed")
      {
        existing.CompletedDate = null;
      }

      _context.MaintenanceRequests.Update(existing);
      await _context.SaveChangesAsync();
      SetSuccessMessage("Maintenance request updated successfully.");
    }
    return RedirectToAction(nameof(Index));
  }

  [HttpPost]
  [Authorize(Roles = "Manager")]
  public async Task<IActionResult> Complete(int id)
  {
    var request = await _context.MaintenanceRequests.FindAsync(id);
    if (request == null)
    {
      SetErrorMessage("Request not found.");
      return NotFound();
    }

    request.Status = "Completed";
    request.CompletedDate = DateTime.Now;
    await _context.SaveChangesAsync();
    SetSuccessMessage("Maintenance request marked as completed.");
    return RedirectToAction(nameof(Index));
  }

  // GET: /Maintenance/GetRequest/5
  [Authorize(Roles = "Manager")]
  public async Task<IActionResult> GetRequest(int id)
  {
    var request = await _context.MaintenanceRequests.FindAsync(id);
    if (request == null) return NotFound();
    return Json(request);
  }

  // POST: /Maintenance/Delete/5
  [HttpPost]
  [Authorize(Roles = "Manager")]
  public async Task<IActionResult> Delete(int id)
  {
    var request = await _context.MaintenanceRequests
        .Include(r => r.Room)
        .FirstOrDefaultAsync(r => r.MaintenanceRequestId == id);

    if (request == null)
    {
        SetErrorMessage("Maintenance request not found.");
        return NotFound();
    }

    var room = request.Room;

    _context.MaintenanceRequests.Remove(request);
    await _context.SaveChangesAsync();

    if (room != null)
    {
        // Check if there are any remaining maintenance requests for this room
        var hasOtherRequests = await _context.MaintenanceRequests
            .AnyAsync(r => r.RoomId == room.RoomId);

        if (!hasOtherRequests)
        {
            // Load tenants for the room
            await _context.Entry(room).Collection(r => r.Tenants).LoadAsync();

            if (room.Tenants != null && room.Tenants.Any())
            {
                room.Status = "Occupied";
            }
            else
            {
                room.Status = "Available";
            }
            await _context.SaveChangesAsync();
        }
    }

    SetSuccessMessage("Maintenance request deleted and room status updated if applicable.");
    return RedirectToAction("Index");
  }

  // GET: /Maintenance/History/roomId
  [Authorize(Roles = "Manager")]
  public async Task<IActionResult> History(int roomId)
  {
    var history = await _context.MaintenanceRequests
        .Where(r => r.RoomId == roomId)
        .Include(r => r.Room)
        .ToListAsync();
    return View(history);
  }

  // GET: /Maintenance/DeleteModal
  public IActionResult DeleteModal(int id)
  {
    var request = _context.MaintenanceRequests.Find(id);
    if (request == null)
        return NotFound();

    return PartialView("_DeleteModal", request);
  }
}