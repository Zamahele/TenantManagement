using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PropertyManagement.Domain.Entities;
using PropertyManagement.Infrastructure.Repositories;
using PropertyManagement.Web.Controllers;
using PropertyManagement.Web.ViewModels;
using System.Threading.Tasks;

[Authorize]
[Authorize(Roles = "Manager")]
public class LeaseAgreementsController : BaseController
{
  private readonly IGenericRepository<LeaseAgreement> _leaseAgreementRepository;
  private readonly IGenericRepository<Tenant> _tenantRepository;
  private readonly IGenericRepository<Room> _roomRepository;
  private readonly IWebHostEnvironment _env;
  private readonly IMapper _mapper;

  public LeaseAgreementsController(
      IGenericRepository<LeaseAgreement> leaseAgreementRepository,
      IGenericRepository<Tenant> tenantRepository,
      IGenericRepository<Room> roomRepository,
      IWebHostEnvironment env,
      IMapper mapper)
  {
    _leaseAgreementRepository = leaseAgreementRepository;
    _tenantRepository = tenantRepository;
    _roomRepository = roomRepository;
    _env = env;
    _mapper = mapper;
  }

  // GET: /LeaseAgreements
  public async Task<IActionResult> Index()
  {
    var agreements = await _leaseAgreementRepository.Query()
        .Include(l => l.Tenant)
            .ThenInclude(t => t.Room)
        .Include(l => l.Room)
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

    var tenants = await _tenantRepository.Query().Include(t => t.Room).ToListAsync();
    ViewBag.Tenants = _mapper.Map<List<TenantViewModel>>(tenants);
    ViewBag.ExpiringIds = expiringIds;
    ViewBag.OverdueIds = overdueIds;

    var agreementVms = _mapper.Map<List<LeaseAgreementViewModel>>(agreements);
    return View(agreementVms);
  }

  // GET: /LeaseAgreements/GetAgreement/5
  public async Task<IActionResult> GetAgreement(int id)
  {
    var agreement = await _leaseAgreementRepository.GetByIdAsync(id);
    if (agreement == null)
    {
      SetErrorMessage("Lease agreement not found.");
      return NotFound();
    }
    var agreementVm = _mapper.Map<LeaseAgreementViewModel>(agreement);
    return Json(new
    {
      id = agreementVm.LeaseAgreementId,
      tenantId = agreementVm.TenantId,
      startDate = agreementVm.StartDate.ToString("yyyy-MM-dd"),
      endDate = agreementVm.EndDate.ToString("yyyy-MM-dd"),
      rentAmount = agreementVm.RentAmount,
      filePath = agreementVm.FilePath
    });
  }

  // GET: /LeaseAgreements/Edit/5
  public async Task<IActionResult> Edit(int id)
  {
    var agreement = await _leaseAgreementRepository.GetByIdAsync(id);
    if (agreement == null)
    {
      SetErrorMessage("Lease agreement not found.");
      return NotFound();
    }
    var tenants = await _tenantRepository.Query().Include(t => t.Room).ToListAsync();
    ViewBag.Tenants = _mapper.Map<List<TenantViewModel>>(tenants);
    var agreementVm = _mapper.Map<LeaseAgreementViewModel>(agreement);
    return View("CreateOrEdit", agreementVm);
  }

