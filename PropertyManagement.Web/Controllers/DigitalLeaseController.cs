using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PropertyManagement.Application.DTOs;
using PropertyManagement.Application.Services;
using PropertyManagement.Web.Controllers;
using PropertyManagement.Web.ViewModels;
using System.Security.Claims;

namespace PropertyManagement.Web.Controllers
{
    [Authorize]
    public class DigitalLeaseController : BaseController
    {
        private readonly ILeaseGenerationService _leaseGenerationService;
        private readonly ILeaseAgreementApplicationService _leaseAgreementService;
        private readonly ITenantApplicationService _tenantApplicationService;
        private readonly IMapper _mapper;

        public DigitalLeaseController(
            ILeaseGenerationService leaseGenerationService,
            ILeaseAgreementApplicationService leaseAgreementService,
            ITenantApplicationService tenantApplicationService,
            IMapper mapper)
        {
            _leaseGenerationService = leaseGenerationService;
            _leaseAgreementService = leaseAgreementService;
            _tenantApplicationService = tenantApplicationService;
            _mapper = mapper;
        }

        // Manager Actions
        [HttpPost]
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> GenerateLease(int leaseAgreementId, int? templateId = null)
        {
            try
            {
                // Generate HTML content
                var htmlResult = await _leaseGenerationService.GenerateLeaseHtmlAsync(leaseAgreementId, templateId);
                if (!htmlResult.IsSuccess)
                {
                    SetErrorMessage($"Failed to generate lease: {htmlResult.ErrorMessage}");
                    return RedirectToAction("Index", "LeaseAgreements");
                }

                // Generate PDF
                var pdfResult = await _leaseGenerationService.GenerateLeasePdfAsync(leaseAgreementId, htmlResult.Data);
                if (!pdfResult.IsSuccess)
                {
                    SetErrorMessage($"Failed to generate lease: {pdfResult.ErrorMessage}");
                    return RedirectToAction("Index", "LeaseAgreements");
                }

                SetSuccessMessage($"Lease agreement generated successfully! PDF created at: {pdfResult.Data}");
                return RedirectToAction("Index", "LeaseAgreements");
            }
            catch (Exception ex)
            {
                SetErrorMessage($"Error generating lease: {ex.Message}");
                return RedirectToAction("Index", "LeaseAgreements");
            }
        }

        [HttpPost]
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> SendToTenant(int leaseAgreementId)
        {
            try
            {
                var result = await _leaseGenerationService.SendLeaseToTenantAsync(leaseAgreementId);
                if (!result.IsSuccess)
                {
                    SetErrorMessage($"Failed to send lease: {result.ErrorMessage}");
                }
                else
                {
                    SetSuccessMessage("Lease agreement sent to tenant for signing!");
                }

                return RedirectToAction("Index", "LeaseAgreements");
            }
            catch (Exception ex)
            {
                SetErrorMessage($"Error sending lease: {ex.Message}");
                return RedirectToAction("Index", "LeaseAgreements");
            }
        }

        // Tenant Actions
        [HttpGet]
        public async Task<IActionResult> MyLeases()
        {
            try
            {
                var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    SetErrorMessage("Invalid user session.");
                    return RedirectToAction("Login", "Auth");
                }

                var tenantResult = await _tenantApplicationService.GetTenantByUserIdAsync(userId);
                if (!tenantResult.IsSuccess)
                {
                    SetErrorMessage("Tenant information not found.");
                    return RedirectToAction("Profile", "Account");
                }

                var leasesResult = await _leaseAgreementService.GetLeaseAgreementsByTenantIdAsync(tenantResult.Data.TenantId);
                if (!leasesResult.IsSuccess)
                {
                    SetErrorMessage($"Error loading leases: {leasesResult.ErrorMessage}");
                    return View(new List<LeaseAgreementDto>());
                }

                var leaseViewModels = _mapper.Map<List<LeaseAgreementViewModel>>(leasesResult.Data);
                ViewBag.TenantId = tenantResult.Data.TenantId;
                return View(leaseViewModels);
            }
            catch (Exception ex)
            {
                SetErrorMessage($"Error loading leases: {ex.Message}");
                return RedirectToAction("Login", "Auth");
            }
        }

