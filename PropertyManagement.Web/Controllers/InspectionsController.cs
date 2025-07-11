using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PropertyManagement.Domain.Entities;
using PropertyManagement.Infrastructure.Data;
using System.Linq;
using System.Threading.Tasks;

namespace PropertyManagement.Web.Controllers
{
    [Authorize]
    public class InspectionsController : BaseController
    {
        private readonly ApplicationDbContext _context;
        public InspectionsController(ApplicationDbContext context) => _context = context;

        // Index view
        public async Task<IActionResult> Index()
        {
            var inspections = await _context.Inspections
                .Include(i => i.Room)
                .OrderByDescending(i => i.Date)
                .ToListAsync();
            return View(inspections);
        }

        // GET: Modal for Add/Edit
        [HttpGet]
        public async Task<IActionResult> InspectionModal(int? id = null)
        {
            Inspection model;
            if (id.HasValue)
            {
                model = await _context.Inspections.FindAsync(id.Value);
                if (model == null)
                {
                    SetErrorMessage("Inspection not found.");
                    return PartialView("_InspectionModal", new Inspection());
                }
            }
            else
            {
                model = new Inspection();
            }

            ViewBag.Rooms = new SelectList(_context.Rooms.ToList(), "RoomId", "Number", model.RoomId);
            return PartialView("_InspectionModal", model);
        }

        // POST: Save Add/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveInspection(Inspection model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Rooms = new SelectList(_context.Rooms.ToList(), "RoomId", "Number", model.RoomId);
                return PartialView("_InspectionModal", model);
            }

            if (model.InspectionId == 0)
            {
                _context.Inspections.Add(model);
                SetSuccessMessage("Inspection added successfully.");
            }
            else
            {
                var existing = await _context.Inspections.FindAsync(model.InspectionId);
                if (existing == null)
                {
                    SetErrorMessage("Inspection not found.");
                    ViewBag.Rooms = new SelectList(_context.Rooms.ToList(), "RoomId", "Number", model.RoomId);
                    return PartialView("_InspectionModal", model);
                }

                existing.RoomId = model.RoomId;
                existing.Date = model.Date;
                existing.Result = model.Result;
                existing.Notes = model.Notes;
                SetSuccessMessage("Inspection updated successfully.");
            }
            await _context.SaveChangesAsync();

            // After save, return the updated list view
            var inspections = await _context.Inspections
                .Include(i => i.Room)
                .OrderByDescending(i => i.Date)
                .ToListAsync();
            return View("Index", inspections);
        }

        // POST: Delete
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var inspection = await _context.Inspections.FindAsync(id);
            if (inspection == null)
            {
                SetErrorMessage("Inspection not found.");
                var inspections = await _context.Inspections
                    .Include(i => i.Room)
                    .OrderByDescending(i => i.Date)
                    .ToListAsync();
                return View("Index", inspections);
            }

            _context.Inspections.Remove(inspection);
            await _context.SaveChangesAsync();
            SetSuccessMessage("Inspection deleted successfully.");

            var updatedInspections = await _context.Inspections
                .Include(i => i.Room)
                .OrderByDescending(i => i.Date)
                .ToListAsync();
            return View("Index", updatedInspections);
        }
    }
}