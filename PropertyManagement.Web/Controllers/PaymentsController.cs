using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Prometheus;
using PropertyManagement.Domain.Entities;
using PropertyManagement.Infrastructure.Repositories;
using PropertyManagement.Web.Controllers;
using PropertyManagement.Web.ViewModels;

[Authorize]
[Authorize(Roles = "Manager")]
public class PaymentsController : BaseController
{
  private readonly IGenericRepository<Payment> _paymentRepository;
  private readonly IGenericRepository<Tenant> _tenantRepository;
  private readonly IGenericRepository<LeaseAgreement> _leaseAgreementRepository;
  private readonly IMapper _mapper;

  // Prometheus counter for payment creations
  private static readonly Counter PaymentCreatedCounter =
      Metrics.CreateCounter("payments_created_total", "Total number of payments created.");

  public PaymentsController(
      IGenericRepository<Payment> paymentRepository,
      IGenericRepository<Tenant> tenantRepository,
      IGenericRepository<LeaseAgreement> leaseAgreementRepository,
      IMapper mapper)
  {
    _paymentRepository = paymentRepository;
    _tenantRepository = tenantRepository;
    _leaseAgreementRepository = leaseAgreementRepository;
    _mapper = mapper;
  }

  public async Task<IActionResult> Index()
  {
    // Use Query() for ThenInclude support
    var payments = await _paymentRepository.Query()
        .Include(p => p.Tenant)
            .ThenInclude(t => t.Room)
        .Include(p => p.LeaseAgreement)
            .ThenInclude(l => l.Room)
        .ToListAsync();

    var tenants = await _tenantRepository.Query()
        .Include(t => t.Room)
        .Include(t => t.LeaseAgreements)
        .ToListAsync();

    ViewBag.Tenants = _mapper.Map<List<TenantViewModel>>(tenants);
    var paymentVm = _mapper.Map<List<PaymentViewModel>>(payments);

    return View(paymentVm);
  }

  [HttpPost]
  [ValidateAntiForgeryToken]
  public async Task<IActionResult> Create(PaymentViewModel payment)
  {
    await Task.Delay(200);

    if (!ModelState.IsValid)
    {
      SetErrorMessage("Please correct the errors in the form.");
      return View(payment);
    }

    if (payment.PaymentMonth < 1 || payment.PaymentMonth > 12)
      payment.PaymentMonth = DateTime.Now.Month;
    if (payment.PaymentYear < 2000 || payment.PaymentYear > DateTime.Now.Year + 1)
      payment.PaymentYear = DateTime.Now.Year;

    var getLeaseAgreement = await _leaseAgreementRepository.Query()
        .FirstOrDefaultAsync(la => la.TenantId == payment.TenantId);
    if (getLeaseAgreement != null)
      payment.LeaseAgreementId = getLeaseAgreement.LeaseAgreementId;
    else
      payment.LeaseAgreementId = null;

    if (ModelState.IsValid)
    {
      payment.Date = DateTime.Now;

      var paymentEntity = _mapper.Map<Payment>(payment);
      await _paymentRepository.AddAsync(paymentEntity);

      PaymentCreatedCounter.Inc();

      SetSuccessMessage("Payment recorded successfully.");
      return RedirectToAction(nameof(Index));
    }

    SetErrorMessage("Failed to record payment. Please check the form.");
    var payments = await _paymentRepository.Query()
        .Include(p => p.Tenant)
            .ThenInclude(t => t.Room)
        .ToListAsync();

    var tenants = await _tenantRepository.Query()
        .Include(t => t.Room)
        .ToListAsync();

    ViewBag.Tenants = _mapper.Map<List<TenantViewModel>>(tenants);

    return View("Index", _mapper.Map<List<PaymentViewModel>>(payments));
  }

  [HttpPost]
  [ValidateAntiForgeryToken]
  public async Task<IActionResult> Edit(PaymentViewModel payment)
  {
    if (ModelState.IsValid)
    {
      var existing = await _paymentRepository.GetByIdAsync(payment.PaymentId ?? 0);
      if (existing == null)
      {
        SetErrorMessage("Payment not found.");
        return NotFound();
      }

      existing.Amount = payment.Amount;
      existing.Type = payment.Type;
      existing.PaymentMonth = payment.PaymentMonth;
      existing.PaymentYear = payment.PaymentYear;

      await _paymentRepository.UpdateAsync(existing);
      SetSuccessMessage("Payment updated successfully.");
      return RedirectToAction(nameof(Index));
    }

    SetErrorMessage("Failed to update payment. Please check the form.");
    var payments = await _paymentRepository.Query()
        .Include(p => p.Tenant)
        .Include(p => p.LeaseAgreement)
            .ThenInclude(l => l.Room)
        .ToListAsync();

    var tenants = await _tenantRepository.Query()
        .Include(t => t.Room)
        .Include(t => t.LeaseAgreements)
        .ToListAsync();

    ViewBag.Tenants = _mapper.Map<List<TenantViewModel>>(tenants);

    return View("Index", _mapper.Map<List<PaymentViewModel>>(payments));
  }

  [HttpPost]
  [ValidateAntiForgeryToken]
  public async Task<IActionResult> Delete(int id)
  {
    var payment = await _paymentRepository.GetByIdAsync(id);
    if (payment == null)
    {
      SetErrorMessage("Payment not found.");
      return NotFound();
    }

    await _paymentRepository.DeleteAsync(payment);
    SetSuccessMessage("Payment deleted successfully.");
    return RedirectToAction(nameof(Index));
  }

  [HttpGet]
  public async Task<IActionResult> Receipt(int id)
  {
    var payment = await _paymentRepository.Query()
        .Include(p => p.Tenant)
            .ThenInclude(t => t.Room)
        .Include(p => p.LeaseAgreement)
            .ThenInclude(l => l.Room)
        .FirstOrDefaultAsync(p => p.PaymentId == id);

    if (payment == null)
    {
      SetErrorMessage("Payment not found.");
      return NotFound();
    }

    var paymentVm = _mapper.Map<PaymentViewModel>(payment);

    SetInfoMessage("Payment receipt loaded.");
    return PartialView("_PaymentReceipt", paymentVm);
  }

  public async Task<IActionResult> ReceiptPartial(int id)
  {
    var payment = await _paymentRepository.Query()
        .Include(p => p.Tenant)
            .ThenInclude(t => t.Room)
        .Include(p => p.LeaseAgreement)
            .ThenInclude(l => l.Room)
        .FirstOrDefaultAsync(p => p.PaymentId == id);

    if (payment == null)
      return NotFound();

    var paymentVm = _mapper.Map<PaymentViewModel>(payment);

    return PartialView("_ReceiptPartial", paymentVm);
  }
}