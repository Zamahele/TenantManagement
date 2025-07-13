using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PropertyManagement.Domain.Entities;
using PropertyManagement.Infrastructure.Data;
using PropertyManagement.Web.Controllers;
using Prometheus; // Add this at the top

[Authorize]
[Authorize(Roles = "Manager")]
public class PaymentsController : BaseController
{
  private readonly ApplicationDbContext _context;
  // Add a static counter for payment creations
  private static readonly Counter PaymentCreatedCounter =
      Metrics.CreateCounter("payments_created_total", "Total number of payments created.");

  public PaymentsController(ApplicationDbContext context) => _context = context;

  public async Task<IActionResult> Index()
  {
    var payments = await _context.Payments
        .Include(p => p.Tenant)
        .Include (p => p.LeaseAgreement)
        .ThenInclude(t => t.Room)
        .ToListAsync();

    ViewBag.Tenants = await _context.Tenants
        .Include(t => t.Room)
        .Include(t => t.LeaseAgreements)
        .ToListAsync();

    return View(payments);
  }

  [HttpPost]
  [ValidateAntiForgeryToken]
  public async Task<IActionResult> Create([Bind("TenantId,Amount,Type,PaymentMonth,PaymentYear,PaymentId")] Payment payment)
  {
    if (!ModelState.IsValid)
    {
      SetErrorMessage("Please correct the errors in the form.");
      return View(payment);
    }

    // Defensive: Set current month/year if not set (shouldn't happen with required fields)
    if (payment.PaymentMonth < 1 || payment.PaymentMonth > 12)
      payment.PaymentMonth = DateTime.Now.Month;
    if (payment.PaymentYear < 2000 || payment.PaymentYear > DateTime.Now.Year + 1)
      payment.PaymentYear = DateTime.Now.Year;

    // Only set LeaseAgreementId if a valid agreement exists
    var getLeaseAgreement = await _context.LeaseAgreements
        .FirstOrDefaultAsync(la => la.TenantId == payment.TenantId);
    if (getLeaseAgreement != null)
      payment.LeaseAgreementId = getLeaseAgreement.LeaseAgreementId;
    else
      payment.LeaseAgreementId = null;

    if (ModelState.IsValid)
    {
      payment.Date = DateTime.Now;
      _context.Add(payment);
      await _context.SaveChangesAsync();

      // Increment the Prometheus counter
      PaymentCreatedCounter.Inc();

      SetSuccessMessage("Payment recorded successfully.");
      return RedirectToAction(nameof(Index));
    }

    SetErrorMessage("Failed to record payment. Please check the form.");
    var payments = await _context.Payments
        .Include(p => p.Tenant)
        .ThenInclude(t => t.Room)
        .ToListAsync();

    ViewBag.Tenants = await _context.Tenants
        .Include(t => t.Room)
        .ToListAsync();

    return View("Index", payments);
  }

  [HttpPost]
  [ValidateAntiForgeryToken]
  public async Task<IActionResult> Edit([Bind("Id,Amount,Type,PaymentMonth,PaymentYear,PaymentId")] Payment payment)
  {
    if (ModelState.IsValid)
    {
      var existing = await _context.Payments.FindAsync(payment.PaymentId);
      if (existing == null)
      {
        SetErrorMessage("Payment not found.");
        return NotFound();
      }

      existing.Amount = payment.Amount;
      existing.Type = payment.Type;
      existing.PaymentMonth = payment.PaymentMonth;
      existing.PaymentYear = payment.PaymentYear;

      _context.Update(existing);
      await _context.SaveChangesAsync();
      SetSuccessMessage("Payment updated successfully.");
      return RedirectToAction(nameof(Index));
    }

    SetErrorMessage("Failed to update payment. Please check the form.");
    var payments = await _context.Payments
        .Include(p => p.Tenant)
        .Include(p => p.LeaseAgreement)
        .ThenInclude(t => t.Room)
        .ToListAsync();

    ViewBag.Tenants = await _context.Tenants
        .Include(t => t.Room)
        .Include(t => t.LeaseAgreements)
        .ToListAsync();

    return View("Index", payments);
  }

  [HttpPost]
  [ValidateAntiForgeryToken]
  public async Task<IActionResult> Delete(int id)
  {
    var payment = await _context.Payments.FindAsync(id);
    if (payment == null)
    {
      SetErrorMessage("Payment not found.");
      return NotFound();
    }

    _context.Payments.Remove(payment);
    await _context.SaveChangesAsync();
    SetSuccessMessage("Payment deleted successfully.");
    return RedirectToAction(nameof(Index));
  }

  [HttpGet]
  public IActionResult Receipt(int id)
  {
    var payment = _context.Payments
        .Include(p => p.Tenant).Include(p => p.LeaseAgreement).Include(p => p.Tenant.Room)
        .FirstOrDefault(p => p.PaymentId == id);

    if (payment == null)
    {
      SetErrorMessage("Payment not found.");
      return NotFound();
    }

    SetInfoMessage("Payment receipt loaded.");
    return PartialView("_PaymentReceipt", payment);
  }

  public async Task<IActionResult> ReceiptPartial(int id)
  {
    var payment = await _context.Payments
        .Include(p => p.Tenant)
        .ThenInclude(t => t.Room)
        .FirstOrDefaultAsync(p => p.PaymentId == id);

    if (payment == null)
        return NotFound();

    return PartialView("_ReceiptPartial", payment);
  }
}