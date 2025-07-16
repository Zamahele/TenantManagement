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

namespace PropertyManagement.Web.Controllers
{
  [Authorize]
  [Authorize(Roles = "Manager")]
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

    // GET: /Inspections
    public async Task<IActionResult> Index()
    {
      var inspections = await _inspectionRepository.GetAllAsync(null, i => i.Room);
      var orderedInspections = inspections.OrderByDescending(i => i.Date).ToList();
      
      var inspectionVms = _mapper.Map<List<InspectionViewModel>>(orderedInspections);
      return View(inspectionVms);
    }

    // GET: /Inspections/Create
    public async Task<IActionResult> Create()
    {
      var rooms = await _roomRepository.GetAllAsync();
      ViewBag.Rooms = new SelectList(rooms, "RoomId", "Number");
      
      var model = new InspectionViewModel();
      return PartialView("_InspectionModal", model);
    }

    // GET: /Inspections/Edit/5
    public async Task<IActionResult> Edit(int id)
    {
      var inspection = await _inspectionRepository.GetByIdAsync(id);
      if (inspection == null) return NotFound();

      var rooms = await _roomRepository.GetAllAsync();
      ViewBag.Rooms = new SelectList(rooms, "RoomId", "Number", inspection.RoomId);

      var model = _mapper.Map<InspectionViewModel>(inspection);
      return PartialView("_InspectionModal", model);
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
      try
      {
        if (!ModelState.IsValid)
        {
          var rooms = await _roomRepository.GetAllAsync();
          ViewBag.Rooms = new SelectList(rooms, "RoomId", "Number", model.RoomId);
          SetErrorMessage("Please correct the errors in the form.");
          return PartialView("_InspectionModal", model);
        }

        Inspection inspection;
        if (model.InspectionId == 0)
        {
          inspection = _mapper.Map<Inspection>(model);
          await _inspectionRepository.AddAsync(inspection);
          SetSuccessMessage("Inspection created successfully.");
        }
        else
        {
          inspection = await _inspectionRepository.GetByIdAsync(model.InspectionId);
          if (inspection == null)
          {
            SetErrorMessage("Inspection not found.");
            return NotFound();
          }
          _mapper.Map(model, inspection);
          await _inspectionRepository.UpdateAsync(inspection);
          SetSuccessMessage("Inspection updated successfully.");
        }
        return RedirectToAction(nameof(Index));
      }
      catch (Exception ex)
      {
        var rooms = await _roomRepository.GetAllAsync();
        ViewBag.Rooms = new SelectList(rooms, "RoomId", "Number", model.RoomId);
        SetErrorMessage("Error saving inspection: " + ex.Message);
        return PartialView("_InspectionModal", model);
      }
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
        var inspection = await _inspectionRepository.GetByIdAsync(id);
        if (inspection == null)
        {
          SetErrorMessage("Inspection not found.");
          return RedirectToAction(nameof(Index));
        }

        await _inspectionRepository.DeleteAsync(inspection);
        SetSuccessMessage("Inspection deleted successfully.");
      }
      catch (Exception ex)
      {
        SetErrorMessage("Error deleting inspection: " + ex.Message);
      }
      
      return RedirectToAction(nameof(Index));
    }
  }
}