using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;
using PropertyManagement.Application.Common;
using PropertyManagement.Application.DTOs;
using PropertyManagement.Application.Services;
using PropertyManagement.Domain.Entities;
using PropertyManagement.Test.Infrastructure;
using PropertyManagement.Web.Controllers;
using PropertyManagement.Web.ViewModels;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Xunit;

namespace PropertyManagement.Test.Controllers
{
    public class DigitalLeaseControllerTests : BaseControllerTest
    {
        private readonly Mock<ILeaseGenerationService> _mockLeaseGenerationService;
        private readonly Mock<ILeaseAgreementApplicationService> _mockLeaseAgreementService;
        private readonly Mock<ITenantApplicationService> _mockTenantApplicationService;

        public DigitalLeaseControllerTests()
        {
            _mockLeaseGenerationService = new Mock<ILeaseGenerationService>();
            _mockLeaseAgreementService = new Mock<ILeaseAgreementApplicationService>();
            _mockTenantApplicationService = new Mock<ITenantApplicationService>();
        }

        private DigitalLeaseController GetController(string role = "Tenant", int userId = 1)
        {
            var controller = new DigitalLeaseController(
                _mockLeaseGenerationService.Object,
                _mockLeaseAgreementService.Object,
                _mockTenantApplicationService.Object,
                Mapper);

            SetupControllerContext(controller, GetTestUser(role, userId));
            return controller;
        }

        [Fact]
        public async Task GenerateLease_WithValidId_ReturnsRedirectWithSuccess()
        {
            // Arrange
            int leaseAgreementId = 1;
            var htmlContent = "<html><body>Generated Lease Content</body></html>";
            var pdfPath = "/uploads/leases/lease_1.pdf";

            _mockLeaseGenerationService.Setup(s => s.GenerateLeaseHtmlAsync(leaseAgreementId, null))
                .ReturnsAsync(ServiceResult<string>.Success(htmlContent));
            
            _mockLeaseGenerationService.Setup(s => s.GenerateLeasePdfAsync(leaseAgreementId, htmlContent))
                .ReturnsAsync(ServiceResult<string>.Success(pdfPath));

            var controller = GetController("Manager");

            // Act
            var result = await controller.GenerateLease(leaseAgreementId);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);
            Assert.Equal("LeaseAgreements", redirectResult.ControllerName);
            Assert.Contains("generated successfully", controller.TempData["Success"]?.ToString());
        }

        [Fact]
        public async Task GenerateLease_HtmlGenerationFails_ReturnsRedirectWithError()
        {
            // Arrange
            int leaseAgreementId = 1;
            
            _mockLeaseGenerationService.Setup(s => s.GenerateLeaseHtmlAsync(leaseAgreementId, null))
                .ReturnsAsync(ServiceResult<string>.Failure("Template not found"));

            var controller = GetController("Manager");

            // Act
            var result = await controller.GenerateLease(leaseAgreementId);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Contains("Failed to generate lease", controller.TempData["Error"]?.ToString());
        }

        [Fact]
        public async Task GenerateLease_PdfGenerationFails_ReturnsRedirectWithError()
        {
            // Arrange
            int leaseAgreementId = 1;
            var htmlContent = "<html>Content</html>";

            _mockLeaseGenerationService.Setup(s => s.GenerateLeaseHtmlAsync(leaseAgreementId, null))
                .ReturnsAsync(ServiceResult<string>.Success(htmlContent));
            
            _mockLeaseGenerationService.Setup(s => s.GenerateLeasePdfAsync(leaseAgreementId, htmlContent))
                .ReturnsAsync(ServiceResult<string>.Failure("PDF generation failed"));

            var controller = GetController("Manager");

            // Act
            var result = await controller.GenerateLease(leaseAgreementId);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Contains("Failed to generate lease", controller.TempData["Error"]?.ToString());
        }