        [HttpGet]
        public async Task<IActionResult> SignLease(int leaseAgreementId)
        {
            try
            {
                var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    SetErrorMessage("Invalid user session.");
                    return RedirectToAction("Login", "Tenants");
                }

                var tenantResult = await _tenantApplicationService.GetTenantByUserIdAsync(userId);
                if (!tenantResult.IsSuccess)
                {
                    SetErrorMessage("Tenant information not found.");
                    return RedirectToAction("Profile", "Tenants");
                }

                var leaseSigningResult = await _leaseGenerationService.GetLeaseForSigningAsync(leaseAgreementId, tenantResult.Data.TenantId);
                if (!leaseSigningResult.IsSuccess)
                {
                    // Provide more specific error messages based on the lease status
                    if (leaseSigningResult.ErrorMessage.Contains("not ready for signing"))
                    {
                        SetErrorMessage("? This lease is not ready for signing yet. The manager needs to generate the digital lease document and send it to you first.");
                        SetInfoMessage("?? Please contact your property manager to complete the lease preparation process.");
                    }
                    else if (leaseSigningResult.ErrorMessage.Contains("not found"))
                    {
                        SetErrorMessage("? Lease agreement not found or you don't have access to it.");
                    }
                    else
                    {
                        SetErrorMessage($"? Unable to access lease for signing: {leaseSigningResult.ErrorMessage}");
                    }
                    return RedirectToAction(nameof(MyLeases));
                }

                var viewModel = new LeaseSigningViewModel
                {
                    LeaseAgreement = leaseSigningResult.Data,
                    TenantId = tenantResult.Data.TenantId
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                SetErrorMessage($"Error loading lease for signing: {ex.Message}");
                return RedirectToAction(nameof(MyLeases));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitSignature(int leaseAgreementId, string signatureData, string signingNotes = "")
        {
            try
            {
                var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    return Json(new { success = false, message = "Invalid user session" });
                }

                var tenantResult = await _tenantApplicationService.GetTenantByUserIdAsync(userId);
                if (!tenantResult.IsSuccess)
                {
                    return Json(new { success = false, message = "Tenant information not found" });
                }

                var signLeaseDto = new SignLeaseDto
                {
                    LeaseAgreementId = leaseAgreementId,
                    SignatureDataUrl = signatureData,
                    SigningNotes = signingNotes,
                    SignerIPAddress = GetClientIpAddress(),
                    SignerUserAgent = Request.Headers["User-Agent"].ToString()
                };

                var result = await _leaseGenerationService.SignLeaseAsync(signLeaseDto);
                if (!result.IsSuccess)
                {
                    return Json(new { success = false, message = result.ErrorMessage });
                }

                return Json(new { success = true, message = "Lease signed successfully!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error signing lease: {ex.Message}" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> DownloadSignedLease(int leaseAgreementId)
        {
            try
            {
                var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    SetErrorMessage("Invalid user session.");
                    return RedirectToAction("Login", "Auth");
                }

                var tenantResult = await _tenantApplicationService.GetTenantByUserIdAsync(userId);
                if (!tenantResult.IsSuccess)
                {
                    SetErrorMessage("Tenant information not found.");
                    return RedirectToAction("Profile", "Account");
                }

                var result = await _leaseGenerationService.DownloadSignedLeaseAsync(leaseAgreementId, tenantResult.Data.TenantId);
                if (!result.IsSuccess || result.Data == null || result.Data.Length == 0)
                {
                    SetErrorMessage("Signed lease not found.");
                    return RedirectToAction(nameof(MyLeases));
                }

                var leaseResult = await _leaseAgreementService.GetLeaseAgreementByIdAsync(leaseAgreementId);
                var fileName = $"lease_{leaseAgreementId}_signed.pdf";
                var contentType = "application/pdf";
                if (leaseResult.IsSuccess && !string.IsNullOrEmpty(leaseResult.Data.GeneratedPdfPath) && leaseResult.Data.GeneratedPdfPath.EndsWith(".html"))
                {
                    contentType = "text/html";
                    fileName = $"lease_{leaseAgreementId}_signed.html";
                }
                return new FileContentResult(result.Data, contentType) { FileDownloadName = fileName };
            }
            catch (Exception ex)
            {
                SetErrorMessage($"Error downloading lease: {ex.Message}");
                return RedirectToAction(nameof(MyLeases));
            }
        }

        [HttpGet]
        public async Task<IActionResult> PreviewLease(int leaseAgreementId)
        {
            try
            {
                var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    return Unauthorized("Invalid user session");
                }

                var userRole = User.FindFirstValue(ClaimTypes.Role);

                // If the user is a manager, allow them to preview any lease
                if (userRole == "Manager")
                {
                    // For managers, get the lease directly using tenantId = 0 (bypasses tenant validation)
                    var leaseSigningResult = await _leaseGenerationService.GetLeaseForSigningAsync(leaseAgreementId, 0);
                    if (!leaseSigningResult.IsSuccess)
                    {
                        return NotFound(leaseSigningResult.ErrorMessage);
                    }

                    // Return HTML content for preview
                    if (!string.IsNullOrEmpty(leaseSigningResult.Data.GeneratedHtmlContent))
                    {
                        return Content(leaseSigningResult.Data.GeneratedHtmlContent, "text/html");
                    }

                    return NotFound("Lease content not available");
                }
                else
                {
                    // For tenants, validate they own this lease
                    var tenantResult = await _tenantApplicationService.GetTenantByUserIdAsync(userId);
                    if (!tenantResult.IsSuccess)
                    {
                        return Unauthorized("Tenant information not found");
                    }

                    var leaseSigningResult = await _leaseGenerationService.GetLeaseForSigningAsync(leaseAgreementId, tenantResult.Data.TenantId);
                    if (!leaseSigningResult.IsSuccess)
                    {
                        return NotFound(leaseSigningResult.ErrorMessage);
                    }

                    // Return HTML content for preview
                    if (!string.IsNullOrEmpty(leaseSigningResult.Data.GeneratedHtmlContent))
                    {
                        return Content(leaseSigningResult.Data.GeneratedHtmlContent, "text/html");
                    }

                    return NotFound("Lease content not available");
                }
            }
            catch (Exception ex)
            {
                return BadRequest($"Error previewing lease: {ex.Message}");
            }
        }

        // Template Management (Manager only)
        [HttpGet]
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> Templates()
        {
            try
            {
                var result = await _leaseGenerationService.GetLeaseTemplatesAsync();
                if (!result.IsSuccess)
                {
                    SetErrorMessage($"Error loading templates: {result.ErrorMessage}");
                    return View(new List<LeaseTemplateDto>());
                }

                var templateViewModels = _mapper.Map<List<LeaseTemplateViewModel>>(result.Data);
                return View(templateViewModels);
            }
            catch (Exception ex)
            {
                SetErrorMessage($"Error loading templates: {ex.Message}");
                return View(new List<LeaseTemplateViewModel>());
            }
        }

        [HttpGet]
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> CreateTemplate()
        {
            var viewModel = new LeaseTemplateViewModel();
            return View("EditTemplate", viewModel);
        }

        [HttpGet]
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> EditTemplate(int id)
        {
            try
            {
                var templates = await _leaseGenerationService.GetLeaseTemplatesAsync();
                var template = templates.Data?.FirstOrDefault(t => t.LeaseTemplateId == id);
                
                if (template == null)
                {
                    SetErrorMessage("Template not found.");
                    return RedirectToAction(nameof(Templates));
                }

                var viewModel = _mapper.Map<LeaseTemplateViewModel>(template);
                return View(viewModel);
            }
            catch (Exception ex)
            {
                SetErrorMessage($"Error loading template: {ex.Message}");
                return RedirectToAction(nameof(Templates));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> SaveTemplate(LeaseTemplateViewModel viewModel)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    SetErrorMessage("Please correct the errors in the form.");
                    return View("EditTemplate", viewModel);
                }

                if (viewModel.LeaseTemplateId == 0)
                {
                    // Create new template
                    var createDto = _mapper.Map<CreateLeaseTemplateDto>(viewModel);
                    var result = await _leaseGenerationService.CreateLeaseTemplateAsync(createDto);
                    
                    if (!result.IsSuccess)
                    {
                        SetErrorMessage($"Failed to create template: {result.ErrorMessage}");
                        return View("EditTemplate", viewModel);
                    }

                    SetSuccessMessage("Template saved successfully");
                }
                else
                {
                    // Update existing template
                    var updateDto = _mapper.Map<UpdateLeaseTemplateDto>(viewModel);
                    var result = await _leaseGenerationService.UpdateLeaseTemplateAsync(viewModel.LeaseTemplateId, updateDto);
                    
                    if (!result.IsSuccess)
                    {
                        SetErrorMessage($"Failed to update template: {result.ErrorMessage}");
                        return View("EditTemplate", viewModel);
                    }

                    SetSuccessMessage("Template saved successfully");
                }

                return RedirectToAction(nameof(Templates));
            }
            catch (Exception ex)
            {
                SetErrorMessage($"Error saving template: {ex.Message}");
                return View("EditTemplate", viewModel);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> DeleteTemplate(int id)
        {
            try
            {
                var result = await _leaseGenerationService.DeleteLeaseTemplateAsync(id);
                if (!result.IsSuccess)
                {
                    SetErrorMessage($"? Failed to delete template: {result.ErrorMessage}");
                }
                else
                {
                    SetSuccessMessage("? Template deleted successfully!");
                }

                return RedirectToAction(nameof(Templates));
            }
            catch (Exception ex)
            {
                SetErrorMessage($"? Error deleting template: {ex.Message}");
                return RedirectToAction(nameof(Templates));
            }
        }

        // Helper Methods
        private string GetClientIpAddress()
        {
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            
            // Check for forwarded IP (in case of proxy/load balancer)
            if (Request.Headers.ContainsKey("X-Forwarded-For"))
            {
                ipAddress = Request.Headers["X-Forwarded-For"].FirstOrDefault()?.Split(',')[0].Trim();
            }
            else if (Request.Headers.ContainsKey("X-Real-IP"))
            {
                ipAddress = Request.Headers["X-Real-IP"].FirstOrDefault();
            }

            return ipAddress ?? "Unknown";
        }

        // Quick Action Methods for Profile Cards
        [HttpGet]
        public async Task<IActionResult> QuickPreview()
        {
            try
            {
                var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    SetErrorMessage("Invalid user session.");
                    return RedirectToAction("Login", "Auth");
                }

                var tenantResult = await _tenantApplicationService.GetTenantByUserIdAsync(userId);
                if (!tenantResult.IsSuccess)
                {
                    SetErrorMessage("Tenant information not found.");
                    return RedirectToAction("Profile", "Account");
                }

                var leasesResult = await _leaseAgreementService.GetLeaseAgreementsByTenantIdAsync(tenantResult.Data.TenantId);
                if (!leasesResult.IsSuccess || !leasesResult.Data.Any())
                {
                    SetInfoMessage("No lease agreements found to preview.");
                    return RedirectToAction(nameof(MyLeases));
                }

                var leaseViewModels = _mapper.Map<List<LeaseAgreementViewModel>>(leasesResult.Data);
                var leaseToPreview = leaseViewModels
                    .FirstOrDefault(l => !string.IsNullOrEmpty(l.GeneratedHtmlContent));

                if (leaseToPreview != null)
                {
                    return RedirectToAction("PreviewLease", new { leaseAgreementId = leaseToPreview.LeaseAgreementId });
                }

                SetInfoMessage("No lease agreements available for preview.");
                return RedirectToAction(nameof(MyLeases));
            }
            catch (Exception ex)
            {
                SetErrorMessage($"Error accessing preview: {ex.Message}");
                return RedirectToAction(nameof(MyLeases));
            }
        }

        [HttpGet]
        public async Task<IActionResult> QuickSign()
        {
            try
            {
                var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    SetErrorMessage("Invalid user session.");
                    return RedirectToAction("Login", "Tenants");
                }

                var tenantResult = await _tenantApplicationService.GetTenantByUserIdAsync(userId);
                if (!tenantResult.IsSuccess)
                {
                    SetErrorMessage("Tenant information not found.");
                    return RedirectToAction("Profile", "Account");
                }

                var leasesResult = await _leaseAgreementService.GetLeaseAgreementsByTenantIdAsync(tenantResult.Data.TenantId);
                if (!leasesResult.IsSuccess || !leasesResult.Data.Any())
                {
                    SetInfoMessage("No lease agreements found to sign.");
                    return RedirectToAction(nameof(MyLeases));
                }

                var leaseViewModels = _mapper.Map<List<LeaseAgreementViewModel>>(leasesResult.Data);
                var leaseToSign = leaseViewModels
                    .FirstOrDefault(l => l.Status == Domain.Entities.LeaseAgreement.LeaseStatus.Sent && !l.IsDigitallySigned);

                if (leaseToSign != null)
                {
                    return RedirectToAction("SignLease", new { leaseAgreementId = leaseToSign.LeaseAgreementId });
                }

                SetInfoMessage("No lease agreements are currently available for signing.");
                return RedirectToAction(nameof(MyLeases));
            }
            catch (Exception ex)
            {
                SetErrorMessage($"Error accessing signing: {ex.Message}");
                return RedirectToAction(nameof(MyLeases));
            }
        }

        [HttpGet]
        public async Task<IActionResult> QuickDownload()
        {
            try
            {
                var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    SetErrorMessage("Invalid user session.");
                    return RedirectToAction("Login", "Tenants");
                }

                var tenantResult = await _tenantApplicationService.GetTenantByUserIdAsync(userId);
                if (!tenantResult.IsSuccess)
                {
                    SetErrorMessage("Tenant information not found.");
                    return RedirectToAction("Profile", "Account");
                }

                var leasesResult = await _leaseAgreementService.GetLeaseAgreementsByTenantIdAsync(tenantResult.Data.TenantId);
                if (!leasesResult.IsSuccess || !leasesResult.Data.Any())
                {
                    SetInfoMessage("No lease agreements found to download.");
                    return RedirectToAction(nameof(MyLeases));
                }

                // Convert to ViewModels to access digital properties
                var leaseViewModels = _mapper.Map<List<LeaseAgreementViewModel>>(leasesResult.Data);

                // Find the most recent signed lease
                var signedLease = leaseViewModels
                    .Where(l => l.IsDigitallySigned && !string.IsNullOrEmpty(l.GeneratedPdfPath))
                    .OrderByDescending(l => l.SignedAt ?? DateTime.MinValue)
                    .FirstOrDefault();

                if (signedLease == null)
                {
                    SetInfoMessage("No signed lease documents are available for download.");
                    return RedirectToAction(nameof(MyLeases));
                }

                return RedirectToAction(nameof(DownloadSignedLease), new { leaseAgreementId = signedLease.LeaseAgreementId });
            }
            catch (Exception ex)
            {
                SetErrorMessage($"Error accessing download: {ex.Message}");
                return RedirectToAction(nameof(MyLeases));
            }
        }

        // Debug action to help understand lease status
        [HttpGet]
        public async Task<IActionResult> LeaseStatusInfo(int leaseAgreementId)
        {
            try
            {
                var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    SetErrorMessage("Invalid user session.");
                    return RedirectToAction("Login", "Tenants");
                }

                var tenantResult = await _tenantApplicationService.GetTenantByUserIdAsync(userId);
                if (!tenantResult.IsSuccess)
                {
                    SetErrorMessage("Tenant information not found.");
                    return RedirectToAction("Profile", "Tenants");
                }

                var leasesResult = await _leaseAgreementService.GetLeaseAgreementsByTenantIdAsync(tenantResult.Data.TenantId);
                if (!leasesResult.IsSuccess)
                {
                    SetErrorMessage("Error loading lease information.");
                    return RedirectToAction(nameof(MyLeases));
                }

                var leaseViewModels = _mapper.Map<List<LeaseAgreementViewModel>>(leasesResult.Data);
                var lease = leaseViewModels.FirstOrDefault(l => l.LeaseAgreementId == leaseAgreementId);

                if (lease == null)
                {
                    SetErrorMessage("Lease not found or you don't have access to it.");
                    return RedirectToAction(nameof(MyLeases));
                }

                if (lease.Status == Domain.Entities.LeaseAgreement.LeaseStatus.Sent)
                {
                    TempData["Info"] = "Lease is ready for signing";
                }
                else
                {
                    var statusMessage = lease.Status switch
                    {
                        Domain.Entities.LeaseAgreement.LeaseStatus.Draft => "Draft - Your lease is being prepared by management.",
                        Domain.Entities.LeaseAgreement.LeaseStatus.Generated => "Generated - The digital lease has been created but not yet sent to you.",
                        Domain.Entities.LeaseAgreement.LeaseStatus.Signed => "Signed - The lease has been digitally signed and is complete.",
                        Domain.Entities.LeaseAgreement.LeaseStatus.Completed => "Completed - The lease process is fully complete.",
                        Domain.Entities.LeaseAgreement.LeaseStatus.Cancelled => "Cancelled - This lease has been cancelled.",
                        _ => "Unknown status"
                    };
                    TempData["Info"] = statusMessage;
                }

                return RedirectToAction(nameof(MyLeases));
            }
            catch (Exception ex)
            {
                SetErrorMessage($"Error retrieving lease status: {ex.Message}");
                return RedirectToAction(nameof(MyLeases));
            }
        }

