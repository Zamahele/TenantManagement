using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using PropertyManagement.Application.Common;
using PropertyManagement.Application.DTOs;
using PropertyManagement.Application.Services;
using PropertyManagement.Domain.Entities;
using PropertyManagement.Web.Controllers;
using PropertyManagement.Web.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Xunit;

namespace PropertyManagement.Test.Controllers
{
    public class DigitalLeaseControllerTests
    {
        private readonly Mock<ILeaseGenerationService> _mockLeaseGenerationService;
        private readonly Mock<ILeaseAgreementApplicationService> _mockLeaseAgreementService;
        private readonly Mock<ITenantApplicationService> _mockTenantApplicationService;
        private readonly IMapper _mapper;
        private readonly DigitalLeaseController _controller;

        public DigitalLeaseControllerTests()
        {
            _mockLeaseGenerationService = new Mock<ILeaseGenerationService>();
            _mockLeaseAgreementService = new Mock<ILeaseAgreementApplicationService>();
            _mockTenantApplicationService = new Mock<ITenantApplicationService>();
            _mapper = GetMapper();

            _controller = new DigitalLeaseController(
                _mockLeaseGenerationService.Object,
                _mockLeaseAgreementService.Object,
                _mockTenantApplicationService.Object,
                _mapper);

            SetupControllerContext();
        }

        private IMapper GetMapper()
        {
            var expr = new MapperConfigurationExpression();
            expr.CreateMap<LeaseAgreementDto, LeaseAgreementViewModel>()
               .ForMember(dest => dest.Status, opt => opt.MapFrom(src => LeaseAgreement.LeaseStatus.Draft))
               .ReverseMap();
            expr.CreateMap<TenantDto, TenantViewModel>().ReverseMap();
            expr.CreateMap<RoomDto, RoomViewModel>().ReverseMap();
            expr.CreateMap<DigitalSignatureDto, DigitalSignatureViewModel>().ReverseMap();
            expr.CreateMap<LeaseTemplateDto, LeaseTemplateViewModel>().ReverseMap();
            expr.CreateMap<LeaseTemplateViewModel, CreateLeaseTemplateDto>();
            expr.CreateMap<LeaseTemplateViewModel, UpdateLeaseTemplateDto>();
            
            var config = new MapperConfiguration(expr, NullLoggerFactory.Instance);
            return config.CreateMapper();
        }

