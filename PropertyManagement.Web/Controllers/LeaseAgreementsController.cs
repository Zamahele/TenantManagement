using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PropertyManagement.Domain.Entities;
using PropertyManagement.Infrastructure.Data;
using PropertyManagement.Web.Controllers;
using System.Threading.Tasks;

[Authorize]
[Authorize(Roles = "Manager")]
public class LeaseAgreementsController : BaseController
{
    private readonly ApplicationDbContext _context;
    private readonly IWebHostEnvironment _env;

    public LeaseAgreementsController(ApplicationDbContext context, IWebHostEnvironment env)
    {
        _context = context;
        _env = env;
    }

    // GET: /LeaseAgreements
    public async Task<IActionResult> Index()
    {
        var agreements = await _context.LeaseAgreements
            .Include(l => l.Tenant)
            .ThenInclude(t => t.Room)
            .ToListAsync();

        var now = DateTime.UtcNow;
        var expiringIds = agreements
            .Where(a => a.EndDate > now && a.EndDate <= now.AddDays(30))
            .Select(a => a.LeaseAgreementId)
            .ToList();

        var overdueIds = agreements
            .Where(a => a.EndDate < now)
            .Select(a => a.LeaseAgreementId)
            .ToList();

        ViewBag.Tenants = await _context.Tenants.Include(t => t.Room).ToListAsync();
        ViewBag.ExpiringIds = expiringIds;
        ViewBag.OverdueIds = overdueIds;
        return View(agreements);
    }

    // GET: /LeaseAgreements/GetAgreement/5
    public async Task<IActionResult> GetAgreement(int id)
    {
        var agreement = await _context.LeaseAgreements.FindAsync(id);
        if (agreement == null)
        {
            SetErrorMessage("Lease agreement not found.");
            return NotFound();
        }
        return Json(new
        {
            id = agreement.LeaseAgreementId,
            tenantId = agreement.TenantId,
            startDate = agreement.StartDate.ToString("yyyy-MM-dd"),
            endDate = agreement.EndDate.ToString("yyyy-MM-dd"),
            rentAmount = agreement.RentAmount,
            filePath = agreement.FilePath
        });
    }

    // GET: /LeaseAgreements/Edit/5
    public async Task<IActionResult> Edit(int id)
    {
        var agreement = await _context.LeaseAgreements.FindAsync(id);
        if (agreement == null)
        {
            SetErrorMessage("Lease agreement not found.");
            return NotFound();
        }
        ViewBag.Tenants = await _context.Tenants.Include(t => t.Room).ToListAsync();
        return View("CreateOrEdit", agreement);
    }

    // POST: /LeaseAgreements/CreateOrEdit
    [HttpPost]
  // POST: /LeaseAgreements/CreateOrEdit
  [HttpPost]
  public async Task<IActionResult> CreateOrEdit(
    [Bind("LeaseAgreementId,TenantId,RoomId,StartDate,EndDate,FilePath,RentAmount,ExpectedRentDay")] LeaseAgreement agreement,
    IFormFile? File)
  {
    // Custom validation: EndDate must be after Start Date
    if (agreement.EndDate <= agreement.StartDate)
    {
      ModelState.AddModelError("EndDate", "End Date must be after Start Date.");
      SetErrorMessage("End Date must be after Start Date.");
    }

    // Ensure TenantId exists
    var tenantExists = await _context.Tenants.AnyAsync(t => t.TenantId == agreement.TenantId);
    if (!tenantExists)
    {
      ModelState.AddModelError("TenantId", "Selected tenant does not exist.");
      SetErrorMessage("Selected tenant does not exist.");
    }

    // Ensure RoomId exists
    var roomExists = await _context.Rooms.AnyAsync(r => r.RoomId == agreement.RoomId);
    if (!roomExists)
    {
      ModelState.AddModelError("RoomId", "Selected room does not exist.");
      SetErrorMessage("Selected room does not exist.");
    }

    // If editing and no new file uploaded, preserve existing FilePath
    if (agreement.LeaseAgreementId != 0 && (File == null || File.Length == 0))
    {
      var existingAgreement = await _context.LeaseAgreements
          .AsNoTracking()
          .FirstOrDefaultAsync(x => x.LeaseAgreementId == agreement.LeaseAgreementId);

      if (existingAgreement != null)
      {
        agreement.FilePath = existingAgreement.FilePath;
        ModelState.Clear();
        TryValidateModel(agreement);
      }
    }

    if (!ModelState.IsValid)
    {
      ViewBag.Tenants = await _context.Tenants.Include(t => t.Room).ToListAsync();
      ViewBag.IsEdit = agreement.LeaseAgreementId != 0;
      return PartialView("_LeaseAgreementModal", agreement);
    }

    // Handle file upload if present
    if (File != null && File.Length > 0)
    {
      var uploads = Path.Combine(_env.WebRootPath, "uploads");
      Directory.CreateDirectory(uploads);
      var fileName = $"{Guid.NewGuid()}_{Path.GetFileName(File.FileName)}";
      var filePath = Path.Combine(uploads, fileName);
      using (var stream = new FileStream(filePath, FileMode.Create))
      {
        await File.CopyToAsync(stream);
      }
      agreement.FilePath = "/uploads/" + fileName;
    }

    if (agreement.LeaseAgreementId == 0)
    {
      _context.LeaseAgreements.Add(agreement);
      SetSuccessMessage("Lease agreement created successfully.");
    }
    else
    {
      var existing = await _context.LeaseAgreements.FindAsync(agreement.LeaseAgreementId);
      if (existing == null)
      {
        SetErrorMessage("Lease agreement not found.");
        return NotFound();
      }

      existing.TenantId = agreement.TenantId;
      existing.RoomId = agreement.RoomId;
      existing.StartDate = agreement.StartDate;
      existing.EndDate = agreement.EndDate;
      existing.RentAmount = agreement.RentAmount;
      existing.ExpectedRentDay = agreement.ExpectedRentDay;
      if (!string.IsNullOrEmpty(agreement.FilePath))
        existing.FilePath = agreement.FilePath;
      _context.LeaseAgreements.Update(existing);
      SetSuccessMessage("Lease agreement updated successfully.");
    }
    await _context.SaveChangesAsync();
    return RedirectToAction(nameof(Index));
  }

  // POST: /LeaseAgreements/Delete/5
  [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        var agreement = await _context.LeaseAgreements.FindAsync(id);
        if (agreement != null)
        {
            _context.LeaseAgreements.Remove(agreement);
            await _context.SaveChangesAsync();
            SetSuccessMessage("Lease agreement deleted successfully.");
        }
        else
        {
            SetErrorMessage("Lease agreement not found.");
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> LeaseAgreementModal(int? id)
    {
        LeaseAgreement model;
        bool isEdit = false;
        if (id.HasValue && id.Value != 0)
        {
            model = await _context.LeaseAgreements.FindAsync(id.Value);
            if (model == null)
            {
                SetErrorMessage("Lease agreement not found.");
                return NotFound();
            }
            isEdit = true;
        }
        else
        {
            model = new LeaseAgreement();
        }

        ViewBag.Tenants = await _context.Tenants.Include(t => t.Room).ToListAsync();
        ViewBag.IsEdit = isEdit;
        return PartialView("_LeaseAgreementModal", model);
    }

  [HttpGet]
  public async Task<IActionResult> GetRoomIdByTenant(int tenantId)
  {
    var roomId = await _context.Tenants
        .Where(t => t.TenantId == tenantId)
        .Select(t => t.RoomId)
        .FirstOrDefaultAsync();

    return Json(new { roomId });
  }
}