        // Manager action to fix leases with inconsistent data
        [HttpPost]
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> RegenerateLeaseContent(int leaseAgreementId)
        {
            try
            {
                // Generate HTML content (this will update the status and content)
                var htmlResult = await _leaseGenerationService.GenerateLeaseHtmlAsync(leaseAgreementId, null);
                if (!htmlResult.IsSuccess)
                {
                    SetErrorMessage($"? Failed to regenerate lease content: {htmlResult.ErrorMessage}");
                    return RedirectToAction("Index", "LeaseAgreements");
                }

                // Generate PDF
                var pdfResult = await _leaseGenerationService.GenerateLeasePdfAsync(leaseAgreementId, htmlResult.Data);
                if (!pdfResult.IsSuccess)
                {
                    SetWarningMessage($"?? HTML generated successfully, but PDF generation failed: {pdfResult.ErrorMessage}");
                }
                else
                {
                    SetSuccessMessage("? Lease content and PDF regenerated successfully!");
                }

                // Note: Manager can manually send to tenant using the existing SendToTenant action
                SetInfoMessage("?? If this lease was previously sent to tenant, please use the 'Send to Tenant' action to notify them of the updated content.");

                return RedirectToAction("Index", "LeaseAgreements");
            }
            catch (Exception ex)
            {
                SetErrorMessage($"? Error regenerating lease content: {ex.Message}");
                return RedirectToAction("Index", "LeaseAgreements");
            }
        }

