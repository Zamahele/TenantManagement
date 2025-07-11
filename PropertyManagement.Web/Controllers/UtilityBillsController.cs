using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using PropertyManagement.Domain.Entities;
using PropertyManagement.Infrastructure.Data;
using System.Linq;
using System.Threading.Tasks;

namespace PropertyManagement.Web.Controllers
{
    [Authorize]
    public class UtilityBillsController : BaseController
    {
        private readonly ApplicationDbContext _context;
        private readonly decimal _waterRate;
        private readonly decimal _electricityRate;

        public UtilityBillsController(ApplicationDbContext context, IConfiguration config)
        {
            _context = context;
            _waterRate = config.GetSection("UtilityRates").GetValue<decimal>("WaterPerLiter");
            _electricityRate = config.GetSection("UtilityRates").GetValue<decimal>("ElectricityPerKwh");
        }

        public async Task<IActionResult> Index()
        {
            var bills = await _context.UtilityBills.Include(u => u.Room).OrderByDescending(u => u.BillingDate).ToListAsync();
            ViewBag.WaterRate = _waterRate;
            ViewBag.ElectricityRate = _electricityRate;
            return View(bills);
        }

        [HttpGet]
        public async Task<IActionResult> UtilityBillModal(int? id = null)
        {
            UtilityBill model;
            if (id.HasValue)
            {
                model = await _context.UtilityBills.FindAsync(id.Value);
                if (model == null)
                {
                    SetErrorMessage("Utility bill not found.");
                    return PartialView("_UtilityBillModal", new UtilityBill());
                }
            }
            else
            {
                model = new UtilityBill { BillingDate = DateTime.Today };
            }

            ViewBag.Rooms = new SelectList(_context.Rooms.ToList(), "RoomId", "Number", model.RoomId);
            ViewBag.WaterRate = _waterRate;
            ViewBag.ElectricityRate = _electricityRate;
            return PartialView("_UtilityBillModal", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveUtilityBill(UtilityBill model)
        {
            // Calculate total using rates from appsettings
            model.TotalAmount = (model.WaterUsage * _waterRate) + (model.ElectricityUsage * _electricityRate);

            if (!ModelState.IsValid)
            {
                ViewBag.Rooms = new SelectList(_context.Rooms.ToList(), "RoomId", "Number", model.RoomId);
                return PartialView("_UtilityBillModal", model);
            }

            if (model.UtilityBillId == 0)
            {
                _context.UtilityBills.Add(model);
                SetSuccessMessage("Utility bill added successfully.");
            }
            else
            {
                var existing = await _context.UtilityBills.FindAsync(model.UtilityBillId);
                if (existing == null)
                {
                    SetErrorMessage("Utility bill not found.");
                    ViewBag.Rooms = new SelectList(_context.Rooms.ToList(), "RoomId", "Number", model.RoomId);
                    return PartialView("_UtilityBillModal", model);
                }

                existing.RoomId = model.RoomId;
                existing.BillingDate = model.BillingDate;
                existing.WaterUsage = model.WaterUsage;
                existing.ElectricityUsage = model.ElectricityUsage;
                existing.TotalAmount = model.TotalAmount;
                existing.Notes = model.Notes;
                SetSuccessMessage("Utility bill updated successfully.");
            }
            await _context.SaveChangesAsync();

            var bills = await _context.UtilityBills.Include(u => u.Room).OrderByDescending(u => u.BillingDate).ToListAsync();
            return View("Index", bills);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var bill = await _context.UtilityBills.FindAsync(id);
            if (bill == null)
            {
                SetErrorMessage("Utility bill not found.");
                var bills = await _context.UtilityBills.Include(u => u.Room).OrderByDescending(u => u.BillingDate).ToListAsync();
                return View("Index", bills);
            }

            _context.UtilityBills.Remove(bill);
            await _context.SaveChangesAsync();
            SetSuccessMessage("Utility bill deleted successfully.");

            var updatedBills = await _context.UtilityBills.Include(u => u.Room).OrderByDescending(u => u.BillingDate).ToListAsync();
            return View("Index", updatedBills);
        }
    }
}