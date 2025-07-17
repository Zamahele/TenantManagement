using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Prometheus;
using PropertyManagement.Application.DTOs;
using PropertyManagement.Application.Services;
using PropertyManagement.Web.Controllers;
using PropertyManagement.Web.ViewModels;

[Authorize]
[Authorize(Roles = "Manager")]
public class PaymentsController : BaseController
{
  private readonly IPaymentApplicationService _paymentApplicationService;
  private readonly ITenantApplicationService _tenantApplicationService;
  private readonly IMapper _mapper;

  // Prometheus counter for payment creations
  private static readonly Counter PaymentCreatedCounter =
      Metrics.CreateCounter("payments_created_total", "Total number of payments created.");

  public PaymentsController(
      IPaymentApplicationService paymentApplicationService,
      ITenantApplicationService tenantApplicationService,
      IMapper mapper)
  {
    _paymentApplicationService = paymentApplicationService;
    _tenantApplicationService = tenantApplicationService;
    _mapper = mapper;
  }

  public async Task<IActionResult> Index()
  {
    var paymentsResult = await _paymentApplicationService.GetAllPaymentsAsync();
    if (!paymentsResult.IsSuccess)
    {
      SetErrorMessage(paymentsResult.ErrorMessage);
      return View(new List<PaymentViewModel>());
    }

    var tenantsResult = await _tenantApplicationService.GetAllTenantsAsync();
    if (!tenantsResult.IsSuccess)
    {
      SetErrorMessage(tenantsResult.ErrorMessage);
      return View(new List<PaymentViewModel>());
    }

    ViewBag.Tenants = _mapper.Map<List<TenantViewModel>>(tenantsResult.Data);
    var paymentVm = _mapper.Map<List<PaymentViewModel>>(paymentsResult.Data);

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
      return await GetIndexViewWithData();
    }

    if (payment.PaymentMonth < 1 || payment.PaymentMonth > 12)
      payment.PaymentMonth = DateTime.Now.Month;
    if (payment.PaymentYear < 2000 || payment.PaymentYear > DateTime.Now.Year + 1)
      payment.PaymentYear = DateTime.Now.Year;

    var createPaymentDto = new CreatePaymentDto
    {
      TenantId = payment.TenantId,
      Amount = payment.Amount,
      PaymentDate = DateTime.Now,
      Type = payment.Type,
      PaymentMonth = payment.PaymentMonth,
      PaymentYear = payment.PaymentYear,
      ReceiptPath = payment.ReceiptPath
    };

    var result = await _paymentApplicationService.CreatePaymentAsync(createPaymentDto);
    if (!result.IsSuccess)
    {
      SetErrorMessage(result.ErrorMessage);
      return await GetIndexViewWithData();
    }

    PaymentCreatedCounter.Inc();

    SetSuccessMessage("Payment recorded successfully.");
    return RedirectToAction(nameof(Index));
  }

  [HttpPost]
  [ValidateAntiForgeryToken]
  public async Task<IActionResult> Edit(PaymentViewModel payment)
  {
    if (!ModelState.IsValid)
    {
      SetErrorMessage("Failed to update payment. Please check the form.");
      return await GetIndexViewWithData();
    }

    var updatePaymentDto = new UpdatePaymentDto
    {
      Amount = payment.Amount,
      PaymentDate = payment.Date,
      Type = payment.Type,
      PaymentMonth = payment.PaymentMonth,
      PaymentYear = payment.PaymentYear,
      ReceiptPath = payment.ReceiptPath
    };

    var result = await _paymentApplicationService.UpdatePaymentAsync(payment.PaymentId ?? 0, updatePaymentDto);
    if (!result.IsSuccess)
    {
      SetErrorMessage(result.ErrorMessage);
      return await GetIndexViewWithData();
    }

    SetSuccessMessage("Payment updated successfully.");
    return RedirectToAction(nameof(Index));
  }

  [HttpPost]
  [ValidateAntiForgeryToken]
  public async Task<IActionResult> Delete(int id)
  {
    var result = await _paymentApplicationService.DeletePaymentAsync(id);
    if (!result.IsSuccess)
    {
      SetErrorMessage(result.ErrorMessage);
      return NotFound();
    }

    SetSuccessMessage("Payment deleted successfully.");
    return RedirectToAction(nameof(Index));
  }

  [HttpGet]
  public async Task<IActionResult> Receipt(int id)
  {
    var result = await _paymentApplicationService.GetPaymentByIdAsync(id);
    if (!result.IsSuccess)
    {
      SetErrorMessage(result.ErrorMessage);
      return NotFound();
    }

    var paymentVm = _mapper.Map<PaymentViewModel>(result.Data);

    SetInfoMessage("Payment receipt loaded.");
    return PartialView("_PaymentReceipt", paymentVm);
  }

  public async Task<IActionResult> ReceiptPartial(int id)
  {
    var result = await _paymentApplicationService.GetPaymentByIdAsync(id);
    if (!result.IsSuccess)
      return NotFound();

    var paymentVm = _mapper.Map<PaymentViewModel>(result.Data);

    return PartialView("_ReceiptPartial", paymentVm);
  }

  private async Task<IActionResult> GetIndexViewWithData()
  {
    var paymentsResult = await _paymentApplicationService.GetAllPaymentsAsync();
    var tenantsResult = await _tenantApplicationService.GetAllTenantsAsync();
    
    ViewBag.Tenants = tenantsResult.IsSuccess ? 
        _mapper.Map<List<TenantViewModel>>(tenantsResult.Data) : 
        new List<TenantViewModel>();

    var paymentVms = paymentsResult.IsSuccess ? 
        _mapper.Map<List<PaymentViewModel>>(paymentsResult.Data) : 
        new List<PaymentViewModel>();

    return View("Index", paymentVms);
  }
}