        private void SetupControllerContext(string role = "Tenant", int userId = 1)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Role, role)
            };

            var identity = new ClaimsIdentity(claims, "test");
            var principal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            };

            _controller.TempData = new TempDataDictionary(
                _controller.ControllerContext.HttpContext,
                Mock.Of<ITempDataProvider>());
        }

        #region Manager Actions Tests

        [Fact]
        public async Task GenerateLease_WithValidId_ReturnsRedirectWithSuccess()
        {
            // Arrange
            SetupControllerContext("Manager");
            int leaseAgreementId = 1;
            
            _mockLeaseGenerationService.Setup(s => s.GenerateLeaseHtmlAsync(leaseAgreementId, null))
                .ReturnsAsync(ServiceResult<string>.Success("<html>Test</html>"));
            
            _mockLeaseGenerationService.Setup(s => s.GenerateLeasePdfAsync(leaseAgreementId, It.IsAny<string>()))
                .ReturnsAsync(ServiceResult<string>.Success("/uploads/lease.pdf"));

            // Act
            var result = await _controller.GenerateLease(leaseAgreementId);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);
            Assert.Equal("LeaseAgreements", redirectResult.ControllerName);
            Assert.Equal("? Lease agreement generated successfully! PDF created at: /uploads/lease.pdf", _controller.TempData["Success"]);
        }

        [Fact]
        public async Task GenerateLease_HtmlGenerationFails_ReturnsRedirectWithError()
        {
            // Arrange
            SetupControllerContext("Manager");
            int leaseAgreementId = 1;
            
            _mockLeaseGenerationService.Setup(s => s.GenerateLeaseHtmlAsync(leaseAgreementId, null))
                .ReturnsAsync(ServiceResult<string>.Failure("HTML generation failed"));

            // Act
            var result = await _controller.GenerateLease(leaseAgreementId);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);
            Assert.Equal("LeaseAgreements", redirectResult.ControllerName);
            Assert.Equal("? Failed to generate lease: HTML generation failed", _controller.TempData["Error"]);
        }

        [Fact]
        public async Task GenerateLease_PdfGenerationFails_ReturnsRedirectWithError()
        {
            // Arrange
            SetupControllerContext("Manager");
            int leaseAgreementId = 1;
            
            _mockLeaseGenerationService.Setup(s => s.GenerateLeaseHtmlAsync(leaseAgreementId, null))
                .ReturnsAsync(ServiceResult<string>.Success("<html>Test</html>"));
            
            _mockLeaseGenerationService.Setup(s => s.GenerateLeasePdfAsync(leaseAgreementId, It.IsAny<string>()))
                .ReturnsAsync(ServiceResult<string>.Failure("PDF generation failed"));

            // Act
            var result = await _controller.GenerateLease(leaseAgreementId);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("? Failed to generate PDF: PDF generation failed", _controller.TempData["Error"]);
        }

        [Fact]
        public async Task SendToTenant_WithValidId_ReturnsRedirectWithSuccess()
        {
            // Arrange
            SetupControllerContext("Manager");
            int leaseAgreementId = 1;
            
            _mockLeaseGenerationService.Setup(s => s.SendLeaseToTenantAsync(leaseAgreementId))
                .ReturnsAsync(ServiceResult<bool>.Success(true));

            // Act
            var result = await _controller.SendToTenant(leaseAgreementId);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);
            Assert.Equal("LeaseAgreements", redirectResult.ControllerName);
            Assert.Equal("? Lease agreement sent to tenant for signing!", _controller.TempData["Success"]);
        }

        [Fact]
        public async Task SendToTenant_ServiceFails_ReturnsRedirectWithError()
        {
            // Arrange
            SetupControllerContext("Manager");
            int leaseAgreementId = 1;
            
            _mockLeaseGenerationService.Setup(s => s.SendLeaseToTenantAsync(leaseAgreementId))
                .ReturnsAsync(ServiceResult<bool>.Failure("Send failed"));

            // Act
            var result = await _controller.SendToTenant(leaseAgreementId);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("? Failed to send lease: Send failed", _controller.TempData["Error"]);
        }

        #endregion

        #region Tenant Actions Tests

        [Fact]
        public async Task MyLeases_WithValidTenant_ReturnsViewWithLeases()
        {
            // Arrange
            var tenantDto = new TenantDto { TenantId = 1, FullName = "Test Tenant" };
            var leaseDto = new LeaseAgreementDto { LeaseAgreementId = 1, TenantId = 1 };
            
            _mockTenantApplicationService.Setup(s => s.GetTenantByUserIdAsync(1))
                .ReturnsAsync(ServiceResult<TenantDto>.Success(tenantDto));
            
            _mockLeaseAgreementService.Setup(s => s.GetLeaseAgreementsByTenantIdAsync(1))
                .ReturnsAsync(ServiceResult<IEnumerable<LeaseAgreementDto>>.Success(new[] { leaseDto }));

            // Act
            var result = await _controller.MyLeases();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<List<LeaseAgreementViewModel>>(viewResult.Model);
            Assert.Single(model);
            Assert.Equal(1, model.First().LeaseAgreementId);
        }

        [Fact]
        public async Task MyLeases_TenantNotFound_RedirectsToProfile()
        {
            // Arrange
            _mockTenantApplicationService.Setup(s => s.GetTenantByUserIdAsync(1))
                .ReturnsAsync(ServiceResult<TenantDto>.Failure("Tenant not found"));

            // Act
            var result = await _controller.MyLeases();

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Profile", redirectResult.ActionName);
            Assert.Equal("Tenants", redirectResult.ControllerName);
            Assert.Equal("Tenant information not found.", _controller.TempData["Error"]);
        }

        [Fact]
        public async Task MyLeases_InvalidUserSession_RedirectsToLogin()
        {
            // Arrange
            SetupControllerContext("Tenant", 0); // Invalid user ID

            // Act
            var result = await _controller.MyLeases();

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Login", redirectResult.ActionName);
            Assert.Equal("Tenants", redirectResult.ControllerName);
            Assert.Equal("Invalid user session.", _controller.TempData["Error"]);
        }

        [Fact]
        public async Task SignLease_WithValidRequest_ReturnsSigningView()
        {
            // Arrange
            int leaseAgreementId = 1;
            var tenantDto = new TenantDto { TenantId = 1, FullName = "Test Tenant" };
            var signingDto = new LeaseAgreementSigningDto 
            { 
                LeaseAgreementId = 1, 
                TenantName = "Test Tenant",
                Status = LeaseAgreement.LeaseStatus.Sent
            };

            _mockTenantApplicationService.Setup(s => s.GetTenantByUserIdAsync(1))
                .ReturnsAsync(ServiceResult<TenantDto>.Success(tenantDto));
            
            _mockLeaseGenerationService.Setup(s => s.GetLeaseForSigningAsync(leaseAgreementId, 1))
                .ReturnsAsync(ServiceResult<LeaseAgreementSigningDto>.Success(signingDto));

            // Act
            var result = await _controller.SignLease(leaseAgreementId);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<LeaseSigningViewModel>(viewResult.Model);
            Assert.Equal(1, model.LeaseAgreement.LeaseAgreementId);
            Assert.Equal(1, model.TenantId);
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

            // Act
            var result = await _controller.SignLease(leaseAgreementId);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("MyLeases", redirectResult.ActionName);
            Assert.Equal("? This lease is not ready for signing yet. The manager needs to generate the digital lease document and send it to you first.", _controller.TempData["Error"]);
            Assert.Equal("?? Please contact your property manager to complete the lease preparation process.", _controller.TempData["Info"]);
        }

        [Fact]
        public async Task SubmitSignature_WithValidData_ReturnsSuccessJson()
        {
            // Arrange
            int leaseAgreementId = 1;
            string signatureData = "data:image/png;base64,test";
            string signingNotes = "Test notes";
            
            var tenantDto = new TenantDto { TenantId = 1, FullName = "Test Tenant" };
            var signatureDto = new DigitalSignatureDto { DigitalSignatureId = 1 };

            _mockTenantApplicationService.Setup(s => s.GetTenantByUserIdAsync(1))
                .ReturnsAsync(ServiceResult<TenantDto>.Success(tenantDto));
            
            _mockLeaseGenerationService.Setup(s => s.SignLeaseAsync(It.IsAny<SignLeaseDto>()))
                .ReturnsAsync(ServiceResult<DigitalSignatureDto>.Success(signatureDto));

            // Act
            var result = await _controller.SubmitSignature(leaseAgreementId, signatureData, signingNotes);

            // Assert
            var jsonResult = Assert.IsType<JsonResult>(result);
            dynamic data = jsonResult.Value;
            Assert.True((bool)data.success);
            Assert.Equal("Lease signed successfully!", (string)data.message);
        }

        [Fact]
        public async Task SubmitSignature_SigningFails_ReturnsErrorJson()
        {
            // Arrange
            int leaseAgreementId = 1;
            string signatureData = "data:image/png;base64,test";
            
            var tenantDto = new TenantDto { TenantId = 1, FullName = "Test Tenant" };

            _mockTenantApplicationService.Setup(s => s.GetTenantByUserIdAsync(1))
                .ReturnsAsync(ServiceResult<TenantDto>.Success(tenantDto));
            
            _mockLeaseGenerationService.Setup(s => s.SignLeaseAsync(It.IsAny<SignLeaseDto>()))
                .ReturnsAsync(ServiceResult<DigitalSignatureDto>.Failure("Signing failed"));

            // Act
            var result = await _controller.SubmitSignature(leaseAgreementId, signatureData);

            // Assert
            var jsonResult = Assert.IsType<JsonResult>(result);
            dynamic data = jsonResult.Value;
            Assert.False((bool)data.success);
            Assert.Equal("Signing failed", (string)data.message);
        }

        [Fact]
        public async Task DownloadSignedLease_WithValidRequest_ReturnsFile()
        {
            // Arrange
            int leaseAgreementId = 1;
            var tenantDto = new TenantDto { TenantId = 1, FullName = "Test Tenant" };
            var fileBytes = new byte[] { 1, 2, 3, 4, 5 };

            _mockTenantApplicationService.Setup(s => s.GetTenantByUserIdAsync(1))
                .ReturnsAsync(ServiceResult<TenantDto>.Success(tenantDto));
            
            _mockLeaseGenerationService.Setup(s => s.DownloadSignedLeaseAsync(leaseAgreementId, 1))
                .ReturnsAsync(ServiceResult<byte[]>.Success(fileBytes));

            // Act
            var result = await _controller.DownloadSignedLease(leaseAgreementId);

            // Assert
            var fileResult = Assert.IsType<FileContentResult>(result);
            Assert.Equal("application/pdf", fileResult.ContentType);
            Assert.Equal($"signed_lease_{leaseAgreementId}.pdf", fileResult.FileDownloadName);
            Assert.Equal(fileBytes, fileResult.FileContents);
        }

        [Fact]
        public async Task PreviewLease_AsManager_ReturnsHtmlContent()
        {
            // Arrange
            SetupControllerContext("Manager");
            int leaseAgreementId = 1;
            var signingDto = new LeaseAgreementSigningDto 
            { 
                LeaseAgreementId = 1,
                GeneratedHtmlContent = "<html><body>Lease Content</body></html>"
            };

            _mockLeaseGenerationService.Setup(s => s.GetLeaseForSigningAsync(leaseAgreementId, 0))
                .ReturnsAsync(ServiceResult<LeaseAgreementSigningDto>.Success(signingDto));

            // Act
            var result = await _controller.PreviewLease(leaseAgreementId);

            // Assert
            var contentResult = Assert.IsType<ContentResult>(result);
            Assert.Equal("text/html", contentResult.ContentType);
            Assert.Equal("<html><body>Lease Content</body></html>", contentResult.Content);
        }

        [Fact]
        public async Task PreviewLease_AsTenant_WithValidLease_ReturnsHtmlContent()
        {
            // Arrange
            int leaseAgreementId = 1;
            var tenantDto = new TenantDto { TenantId = 1, FullName = "Test Tenant" };
            var signingDto = new LeaseAgreementSigningDto 
            { 
                LeaseAgreementId = 1,
                GeneratedHtmlContent = "<html><body>Lease Content</body></html>"
            };

            _mockTenantApplicationService.Setup(s => s.GetTenantByUserIdAsync(1))
                .ReturnsAsync(ServiceResult<TenantDto>.Success(tenantDto));
            
            _mockLeaseGenerationService.Setup(s => s.GetLeaseForSigningAsync(leaseAgreementId, 1))
                .ReturnsAsync(ServiceResult<LeaseAgreementSigningDto>.Success(signingDto));

            // Act
            var result = await _controller.PreviewLease(leaseAgreementId);

            // Assert
            var contentResult = Assert.IsType<ContentResult>(result);
            Assert.Equal("text/html", contentResult.ContentType);
            Assert.Equal("<html><body>Lease Content</body></html>", contentResult.Content);
        }

        #endregion

        #region Template Management Tests

        [Fact]
        public async Task Templates_AsManager_ReturnsViewWithTemplates()
        {
            // Arrange
            SetupControllerContext("Manager");
            var templateDto = new LeaseTemplateDto { LeaseTemplateId = 1, Name = "Default Template" };
            
            _mockLeaseGenerationService.Setup(s => s.GetLeaseTemplatesAsync())
                .ReturnsAsync(ServiceResult<IEnumerable<LeaseTemplateDto>>.Success(new[] { templateDto }));

            // Act
            var result = await _controller.Templates();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<List<LeaseTemplateViewModel>>(viewResult.Model);
            Assert.Single(model);
            Assert.Equal("Default Template", model.First().Name);
        }

        [Fact]
        public async Task SaveTemplate_CreateNew_ReturnsRedirectWithSuccess()
        {
            // Arrange
            SetupControllerContext("Manager");
            var viewModel = new LeaseTemplateViewModel 
            { 
                LeaseTemplateId = 0, 
                Name = "New Template",
                HtmlContent = "<html>Template</html>",
                IsActive = true
            };
            var templateDto = new LeaseTemplateDto { LeaseTemplateId = 1, Name = "New Template" };

            _mockLeaseGenerationService.Setup(s => s.CreateLeaseTemplateAsync(It.IsAny<CreateLeaseTemplateDto>()))
                .ReturnsAsync(ServiceResult<LeaseTemplateDto>.Success(templateDto));

            // Act
            var result = await _controller.SaveTemplate(viewModel);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Templates", redirectResult.ActionName);
            Assert.Equal("? Template created successfully!", _controller.TempData["Success"]);
        }

        [Fact]
        public async Task SaveTemplate_UpdateExisting_ReturnsRedirectWithSuccess()
        {
            // Arrange
            SetupControllerContext("Manager");
            var viewModel = new LeaseTemplateViewModel 
            { 
                LeaseTemplateId = 1, 
                Name = "Updated Template",
                HtmlContent = "<html>Updated Template</html>",
                IsActive = true
            };
            var templateDto = new LeaseTemplateDto { LeaseTemplateId = 1, Name = "Updated Template" };

            _mockLeaseGenerationService.Setup(s => s.UpdateLeaseTemplateAsync(1, It.IsAny<UpdateLeaseTemplateDto>()))
                .ReturnsAsync(ServiceResult<LeaseTemplateDto>.Success(templateDto));

            // Act
            var result = await _controller.SaveTemplate(viewModel);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Templates", redirectResult.ActionName);
            Assert.Equal("? Template updated successfully!", _controller.TempData["Success"]);
        }

        [Fact]
        public async Task DeleteTemplate_WithValidId_ReturnsRedirectWithSuccess()
        {
            // Arrange
            SetupControllerContext("Manager");
            int templateId = 1;

            _mockLeaseGenerationService.Setup(s => s.DeleteLeaseTemplateAsync(templateId))
                .ReturnsAsync(ServiceResult<bool>.Success(true));

            // Act
            var result = await _controller.DeleteTemplate(templateId);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Templates", redirectResult.ActionName);
            Assert.Equal("? Template deleted successfully!", _controller.TempData["Success"]);
        }

        #endregion

        #region Quick Action Tests

        [Fact]
        public async Task QuickPreview_WithAvailableContent_RedirectsToPreview()
        {
            // Arrange
            var tenantDto = new TenantDto { TenantId = 1 };
            var leaseDto = new LeaseAgreementDto { LeaseAgreementId = 1, TenantId = 1 };
            var leaseViewModel = new LeaseAgreementViewModel 
            { 
                LeaseAgreementId = 1, 
                GeneratedHtmlContent = "<html>Test</html>",
                GeneratedAt = DateTime.Now
            };

            _mockTenantApplicationService.Setup(s => s.GetTenantByUserIdAsync(1))
                .ReturnsAsync(ServiceResult<TenantDto>.Success(tenantDto));
            
            _mockLeaseAgreementService.Setup(s => s.GetLeaseAgreementsByTenantIdAsync(1))
                .ReturnsAsync(ServiceResult<IEnumerable<LeaseAgreementDto>>.Success(new[] { leaseDto }));

            // Override the mapper to return our test ViewModel
            var mapperMock = new Mock<IMapper>();
            mapperMock.Setup(m => m.Map<List<LeaseAgreementViewModel>>(It.IsAny<IEnumerable<LeaseAgreementDto>>()))
                .Returns(new List<LeaseAgreementViewModel> { leaseViewModel });

            var controller = new DigitalLeaseController(
                _mockLeaseGenerationService.Object,
                _mockLeaseAgreementService.Object,
                _mockTenantApplicationService.Object,
                mapperMock.Object);

            SetupControllerContextForController(controller);

            // Act
            var result = await controller.QuickPreview();

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("PreviewLease", redirectResult.ActionName);
            Assert.Equal(1, redirectResult.RouteValues["leaseAgreementId"]);
        }

        [Fact]
        public async Task QuickSign_WithAvailableLease_RedirectsToSign()
        {
            // Arrange
            var tenantDto = new TenantDto { TenantId = 1 };
            var leaseDto = new LeaseAgreementDto { LeaseAgreementId = 1, TenantId = 1 };
            var leaseViewModel = new LeaseAgreementViewModel 
            { 
                LeaseAgreementId = 1, 
                Status = LeaseAgreement.LeaseStatus.Sent,
                IsDigitallySigned = false,
                SentToTenantAt = DateTime.Now
            };

            _mockTenantApplicationService.Setup(s => s.GetTenantByUserIdAsync(1))
                .ReturnsAsync(ServiceResult<TenantDto>.Success(tenantDto));
            
            _mockLeaseAgreementService.Setup(s => s.GetLeaseAgreementsByTenantIdAsync(1))
                .ReturnsAsync(ServiceResult<IEnumerable<LeaseAgreementDto>>.Success(new[] { leaseDto }));

            var mapperMock = new Mock<IMapper>();
            mapperMock.Setup(m => m.Map<List<LeaseAgreementViewModel>>(It.IsAny<IEnumerable<LeaseAgreementDto>>()))
                .Returns(new List<LeaseAgreementViewModel> { leaseViewModel });

            var controller = new DigitalLeaseController(
                _mockLeaseGenerationService.Object,
                _mockLeaseAgreementService.Object,
                _mockTenantApplicationService.Object,
                mapperMock.Object);

            SetupControllerContextForController(controller);

            // Act
            var result = await controller.QuickSign();

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("SignLease", redirectResult.ActionName);
            Assert.Equal(1, redirectResult.RouteValues["leaseAgreementId"]);
        }

        [Fact]
        public async Task LeaseStatusInfo_WithValidLease_RedirectsWithStatusMessages()
        {
            // Arrange
            int leaseAgreementId = 1;
            var tenantDto = new TenantDto { TenantId = 1 };
            var leaseDto = new LeaseAgreementDto { LeaseAgreementId = 1, TenantId = 1 };
            var leaseViewModel = new LeaseAgreementViewModel 
            { 
                LeaseAgreementId = 1, 
                Status = LeaseAgreement.LeaseStatus.Generated,
                GeneratedHtmlContent = "<html>Test</html>",
                GeneratedAt = DateTime.Now
            };

            _mockTenantApplicationService.Setup(s => s.GetTenantByUserIdAsync(1))
                .ReturnsAsync(ServiceResult<TenantDto>.Success(tenantDto));
            
            _mockLeaseAgreementService.Setup(s => s.GetLeaseAgreementsByTenantIdAsync(1))
                .ReturnsAsync(ServiceResult<IEnumerable<LeaseAgreementDto>>.Success(new[] { leaseDto }));

            var mapperMock = new Mock<IMapper>();
            mapperMock.Setup(m => m.Map<List<LeaseAgreementViewModel>>(It.IsAny<IEnumerable<LeaseAgreementDto>>()))
                .Returns(new List<LeaseAgreementViewModel> { leaseViewModel });

            var controller = new DigitalLeaseController(
                _mockLeaseGenerationService.Object,
                _mockLeaseAgreementService.Object,
                _mockTenantApplicationService.Object,
                mapperMock.Object);

            SetupControllerContextForController(controller);

            // Act
            var result = await controller.LeaseStatusInfo(leaseAgreementId);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("MyLeases", redirectResult.ActionName);
            
            // Check that status messages were set
            Assert.Contains("Status: Generated", controller.TempData["Info"]?.ToString());
            Assert.Contains("Digital document has been generated", controller.TempData["Success"]?.ToString());
        }

        #endregion

        #region Helper Methods

        private void SetupControllerContextForController(DigitalLeaseController controller, string role = "Tenant", int userId = 1)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Role, role)
            };

            var identity = new ClaimsIdentity(claims, "test");
            var principal = new ClaimsPrincipal(identity);

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            };

            controller.TempData = new TempDataDictionary(
                controller.ControllerContext.HttpContext,
                Mock.Of<ITempDataProvider>());
        }

        #endregion
    }
}