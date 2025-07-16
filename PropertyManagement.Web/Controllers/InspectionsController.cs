using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PropertyManagement.Domain.Entities;
using PropertyManagement.Infrastructure.Repositories;
using PropertyManagement.Web.ViewModels;
using System.Linq;
using System.Threading.Tasks;

namespace PropertyManagement.Web.Controllers
{
  [Authorize]
  public class InspectionsController : BaseController
  {
    private readonly IGenericRepository<Inspection> _inspectionRepository;
    private readonly IGenericRepository<Room> _roomRepository;
    private readonly IMapper _mapper;

    public InspectionsController(
        IGenericRepository<Inspection> inspectionRepository,
        IGenericRepository<Room> roomRepository,
        IMapper mapper)
    {
      _inspectionRepository = inspectionRepository;
      _roomRepository = roomRepository;
      _mapper = mapper;
    }

    // Index view
    public async Task<IActionResult> Index()
    {
      var inspections = await _inspectionRepository.Query()
          .Include(i => i.Room)
          .OrderByDescending(i => i.Date)
          .ToListAsync();

      var inspectionVms = _mapper.Map<List<InspectionViewModel>>(inspections);
      return View(inspectionVms);
    }

    // GET: Modal for Add/Edit
    [HttpGet]
    public async Task<IActionResult> InspectionModal(int? id = null)
    {
      Inspection model;
      if (id.HasValue)
      {
        model = await _inspectionRepository.GetByIdAsync(id.Value);
        if (model == null)
        {
          SetErrorMessage("Inspection not found.");
          model = new Inspection();
        }
      }
      else
      {
        model = new Inspection();
      }

      var rooms = await _roomRepository.GetAllAsync();
      ViewBag.Rooms = new SelectList(rooms, "RoomId", "Number", model.RoomId);

      var vm = _mapper.Map<InspectionViewModel>(model);
      return PartialView("_InspectionModal", vm);
    }

    // POST: Save Add/Edit
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveInspection(InspectionViewModel model)
    {
      var rooms = await _roomRepository.GetAllAsync();
      if (!ModelState.IsValid)
      {
        ViewBag.Rooms = new SelectList(rooms, "RoomId", "Number", model.RoomId);
        return PartialView("_InspectionModal", model);
      }

      if (model.InspectionId == 0)
      {
        var entity = _mapper.Map<Inspection>(model);
        await _inspectionRepository.AddAsync(entity);
        SetSuccessMessage("Inspection added successfully.");
      }
      else
      {
        var existing = await _inspectionRepository.GetByIdAsync(model.InspectionId);
        if (existing == null)
        {
          SetErrorMessage("Inspection not found.");
          ViewBag.Rooms = new SelectList(rooms, "RoomId", "Number", model.RoomId);
          return PartialView("_InspectionModal", model);
        }

        _mapper.Map(model, existing);
        await _inspectionRepository.UpdateAsync(existing);
        SetSuccessMessage("Inspection updated successfully.");
      }

      var inspections = await _inspectionRepository.Query()
          .Include(i => i.Room)
          .OrderByDescending(i => i.Date)
          .ToListAsync();
      var inspectionVms = _mapper.Map<List<InspectionViewModel>>(inspections);
      return View("Index", inspectionVms);
    }

    // POST: Delete
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
      var inspection = await _inspectionRepository.GetByIdAsync(id);
      if (inspection == null)
      {
        SetErrorMessage("Inspection not found.");
        var inspections = await _inspectionRepository.Query()
            .Include(i => i.Room)
            .OrderByDescending(i => i.Date)
            .ToListAsync();
        var inspectionVms = _mapper.Map<List<InspectionViewModel>>(inspections);
        return View("Index", inspectionVms);
      }

      await _inspectionRepository.DeleteAsync(inspection);
      SetSuccessMessage("Inspection deleted successfully.");

      var updatedInspections = await _inspectionRepository.Query()
          .Include(i => i.Room)
          .OrderByDescending(i => i.Date)
          .ToListAsync();
      var updatedInspectionVms = _mapper.Map<List<InspectionViewModel>>(updatedInspections);
      return View("Index", updatedInspectionVms);
    }
  }
}