        [Fact]
        public async Task SendToTenant_WithValidId_ReturnsRedirectWithSuccess()
        {
            // Arrange
            int leaseAgreementId = 1;

            _mockLeaseGenerationService.Setup(s => s.SendLeaseToTenantAsync(leaseAgreementId))
                .ReturnsAsync(ServiceResult<bool>.Success(true));

            var controller = GetController("Manager");

            // Act
            var result = await controller.SendToTenant(leaseAgreementId);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);
            Assert.Equal("LeaseAgreements", redirectResult.ControllerName);
            Assert.Contains("sent to tenant for signing", controller.TempData["Success"]?.ToString());
        }

        [Fact]
        public async Task SendToTenant_ServiceFails_ReturnsRedirectWithError()
        {
            // Arrange
            int leaseAgreementId = 1;

            _mockLeaseGenerationService.Setup(s => s.SendLeaseToTenantAsync(leaseAgreementId))
                .ReturnsAsync(ServiceResult<bool>.Failure("Lease must be generated before sending"));

            var controller = GetController("Manager");

            // Act
            var result = await controller.SendToTenant(leaseAgreementId);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Contains("Failed to send lease", controller.TempData["Error"]?.ToString());
        }

        [Fact]
        public async Task MyLeases_WithValidTenant_ReturnsViewWithLeases()
        {
            // Arrange
            var tenantDto = new TenantDto { TenantId = 1, FullName = "Test Tenant" };
            var leaseDtos = new List<LeaseAgreementDto>
            {
                new LeaseAgreementDto { LeaseAgreementId = 1, TenantId = 1 },
                new LeaseAgreementDto { LeaseAgreementId = 2, TenantId = 1 }
            };

            _mockTenantApplicationService.Setup(s => s.GetTenantByUserIdAsync(1))
                .ReturnsAsync(ServiceResult<TenantDto>.Success(tenantDto));
            _mockLeaseAgreementService.Setup(s => s.GetLeaseAgreementsByTenantIdAsync(1))
                .ReturnsAsync(ServiceResult<IEnumerable<LeaseAgreementDto>>.Success(leaseDtos));

            var controller = GetController("Tenant", userId: 1);

            // Act
            var result = await controller.MyLeases();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<List<LeaseAgreementViewModel>>(viewResult.Model);
            Assert.Equal(2, model.Count);
        }

        [Fact]
        public async Task MyLeases_TenantNotFound_RedirectsToProfile()
        {
            // Arrange
            _mockTenantApplicationService.Setup(s => s.GetTenantByUserIdAsync(1))
                .ReturnsAsync(ServiceResult<TenantDto>.Failure("Tenant not found"));

            var controller = GetController("Tenant", userId: 1);

            // Act
            var result = await controller.MyLeases();

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Profile", redirectResult.ActionName);
            Assert.Equal("Account", redirectResult.ControllerName);
        }

        [Fact]
        public async Task MyLeases_InvalidUserSession_RedirectsToLogin()
        {
            // Arrange
            var controller = GetController("Tenant", userId: 0); // Invalid user ID

            // Act
            var result = await controller.MyLeases();

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Login", redirectResult.ActionName);
            Assert.Equal("Auth", redirectResult.ControllerName);
        }

        [Fact]
        public async Task SignLease_WithValidRequest_ReturnsSigningView()
        {
            // Arrange
            int leaseAgreementId = 1;
            var tenantDto = new TenantDto { TenantId = 1, FullName = "Test Tenant" };
            var signingDto = new LeaseAgreementSigningDto 
            { 
                LeaseAgreementId = leaseAgreementId,
                Status = LeaseAgreement.LeaseStatus.Sent,
                GeneratedHtmlContent = "<html>Ready to sign</html>"
            };

            _mockTenantApplicationService.Setup(s => s.GetTenantByUserIdAsync(1))
                .ReturnsAsync(ServiceResult<TenantDto>.Success(tenantDto));
            _mockLeaseGenerationService.Setup(s => s.GetLeaseForSigningAsync(leaseAgreementId, 1))
                .ReturnsAsync(ServiceResult<LeaseAgreementSigningDto>.Success(signingDto));

            var controller = GetController("Tenant", userId: 1);

            // Act
            var result = await controller.SignLease(leaseAgreementId);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<LeaseSigningViewModel>(viewResult.Model);
            Assert.Equal(leaseAgreementId, model.LeaseAgreement.LeaseAgreementId);
        }

        [Fact]
        public async Task SignLease_LeaseNotReadyForSigning_RedirectsWithError()
        {
            // Arrange
            int leaseAgreementId = 1;
            var tenantDto = new TenantDto { TenantId = 1, FullName = "Test Tenant" };

            _mockTenantApplicationService.Setup(s => s.GetTenantByUserIdAsync(1))
                .ReturnsAsync(ServiceResult<TenantDto>.Success(tenantDto));
            _mockLeaseGenerationService.Setup(s => s.GetLeaseForSigningAsync(leaseAgreementId, 1))
                .ReturnsAsync(ServiceResult<LeaseAgreementSigningDto>.Failure("Lease agreement is not ready for signing"));

            var controller = GetController("Tenant", userId: 1);

            // Act
            var result = await controller.SignLease(leaseAgreementId);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("MyLeases", redirectResult.ActionName);
            Assert.Contains("not ready for signing", controller.TempData["Error"]?.ToString());
        }

        [Fact]
        public async Task SubmitSignature_WithValidData_ReturnsSuccessJson()
        {
            // Arrange
            int leaseAgreementId = 1;
            string signatureData = "data:image/png;base64,testSignatureData";
            var tenantDto = new TenantDto { TenantId = 1, FullName = "Test Tenant" };
            var signatureDto = new DigitalSignatureDto 
            { 
                DigitalSignatureId = 1,
                LeaseAgreementId = leaseAgreementId,
                TenantId = 1,
                SignedDate = DateTime.UtcNow
            };

            _mockTenantApplicationService.Setup(s => s.GetTenantByUserIdAsync(1))
                .ReturnsAsync(ServiceResult<TenantDto>.Success(tenantDto));
            _mockLeaseGenerationService.Setup(s => s.SignLeaseAsync(It.IsAny<SignLeaseDto>()))
                .ReturnsAsync(ServiceResult<DigitalSignatureDto>.Success(signatureDto));

            var controller = GetController("Tenant", userId: 1);

            // Act
            var result = await controller.SubmitSignature(leaseAgreementId, signatureData, "Test signing");

            // Assert
            var jsonResult = Assert.IsType<JsonResult>(result);
            var valueType = jsonResult.Value.GetType();
            var successProp = valueType.GetProperty("success");
            var messageProp = valueType.GetProperty("message");
            Assert.NotNull(successProp);
            Assert.NotNull(messageProp);
            Assert.True((bool)successProp.GetValue(jsonResult.Value));
            Assert.Equal("Lease signed successfully!", (string)messageProp.GetValue(jsonResult.Value));
        }

        [Fact]
        public async Task SubmitSignature_SigningFails_ReturnsErrorJson()
        {
            // Arrange
            int leaseAgreementId = 1;
            string signatureData = "data:image/png;base64,testData";
            var tenantDto = new TenantDto { TenantId = 1, FullName = "Test Tenant" };

            _mockTenantApplicationService.Setup(s => s.GetTenantByUserIdAsync(1))
                .ReturnsAsync(ServiceResult<TenantDto>.Success(tenantDto));
            _mockLeaseGenerationService.Setup(s => s.SignLeaseAsync(It.IsAny<SignLeaseDto>()))
                .ReturnsAsync(ServiceResult<DigitalSignatureDto>.Failure("Lease agreement is already signed"));

            var controller = GetController("Tenant", userId: 1);

            // Act
            var result = await controller.SubmitSignature(leaseAgreementId, signatureData);

            // Assert
            var jsonResult = Assert.IsType<JsonResult>(result);
            var valueType = jsonResult.Value.GetType();
            var successProp = valueType.GetProperty("success");
            var messageProp = valueType.GetProperty("message");
            Assert.NotNull(successProp);
            Assert.NotNull(messageProp);
            Assert.False((bool)successProp.GetValue(jsonResult.Value));
            Assert.Equal("Lease agreement is already signed", (string)messageProp.GetValue(jsonResult.Value));
        }

        [Fact]
        public async Task DownloadSignedLease_WithValidRequest_ReturnsFile()
        {
            // Arrange
            int leaseAgreementId = 1;
            var fileContent = new byte[] { 1, 2, 3, 4, 5 };
            var fileName = "lease_1_signed.pdf";
            var tenantDto = new TenantDto { TenantId = 1, FullName = "Test Tenant" };
            var leaseDto = new LeaseAgreementDto {
                LeaseAgreementId = leaseAgreementId,
                TenantId = tenantDto.TenantId,
                GeneratedPdfPath = fileName
            };

            _mockTenantApplicationService.Setup(s => s.GetTenantByUserIdAsync(1))
                .ReturnsAsync(ServiceResult<TenantDto>.Success(tenantDto));
            _mockLeaseAgreementService.Setup(s => s.GetLeaseAgreementByIdAsync(leaseAgreementId))
                .ReturnsAsync(ServiceResult<LeaseAgreementDto>.Success(leaseDto));
            _mockLeaseGenerationService.Setup(s => s.DownloadSignedLeaseAsync(leaseAgreementId, tenantDto.TenantId))
                .ReturnsAsync(ServiceResult<byte[]>.Success(fileContent));

            var controller = GetController("Tenant");

            // Act
            var result = await controller.DownloadSignedLease(leaseAgreementId);

            // Assert
            var fileResult = Assert.IsType<FileContentResult>(result);
            Assert.Equal(fileContent, fileResult.FileContents);
            Assert.Equal("application/pdf", fileResult.ContentType);
            Assert.Equal(fileName, fileResult.FileDownloadName);
        }

        [Fact]
        public async Task PreviewLease_AsManager_ReturnsHtmlContent()
        {
            // Arrange
            int leaseAgreementId = 1;
            var htmlContent = "<html><body>Lease Preview</body></html>";
            var signingDto = new LeaseAgreementSigningDto
            {
                LeaseAgreementId = leaseAgreementId,
                Status = LeaseAgreement.LeaseStatus.Sent,
                GeneratedHtmlContent = htmlContent
            };
            _mockLeaseGenerationService.Setup(s => s.GetLeaseForSigningAsync(leaseAgreementId, 0))
                .ReturnsAsync(ServiceResult<LeaseAgreementSigningDto>.Success(signingDto));

            var controller = GetController("Manager");

            // Act
            var result = await controller.PreviewLease(leaseAgreementId);

            // Assert
            var contentResult = Assert.IsType<ContentResult>(result);
            Assert.Equal("text/html", contentResult.ContentType);
            Assert.Equal(htmlContent, contentResult.Content);
        }

        [Fact]
        public async Task PreviewLease_AsTenant_WithValidLease_ReturnsHtmlContent()
        {
            // Arrange
            int leaseAgreementId = 1;
            var tenantDto = new TenantDto { TenantId = 1, FullName = "Test Tenant" };
            var htmlContent = "<html><body>Tenant Lease Preview</body></html>";
            var signingDto = new LeaseAgreementSigningDto
            {
                LeaseAgreementId = leaseAgreementId,
                Status = LeaseAgreement.LeaseStatus.Sent,
                GeneratedHtmlContent = htmlContent
            };
            _mockTenantApplicationService.Setup(s => s.GetTenantByUserIdAsync(1))
                .ReturnsAsync(ServiceResult<TenantDto>.Success(tenantDto));
            _mockLeaseGenerationService.Setup(s => s.GetLeaseForSigningAsync(leaseAgreementId, tenantDto.TenantId))
                .ReturnsAsync(ServiceResult<LeaseAgreementSigningDto>.Success(signingDto));

            var controller = GetController("Tenant", userId: 1);

            // Act
            var result = await controller.PreviewLease(leaseAgreementId);

            // Assert
            var contentResult = Assert.IsType<ContentResult>(result);
            Assert.Equal("text/html", contentResult.ContentType);
            Assert.Equal(htmlContent, contentResult.Content);
        }

        [Fact]
        public async Task Templates_AsManager_ReturnsViewWithTemplates()
        {
            // Arrange
            var templates = new List<LeaseTemplateDto>
            {
                new LeaseTemplateDto { LeaseTemplateId = 1, Name = "Standard Template" },
                new LeaseTemplateDto { LeaseTemplateId = 2, Name = "Premium Template" }
            };

            _mockLeaseGenerationService.Setup(s => s.GetLeaseTemplatesAsync())
                .ReturnsAsync(ServiceResult<IEnumerable<LeaseTemplateDto>>.Success(templates));

            var controller = GetController("Manager");

            // Act
            var result = await controller.Templates();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<List<LeaseTemplateViewModel>>(viewResult.Model);
            Assert.Equal(2, model.Count);
        }

        [Fact]
        public async Task SaveTemplate_CreateNew_ReturnsRedirectWithSuccess()
        {
            // Arrange
            var templateViewModel = new LeaseTemplateViewModel
            {
                Name = "New Template",
                HtmlContent = "<html>Template Content</html>"
            };

            var createdTemplate = new LeaseTemplateDto
            {
                LeaseTemplateId = 1,
                Name = "New Template"
            };

            _mockLeaseGenerationService.Setup(s => s.CreateLeaseTemplateAsync(It.IsAny<CreateLeaseTemplateDto>()))
                .ReturnsAsync(ServiceResult<LeaseTemplateDto>.Success(createdTemplate));

            var controller = GetController("Manager");

            // Act
            var result = await controller.SaveTemplate(templateViewModel);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Templates", redirectResult.ActionName);
            Assert.Contains("Template saved successfully", controller.TempData["Success"]?.ToString());
        }

        [Fact]
        public async Task SaveTemplate_UpdateExisting_ReturnsRedirectWithSuccess()
        {
            // Arrange
            var templateViewModel = new LeaseTemplateViewModel
            {
                LeaseTemplateId = 1,
                Name = "Updated Template",
                HtmlContent = "<html>Updated Content</html>"
            };

            var updatedTemplate = new LeaseTemplateDto
            {
                LeaseTemplateId = 1,
                Name = "Updated Template"
            };

            _mockLeaseGenerationService.Setup(s => s.UpdateLeaseTemplateAsync(1, It.IsAny<UpdateLeaseTemplateDto>()))
                .ReturnsAsync(ServiceResult<LeaseTemplateDto>.Success(updatedTemplate));

            var controller = GetController("Manager");

            // Act
            var result = await controller.SaveTemplate(templateViewModel);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Templates", redirectResult.ActionName);
            Assert.Contains("Template saved successfully", controller.TempData["Success"]?.ToString());
        }

        [Fact]
        public async Task DeleteTemplate_WithValidId_ReturnsRedirectWithSuccess()
        {
            // Arrange
            int templateId = 1;

            _mockLeaseGenerationService.Setup(s => s.DeleteLeaseTemplateAsync(templateId))
                .ReturnsAsync(ServiceResult<bool>.Success(true));

            var controller = GetController("Manager");

            // Act
            var result = await controller.DeleteTemplate(templateId);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Templates", redirectResult.ActionName);
            Assert.Contains("Template deleted successfully", controller.TempData["Success"]?.ToString());
        }

        [Fact]
        public async Task QuickPreview_WithAvailableContent_RedirectsToPreview()
        {
            // Arrange
            int leaseAgreementId = 1;
            var htmlContent = "<html><body>Lease Preview</body></html>";
            var tenantDto = new TenantDto { TenantId = 1, FullName = "Test Tenant" };
            var leaseDtos = new List<LeaseAgreementDto>
            {
                new LeaseAgreementDto {
                    LeaseAgreementId = leaseAgreementId,
                    TenantId = tenantDto.TenantId,
                    GeneratedHtmlContent = htmlContent,
                    Status = LeaseAgreement.LeaseStatus.Sent
                }
            };

            _mockTenantApplicationService.Setup(s => s.GetTenantByUserIdAsync(1))
                .ReturnsAsync(ServiceResult<TenantDto>.Success(tenantDto));
            _mockLeaseAgreementService.Setup(s => s.GetLeaseAgreementsByTenantIdAsync(tenantDto.TenantId))
                .ReturnsAsync(ServiceResult<IEnumerable<LeaseAgreementDto>>.Success(leaseDtos));

            var controller = GetController("Tenant", userId: 1);

            // Act
            var result = await controller.QuickPreview();

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("PreviewLease", redirectResult.ActionName);
            Assert.Equal(leaseAgreementId, redirectResult.RouteValues["leaseAgreementId"]);
        }

        [Fact]
        public async Task QuickSign_WithAvailableLease_RedirectsToSign()
        {
            // Arrange
            int leaseAgreementId = 1;
            var tenantDto = new TenantDto { TenantId = 1, FullName = "Test Tenant" };
            var leaseDtos = new List<LeaseAgreementDto>
            {
                new LeaseAgreementDto {
                    LeaseAgreementId = leaseAgreementId,
                    TenantId = tenantDto.TenantId,
                    Status = LeaseAgreement.LeaseStatus.Sent,
                    IsDigitallySigned = false
                }
            };

            _mockTenantApplicationService.Setup(s => s.GetTenantByUserIdAsync(1))
                .ReturnsAsync(ServiceResult<TenantDto>.Success(tenantDto));
            _mockLeaseAgreementService.Setup(s => s.GetLeaseAgreementsByTenantIdAsync(tenantDto.TenantId))
                .ReturnsAsync(ServiceResult<IEnumerable<LeaseAgreementDto>>.Success(leaseDtos));

            var controller = GetController("Tenant", userId: 1);

            // Act
            var result = await controller.QuickSign();

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("SignLease", redirectResult.ActionName);
            Assert.Equal(leaseAgreementId, redirectResult.RouteValues["leaseAgreementId"]);
        }

        [Fact]
        public async Task LeaseStatusInfo_WithValidLease_RedirectsWithStatusMessages()
        {
            // Arrange
            int leaseAgreementId = 1;
            var tenantDto = new TenantDto { TenantId = 1, FullName = "Test Tenant" };
            var leaseDtos = new List<LeaseAgreementDto>
            {
                new LeaseAgreementDto {
                    LeaseAgreementId = leaseAgreementId,
                    TenantId = tenantDto.TenantId,
                    Status = LeaseAgreement.LeaseStatus.Sent
                }
            };

            _mockTenantApplicationService.Setup(s => s.GetTenantByUserIdAsync(1))
                .ReturnsAsync(ServiceResult<TenantDto>.Success(tenantDto));
            _mockLeaseAgreementService.Setup(s => s.GetLeaseAgreementsByTenantIdAsync(tenantDto.TenantId))
                .ReturnsAsync(ServiceResult<IEnumerable<LeaseAgreementDto>>.Success(leaseDtos));

            var controller = GetController("Tenant", userId: 1);

            // Act
            var result = await controller.LeaseStatusInfo(leaseAgreementId);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("MyLeases", redirectResult.ActionName);
            Assert.Contains("Lease is ready for signing", controller.TempData["Info"]?.ToString());
        }
    }
}