  // POST: /LeaseAgreements/CreateOrEdit
  [HttpPost]
  public async Task<IActionResult> CreateOrEdit(
      LeaseAgreementViewModel agreementVm,
      IFormFile? File)
  {
    // Check if this is an AJAX request
    bool isAjax = Request.Headers["X-Requested-With"] == "XMLHttpRequest";
    
    // Custom validation: EndDate must be after Start Date
    if (agreementVm.EndDate <= agreementVm.StartDate)
    {
      ModelState.AddModelError("EndDate", "End Date must be after Start Date.");
      SetErrorMessage("End Date must be after Start Date.");
    }

    // Ensure TenantId exists
    var tenantExists = await _tenantRepository.Query().AnyAsync(t => t.TenantId == agreementVm.TenantId);
    if (!tenantExists)
    {
      ModelState.AddModelError("TenantId", "Selected tenant does not exist.");
      SetErrorMessage("Selected tenant does not exist.");
    }

    // Ensure RoomId exists
    var roomExists = await _roomRepository.Query().AnyAsync(r => r.RoomId == agreementVm.RoomId);
    if (!roomExists)
    {
      ModelState.AddModelError("RoomId", "Selected room does not exist.");
      SetErrorMessage("Selected room does not exist.");
    }

    // If editing and no new file uploaded, preserve existing FilePath
    if (agreementVm.LeaseAgreementId != 0 && (File == null || File.Length == 0))
    {
      var existingAgreement = await _leaseAgreementRepository.Query()
          .AsNoTracking()
          .FirstOrDefaultAsync(x => x.LeaseAgreementId == agreementVm.LeaseAgreementId);

      if (existingAgreement != null)
      {
        agreementVm.FilePath = existingAgreement.FilePath;
        ModelState.Clear();
        TryValidateModel(agreementVm);
      }
    }

    if (!ModelState.IsValid)
    {
      if (isAjax)
      {
        var errors = ModelState.Values
            .SelectMany(v => v.Errors)
            .Select(e => e.ErrorMessage)
            .ToList();
        return Json(new { success = false, message = "Please correct the form errors.", errors = errors });
      }
      
      var tenants = await _tenantRepository.Query().Include(t => t.Room).ToListAsync();
      ViewBag.Tenants = _mapper.Map<List<TenantViewModel>>(tenants);
      ViewBag.IsEdit = agreementVm.LeaseAgreementId != 0;
      return PartialView("_LeaseAgreementModal", agreementVm);
    }

    try
    {
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
        agreementVm.FilePath = "/uploads/" + fileName;
      }

      if (agreementVm.LeaseAgreementId == 0)
      {
        var entity = _mapper.Map<LeaseAgreement>(agreementVm);
        await _leaseAgreementRepository.AddAsync(entity);
        
        if (isAjax)
        {
          return Json(new { success = true, message = "Lease agreement created successfully." });
        }
        SetSuccessMessage("Lease agreement created successfully.");
      }
      else
      {
        var existing = await _leaseAgreementRepository.GetByIdAsync(agreementVm.LeaseAgreementId);
        if (existing == null)
        {
          if (isAjax)
          {
            return Json(new { success = false, message = "Lease agreement not found." });
          }
          SetErrorMessage("Lease agreement not found.");
          return NotFound();
        }

        // Update properties manually to avoid navigation property issues
        existing.TenantId = agreementVm.TenantId;
        existing.RoomId = agreementVm.RoomId;
        existing.StartDate = agreementVm.StartDate;
        existing.EndDate = agreementVm.EndDate;
        existing.RentAmount = agreementVm.RentAmount;
        existing.ExpectedRentDay = agreementVm.ExpectedRentDay;
        if (!string.IsNullOrEmpty(agreementVm.FilePath))
          existing.FilePath = agreementVm.FilePath;
        await _leaseAgreementRepository.UpdateAsync(existing);
        
        if (isAjax)
        {
          return Json(new { success = true, message = "Lease agreement updated successfully." });
        }
        SetSuccessMessage("Lease agreement updated successfully.");
      }
    }
    catch (Exception ex)
    {
      if (isAjax)
      {
        return Json(new { success = false, message = $"An unexpected error occurred: {ex.Message}" });
      }
      SetErrorMessage($"An unexpected error occurred: {ex.Message}");
      
      var tenants = await _tenantRepository.Query().Include(t => t.Room).ToListAsync();
      ViewBag.Tenants = _mapper.Map<List<TenantViewModel>>(tenants);
      ViewBag.IsEdit = agreementVm.LeaseAgreementId != 0;
      return PartialView("_LeaseAgreementModal", agreementVm);
    }
    
    return RedirectToAction(nameof(Index));
  }

  // POST: /LeaseAgreements/Delete/5
  [HttpPost]
  public async Task<IActionResult> Delete(int id)
  {
    // Check if this is an AJAX request
    bool isAjax = Request.Headers["X-Requested-With"] == "XMLHttpRequest";
    
    var agreement = await _leaseAgreementRepository.GetByIdAsync(id);
    if (agreement != null)
    {
      await _leaseAgreementRepository.DeleteAsync(agreement);
      
      if (isAjax)
      {
        return Json(new { success = true, message = "Lease agreement deleted successfully." });
      }
      SetSuccessMessage("Lease agreement deleted successfully.");
    }
    else
    {
      if (isAjax)
      {
        return Json(new { success = false, message = "Lease agreement not found." });
      }
      SetErrorMessage("Lease agreement not found.");
    }

    return RedirectToAction(nameof(Index));
  }

  [HttpGet]
  public async Task<IActionResult> LeaseAgreementModal(int? id)
  {
    LeaseAgreementViewModel model;
    bool isEdit = false;
    if (id.HasValue && id.Value != 0)
    {
      var entity = await _leaseAgreementRepository.GetByIdAsync(id.Value);
      if (entity == null)
      {
        SetErrorMessage("Lease agreement not found.");
        return NotFound();
      }
      model = _mapper.Map<LeaseAgreementViewModel>(entity);
      isEdit = true;
    }
    else
    {
      model = new LeaseAgreementViewModel();
    }

    var tenants = await _tenantRepository.Query().Include(t => t.Room).ToListAsync();
    ViewBag.Tenants = _mapper.Map<List<TenantViewModel>>(tenants);
    ViewBag.IsEdit = isEdit;
    return PartialView("_LeaseAgreementModal", model);
  }

  [HttpGet]
  public async Task<IActionResult> GetRoomIdByTenant(int tenantId)
  {
    var roomId = await _tenantRepository.Query()
        .Where(t => t.TenantId == tenantId)
        .Select(t => t.RoomId)
        .FirstOrDefaultAsync();

    return Json(new { roomId });
  }
}