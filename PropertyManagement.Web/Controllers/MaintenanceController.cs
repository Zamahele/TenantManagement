using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PropertyManagement.Domain.Entities;
using PropertyManagement.Infrastructure.Repositories;
using PropertyManagement.Web.Controllers;
using PropertyManagement.Web.ViewModels;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

[Authorize]
public class MaintenanceController : BaseController
{
  private readonly IGenericRepository<MaintenanceRequest> _maintenanceRepository;
  private readonly IGenericRepository<Room> _roomRepository;
  private readonly IGenericRepository<Tenant> _tenantRepository;
  private readonly IMapper _mapper;

  public MaintenanceController(
      IGenericRepository<MaintenanceRequest> maintenanceRepository,
      IGenericRepository<Room> roomRepository,
      IGenericRepository<Tenant> tenantRepository,
      IMapper mapper)
  {
    _maintenanceRepository = maintenanceRepository;
    _roomRepository = roomRepository;
    _tenantRepository = tenantRepository;
    _mapper = mapper;
  }

  // GET: /Maintenance
  public async Task<IActionResult> Index()
  {
    if (User.IsInRole("Manager"))
    {
      ViewBag.IsManager = true;
      var rooms = await _roomRepository.GetAllAsync();
      ViewBag.Rooms = _mapper.Map<List<RoomViewModel>>(rooms);

      var requests = await _maintenanceRepository.Query()
          .Include(r => r.Room)
          .OrderByDescending(r => r.RequestDate)
          .ToListAsync();
      var vm = _mapper.Map<List<MaintenanceRequestViewModel>>(requests);
      return View(vm);
    }
    else if (User.IsInRole("Tenant"))
    {
      ViewBag.IsManager = false;
      var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
      var tenant = await _tenantRepository.Query().FirstOrDefaultAsync(t => t.UserId == userId);
      if (tenant == null)
      {
        SetErrorMessage("Tenant record not found.");
        return View(Enumerable.Empty<MaintenanceRequestViewModel>());
      }
      var requests = await _maintenanceRepository.Query()
          .Where(r => r.TenantId == tenant.TenantId.ToString())
          .Include(r => r.Room)
          .OrderByDescending(r => r.RequestDate)
          .ToListAsync();
      var vm = _mapper.Map<List<MaintenanceRequestViewModel>>(requests);
      return View(vm);
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
    var tenant = await _tenantRepository.Query().Include(t => t.Room).FirstOrDefaultAsync(t => t.UserId == userId);
    if (tenant == null)
    {
      SetErrorMessage("Tenant record not found.");
      return RedirectToAction("Index");
    }

    var model = new MaintenanceRequestViewModel
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
  public async Task<IActionResult> SubmitTenantRequest(MaintenanceRequestViewModel model)
  {
    var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
    var tenant = await _tenantRepository.Query().FirstOrDefaultAsync(t => t.UserId == userId);
    if (tenant == null)
    {
      SetErrorMessage("Tenant record not found.");
      return RedirectToAction("Index");
    }

    if (!ModelState.IsValid)
    {
      var room = await _roomRepository.GetByIdAsync(tenant.RoomId);
      ViewBag.RoomNumber = room?.Number;
      return View(model);
    }

    model.TenantId = tenant.TenantId.ToString();
    model.RoomId = tenant.RoomId;
    model.Status = "Pending";
    model.RequestDate = DateTime.UtcNow;
    model.CompletedDate = null;
    model.AssignedTo = "Manager";

    var entity = _mapper.Map<MaintenanceRequest>(model);
    await _maintenanceRepository.AddAsync(entity);

    // Set room status to "Under Maintenance"
    var roomToUpdate = await _roomRepository.GetByIdAsync(model.RoomId);
    if (roomToUpdate != null)
    {
      roomToUpdate.Status = "Under Maintenance";
      await _roomRepository.UpdateAsync(roomToUpdate);
    }

    TempData["Success"] = $"Your maintenance request has been submitted. Reference Number: {entity.MaintenanceRequestId}";
    return RedirectToAction(nameof(Index));
  }

  // MANAGER ACTIONS

  [Authorize(Roles = "Manager")]
  public async Task<IActionResult> Create()
  {
    var rooms = await _roomRepository.GetAllAsync();
    ViewBag.Rooms = _mapper.Map<List<RoomViewModel>>(rooms);
    return View(new MaintenanceRequestViewModel());
  }

  // GET: /Maintenance/RequestModal (for AJAX/modal)
  [HttpGet]
  [Authorize(Roles = "Manager")]
  public async Task<IActionResult> RequestModal(int? id = null)
  {
    MaintenanceRequestViewModel model;
    if (id.HasValue && id.Value > 0)
    {
      var entity = await _maintenanceRepository.GetByIdAsync(id.Value);
      model = entity != null ? _mapper.Map<MaintenanceRequestViewModel>(entity) : new MaintenanceRequestViewModel();
    }
    else
    {
      model = new MaintenanceRequestViewModel();
    }
    var rooms = await _roomRepository.GetAllAsync();
    ViewBag.Rooms = _mapper.Map<List<RoomViewModel>>(rooms);
    return PartialView("_RequestModal", model);
  }

  // POST: /Maintenance/CreateOrEdit
  [HttpPost]
  [Authorize(Roles = "Manager")]
  public async Task<IActionResult> CreateOrEdit(MaintenanceRequestViewModel request)
  {
    var tenant = await _tenantRepository.Query().FirstOrDefaultAsync(t => t.RoomId == request.RoomId);
    if (tenant != null)
    {
      request.TenantId = tenant.TenantId.ToString();
    }
    else
    {
      SetErrorMessage("No tenant found for the selected room.");
      var rooms = await _roomRepository.GetAllAsync();
      ViewBag.Rooms = _mapper.Map<List<RoomViewModel>>(rooms);
      var requests = await _maintenanceRepository.Query().Include(r => r.Room).ToListAsync();
      var vm = _mapper.Map<List<MaintenanceRequestViewModel>>(requests);
      return View("Index", vm);
    }

    if (request.MaintenanceRequestId == 0)
    {
      request.RequestDate = DateTime.UtcNow;
      request.CompletedDate = null;
      var entity = _mapper.Map<MaintenanceRequest>(request);
      await _maintenanceRepository.AddAsync(entity);

      // Set room status to "Under Maintenance"
      var room = await _roomRepository.GetByIdAsync(request.RoomId);
      if (room != null)
      {
        room.Status = "Under Maintenance";
        await _roomRepository.UpdateAsync(room);
      }

      SetSuccessMessage("Maintenance request created successfully.");
    }
    else
    {
      var existing = await _maintenanceRepository.GetByIdAsync(request.MaintenanceRequestId);
      if (existing == null)
      {
        SetErrorMessage("Maintenance request not found.");
        return NotFound();
      }

      _mapper.Map(request, existing);

      if (existing.Status == "Completed" && existing.CompletedDate == null)
      {
        existing.CompletedDate = DateTime.UtcNow;
      }
      else if (existing.Status != "Completed")
      {
        existing.CompletedDate = null;
      }

      await _maintenanceRepository.UpdateAsync(existing);
      SetSuccessMessage("Maintenance request updated successfully.");
    }
    return RedirectToAction(nameof(Index));
  }

  [HttpPost]
  [Authorize(Roles = "Manager")]
  public async Task<IActionResult> Complete(int id)
  {
    var request = await _maintenanceRepository.GetByIdAsync(id);
    if (request == null)
    {
      SetErrorMessage("Request not found.");
      return NotFound();
    }

    request.Status = "Completed";
    request.CompletedDate = DateTime.Now;
    await _maintenanceRepository.UpdateAsync(request);
    SetSuccessMessage("Maintenance request marked as completed.");
    return RedirectToAction(nameof(Index));
  }

  // GET: /Maintenance/GetRequest/5
  [Authorize(Roles = "Manager")]
  public async Task<IActionResult> GetRequest(int id)
  {
    var request = await _maintenanceRepository.GetByIdAsync(id);
    if (request == null) return NotFound();
    var vm = _mapper.Map<MaintenanceRequestViewModel>(request);
    return Json(vm);
  }

  // POST: /Maintenance/Delete/5
  [HttpPost]
  [Authorize(Roles = "Manager")]
  public async Task<IActionResult> Delete(int id)
  {
    var request = await _maintenanceRepository.Query()
        .Include(r => r.Room)
        .FirstOrDefaultAsync(r => r.MaintenanceRequestId == id);

    if (request == null)
    {
      SetErrorMessage("Maintenance request not found.");
      return NotFound();
    }

    var room = request.Room;

    await _maintenanceRepository.DeleteAsync(request);

    if (room != null)
    {
      // Check if there are any remaining maintenance requests for this room
      var hasOtherRequests = await _maintenanceRepository.Query()
          .AnyAsync(r => r.RoomId == room.RoomId);

      if (!hasOtherRequests)
      {
        // Load tenants for the room
        await _roomRepository.Query().Where(r => r.RoomId == room.RoomId).SelectMany(r => r.Tenants).LoadAsync();

        if (room.Tenants != null && room.Tenants.Any())
        {
          room.Status = "Occupied";
        }
        else
        {
          room.Status = "Available";
        }
        await _roomRepository.UpdateAsync(room);
      }
    }

    SetSuccessMessage("Maintenance request deleted and room status updated if applicable.");
    return RedirectToAction("Index");
  }

  // GET: /Maintenance/History/roomId
  [Authorize(Roles = "Manager")]
  public async Task<IActionResult> History(int roomId)
  {
    var history = await _maintenanceRepository.Query()
        .Where(r => r.RoomId == roomId)
        .Include(r => r.Room)
        .ToListAsync();
    var vm = _mapper.Map<List<MaintenanceRequestViewModel>>(history);
    return View(vm);
  }

  // GET: /Maintenance/DeleteModal
  public IActionResult DeleteModal(int id)
  {
    var request = _maintenanceRepository.GetByIdAsync(id).Result;
    if (request == null)
      return NotFound();

    var vm = _mapper.Map<MaintenanceRequestViewModel>(request);
    return PartialView("_DeleteModal", vm);
  }
}