        // Diagnostic action to debug UI vs Database status mismatch
        [HttpGet]
        public async Task<IActionResult> DiagnoseStatusMismatch()
        {
            try
            {
                var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    SetErrorMessage("Invalid user session.");
                    return RedirectToAction("Login", "Tenants");
                }

                var tenantResult = await _tenantApplicationService.GetTenantByUserIdAsync(userId);
                if (!tenantResult.IsSuccess)
                {
                    SetErrorMessage("Tenant information not found.");
                    return RedirectToAction("Profile", "Tenants");
                }

                var leasesResult = await _leaseAgreementService.GetLeaseAgreementsByTenantIdAsync(tenantResult.Data.TenantId);
                if (!leasesResult.IsSuccess)
                {
                    SetErrorMessage("Error loading lease information.");
                    return RedirectToAction(nameof(MyLeases));
                }

                var leaseViewModels = _mapper.Map<List<LeaseAgreementViewModel>>(leasesResult.Data);

                if (!leaseViewModels.Any())
                {
                    SetInfoMessage("No leases found for diagnosis.");
                    return RedirectToAction(nameof(MyLeases));
                }

                // Generate detailed diagnostic information
                var diagnosticInfo = new List<string>();
                
                foreach (var lease in leaseViewModels)
                {
                    var statusInfo = GetStatusInfo(lease.Status);
                    var diagnosis = $"Lease ID {lease.LeaseAgreementId}: " +
                                  $"DB Status='{lease.Status}' ({(int)lease.Status}), " +
                                  $"UI Display='{statusInfo.DisplayName}', " +
                                  $"HasHtml={(lease.GeneratedHtmlContent != null ? "Yes" : "No")}, " +
                                  $"HasPdf={(lease.GeneratedPdfPath != null ? "Yes" : "No")}, " +
                                  $"IsSigned={lease.IsDigitallySigned}, " +
                                  $"GeneratedAt={(lease.GeneratedAt?.ToString("yyyy-MM-dd HH:mm") ?? "None")}, " +
                                  $"SentAt={(lease.SentToTenantAt?.ToString("yyyy-MM-dd HH:mm") ?? "None")}";
                    
                    diagnosticInfo.Add(diagnosis);
                }

                // Also get raw DTO data for comparison
                SetInfoMessage("?? Lease Status Diagnostic Information:");
                SetWarningMessage($"?? Raw DTOs from service: {string.Join(", ", leasesResult.Data.Select(d => $"ID {d.LeaseAgreementId}: Status='{d.Status}' ({(int)d.Status})"))}");
                
                foreach (var info in diagnosticInfo)
                {
                    SetWarningMessage($"?? {info}");
                }

                // Check for common issues
                var draftLeasesCount = leaseViewModels.Count(l => l.Status == Domain.Entities.LeaseAgreement.LeaseStatus.Draft);
                var sentLeasesCount = leaseViewModels.Count(l => l.Status == Domain.Entities.LeaseAgreement.LeaseStatus.Sent);
                var leasesWithHtmlCount = leaseViewModels.Count(l => !string.IsNullOrEmpty(l.GeneratedHtmlContent));

                SetSuccessMessage($"?? Summary: {leaseViewModels.Count} total lease(s), " +
                                $"{draftLeasesCount} Draft, {sentLeasesCount} Sent, {leasesWithHtmlCount} with HTML content");

                if (draftLeasesCount > 0 && sentLeasesCount == 0)
                {
                    SetErrorMessage("?? All leases are in Draft status. If you're seeing 'Awaiting Signature' in the UI, this indicates a display bug.");
                }

                return RedirectToAction(nameof(MyLeases));
            }
            catch (Exception ex)
            {
                SetErrorMessage($"? Error during diagnosis: {ex.Message}");
                return RedirectToAction(nameof(MyLeases));
            }
        }

        // Helper method for status info (matches the one in MyLeases.cshtml)
        private (string DisplayName, string CssClass) GetStatusInfo(Domain.Entities.LeaseAgreement.LeaseStatus status)
        {
            return status switch
            {
                Domain.Entities.LeaseAgreement.LeaseStatus.Draft => ("Draft", "bg-secondary"),
                Domain.Entities.LeaseAgreement.LeaseStatus.Generated => ("Generated", "bg-info"),
                Domain.Entities.LeaseAgreement.LeaseStatus.Sent => ("Awaiting Signature", "bg-warning text-dark"),
                Domain.Entities.LeaseAgreement.LeaseStatus.Signed => ("Signed", "bg-success"),
                Domain.Entities.LeaseAgreement.LeaseStatus.Completed => ("Completed", "bg-primary"),
                Domain.Entities.LeaseAgreement.LeaseStatus.Cancelled => ("Cancelled", "bg-danger"),
                _ => ("Unknown", "bg-secondary")
            };
        }

        // Direct database status check action (for debugging)
        [HttpGet]
        public async Task<IActionResult> CheckDatabaseStatus(int leaseAgreementId)
        {
            try
            {
                var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    return Json(new { success = false, message = "Invalid user session" });
                }

                // Get tenant info
                var tenantResult = await _tenantApplicationService.GetTenantByUserIdAsync(userId);
                if (!tenantResult.IsSuccess)
                {
                    return Json(new { success = false, message = "Tenant information not found" });
                }

                // Get the specific lease
                var leaseResult = await _leaseAgreementService.GetLeaseAgreementByIdAsync(leaseAgreementId);
                if (!leaseResult.IsSuccess)
                {
                    return Json(new { success = false, message = "Lease not found" });
                }

                // Check if tenant owns this lease
                if (leaseResult.Data.TenantId != tenantResult.Data.TenantId)
                {
                    return Json(new { success = false, message = "Access denied" });
                }

                // Map to ViewModel
                var leaseViewModel = _mapper.Map<LeaseAgreementViewModel>(leaseResult.Data);
                var statusInfo = GetStatusInfo(leaseViewModel.Status);

                return Json(new { 
                    success = true, 
                    data = new
                    {
                        LeaseId = leaseAgreementId,
                        RawDtoStatus = leaseResult.Data.Status.ToString(),
                        RawDtoStatusNumber = (int)leaseResult.Data.Status,
                        ViewModelStatus = leaseViewModel.Status.ToString(),
                        ViewModelStatusNumber = (int)leaseViewModel.Status,
                        UIDisplayName = statusInfo.DisplayName,
                        HasGeneratedHtml = !string.IsNullOrEmpty(leaseViewModel.GeneratedHtmlContent),
                        HasPdf = !string.IsNullOrEmpty(leaseViewModel.GeneratedPdfPath),
                        IsDigitallySigned = leaseViewModel.IsDigitallySigned,
                        GeneratedAt = leaseViewModel.GeneratedAt?.ToString("yyyy-MM-dd HH:mm:ss"),
                        SentToTenantAt = leaseViewModel.SentToTenantAt?.ToString("yyyy-MM-dd HH:mm:ss"),
                        SignedAt = leaseViewModel.SignedAt?.ToString("yyyy-MM-dd HH:mm:ss")
                    }
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }
    }
}