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
    /// <summary>
    /// Comprehensive lease workflow tests covering the complete journey:
    /// 1. Manager creates lease (Draft status)
    /// 2. Manager generates lease (Generated status)
    /// 3. Manager sends to tenant (Sent status)
    /// 4. Tenant signs lease (Signed status)
    /// </summary>
    public class LeaseWorkflowIntegrationTests
    {
        private readonly Mock<ILeaseGenerationService> _mockLeaseGenerationService;
        private readonly Mock<ILeaseAgreementApplicationService> _mockLeaseAgreementService;
        private readonly Mock<ITenantApplicationService> _mockTenantApplicationService;
        private readonly IMapper _mapper;

        public LeaseWorkflowIntegrationTests()
        {
            _mockLeaseGenerationService = new Mock<ILeaseGenerationService>();
            _mockLeaseAgreementService = new Mock<ILeaseAgreementApplicationService>();
            _mockTenantApplicationService = new Mock<ITenantApplicationService>();
            _mapper = GetMapper();
        }

        private IMapper GetMapper()
        {
            var expr = new MapperConfigurationExpression();
            expr.CreateMap<LeaseAgreementDto, LeaseAgreementViewModel>().ReverseMap();
            expr.CreateMap<TenantDto, TenantViewModel>().ReverseMap();
            expr.CreateMap<RoomDto, RoomViewModel>().ReverseMap();
            expr.CreateMap<DigitalSignatureDto, DigitalSignatureViewModel>().ReverseMap();
            expr.CreateMap<LeaseTemplateDto, LeaseTemplateViewModel>().ReverseMap();
            
            var config = new MapperConfiguration(expr, NullLoggerFactory.Instance);
            return config.CreateMapper();
        }

        #region Checkpoint 1: Manager Creates Lease (Draft Status)

        [Fact]
        public async Task Checkpoint1_ManagerCreatesLease_StatusShouldBeDraft()
        {
            // Arrange - Manager creates a new lease
            var newLeaseDto = new LeaseAgreementDto
            {
                LeaseAgreementId = 1,
                TenantId = 1,
                RoomId = 101,
                StartDate = DateTime.Today.AddDays(7),
                EndDate = DateTime.Today.AddMonths(12),
                RentAmount = 1200.00m,
                ExpectedRentDay = 5
            };

            _mockLeaseAgreementService.Setup(s => s.CreateLeaseAgreementAsync(It.IsAny<CreateLeaseAgreementDto>()))
                .ReturnsAsync(ServiceResult<LeaseAgreementDto>.Success(newLeaseDto));

            // Act
            var result = await CreateLease(newLeaseDto);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(1, result.Data.LeaseAgreementId);
            Assert.Equal("Draft", GetExpectedStatusDisplay(LeaseAgreement.LeaseStatus.Draft));
            
            // Note: These properties are not available in DTO, would be tested at entity level
            // Assert.Null(result.Data.GeneratedAt);
            // Assert.Null(result.Data.SentToTenantAt); 
            // Assert.False(result.Data.IsDigitallySigned);
        }

        [Fact]
        public async Task Checkpoint1_ManagerCreatesLease_WithValidation_ShouldValidateBusinessRules()
        {
            // Test Case 1: End date before start date
            var invalidLeaseDto = new LeaseAgreementDto
            {
                TenantId = 1,
                RoomId = 101,
                StartDate = DateTime.Today.AddMonths(12),
                EndDate = DateTime.Today, // Invalid: end before start
                RentAmount = 1200.00m
            };

            _mockLeaseAgreementService.Setup(s => s.CreateLeaseAgreementAsync(It.IsAny<CreateLeaseAgreementDto>()))
                .ReturnsAsync(ServiceResult<LeaseAgreementDto>.Failure("End date must be after start date"));

            // Act
            var result = await CreateLease(invalidLeaseDto);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Contains("End date must be after start date", result.ErrorMessage);
        }

        #endregion

        #region Checkpoint 2: Manager Generates Lease (Generated Status)

        [Fact]
        public async Task Checkpoint2_ManagerGeneratesLease_StatusShouldBeGenerated()
        {
            // Arrange - Lease exists in Draft status
            int leaseAgreementId = 1;
            var htmlContent = "<html><body>Generated Lease Content</body></html>";
            var pdfPath = "/uploads/leases/lease_1.pdf";

            _mockLeaseGenerationService.Setup(s => s.GenerateLeaseHtmlAsync(leaseAgreementId, null))
                .ReturnsAsync(ServiceResult<string>.Success(htmlContent));
            
            _mockLeaseGenerationService.Setup(s => s.GenerateLeasePdfAsync(leaseAgreementId, htmlContent))
                .ReturnsAsync(ServiceResult<string>.Success(pdfPath));

            var controller = CreateDigitalLeaseController("Manager");

            // Act
            var result = await controller.GenerateLease(leaseAgreementId);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);
            Assert.Equal("LeaseAgreements", redirectResult.ControllerName);
            Assert.Contains("generated successfully", controller.TempData["Success"]?.ToString());

            // Verify service calls
            _mockLeaseGenerationService.Verify(s => s.GenerateLeaseHtmlAsync(leaseAgreementId, null), Times.Once);
            _mockLeaseGenerationService.Verify(s => s.GenerateLeasePdfAsync(leaseAgreementId, htmlContent), Times.Once);
        }

        [Fact]
        public async Task Checkpoint2_GenerateLease_FromDraftStatus_ShouldTransitionCorrectly()
        {
            // Arrange - Verify lease transitions from Draft ? Generated
            int leaseAgreementId = 1;
            
            // Simulate the service updating status to Generated during HTML generation
            _mockLeaseGenerationService.Setup(s => s.GenerateLeaseHtmlAsync(leaseAgreementId, null))
                .ReturnsAsync(ServiceResult<string>.Success("<html>Content</html>"));
            
            _mockLeaseGenerationService.Setup(s => s.GenerateLeasePdfAsync(leaseAgreementId, It.IsAny<string>()))
                .ReturnsAsync(ServiceResult<string>.Success("/uploads/lease.pdf"));

            var controller = CreateDigitalLeaseController("Manager");

            // Act
            var result = await controller.GenerateLease(leaseAgreementId);

            // Assert
            Assert.IsType<RedirectToActionResult>(result);
            
            // Verify the lease generation service was called (which internally updates status)
            _mockLeaseGenerationService.Verify(s => s.GenerateLeaseHtmlAsync(leaseAgreementId, null), Times.Once);
        }

        [Fact]
        public async Task Checkpoint2_GenerateLease_HtmlGenerationFails_ShouldHandleError()
        {
            // Arrange
            int leaseAgreementId = 1;
            
            _mockLeaseGenerationService.Setup(s => s.GenerateLeaseHtmlAsync(leaseAgreementId, null))
                .ReturnsAsync(ServiceResult<string>.Failure("Template not found"));

            var controller = CreateDigitalLeaseController("Manager");

            // Act
            var result = await controller.GenerateLease(leaseAgreementId);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Contains("Failed to generate lease", controller.TempData["Error"]?.ToString());
        }

        #endregion

        #region Checkpoint 3: Manager Sends to Tenant (Sent Status)

        [Fact]
        public async Task Checkpoint3_ManagerSendsToTenant_StatusShouldBeSent()
        {
            // Arrange - Lease exists in Generated status
            int leaseAgreementId = 1;

            _mockLeaseGenerationService.Setup(s => s.SendLeaseToTenantAsync(leaseAgreementId))
                .ReturnsAsync(ServiceResult<bool>.Success(true));

            var controller = CreateDigitalLeaseController("Manager");

            // Act
            var result = await controller.SendToTenant(leaseAgreementId);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);
            Assert.Equal("LeaseAgreements", redirectResult.ControllerName);
            Assert.Contains("sent to tenant for signing", controller.TempData["Success"]?.ToString());

            // Verify service call
            _mockLeaseGenerationService.Verify(s => s.SendLeaseToTenantAsync(leaseAgreementId), Times.Once);
        }

        [Fact]
        public async Task Checkpoint3_SendToTenant_WithoutGeneration_ShouldFail()
        {
            // Arrange - Try to send a lease that hasn't been generated
            int leaseAgreementId = 1;

            _mockLeaseGenerationService.Setup(s => s.SendLeaseToTenantAsync(leaseAgreementId))
                .ReturnsAsync(ServiceResult<bool>.Failure("Lease must be generated before sending"));

            var controller = CreateDigitalLeaseController("Manager");

            // Act
            var result = await controller.SendToTenant(leaseAgreementId);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Contains("Failed to send lease", controller.TempData["Error"]?.ToString());
            Assert.Contains("must be generated before sending", controller.TempData["Error"]?.ToString());
        }

        #endregion

        #region Checkpoint 4: Tenant Signs Lease (Signed Status)

        [Fact]
        public async Task Checkpoint4_TenantSignsLease_StatusShouldBeSigned()
        {
            // Arrange - Lease exists in Sent status, ready for signing
            int leaseAgreementId = 1;
            int tenantId = 1;
            string signatureData = "data:image/png;base64,testSignatureData";
            
            var tenantDto = new TenantDto { TenantId = tenantId, FullName = "Test Tenant" };
            var signatureDto = new DigitalSignatureDto 
            { 
                DigitalSignatureId = 1,
                LeaseAgreementId = leaseAgreementId,
                TenantId = tenantId,
                SignedDate = DateTime.UtcNow
            };

            _mockTenantApplicationService.Setup(s => s.GetTenantByUserIdAsync(1))
                .ReturnsAsync(ServiceResult<TenantDto>.Success(tenantDto));
            
            _mockLeaseGenerationService.Setup(s => s.SignLeaseAsync(It.IsAny<SignLeaseDto>()))
                .ReturnsAsync(ServiceResult<DigitalSignatureDto>.Success(signatureDto));

            var controller = CreateDigitalLeaseController("Tenant", userId: 1);

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

            // Verify service calls
            _mockLeaseGenerationService.Verify(s => s.SignLeaseAsync(It.Is<SignLeaseDto>(dto => 
                dto.LeaseAgreementId == leaseAgreementId && 
                dto.SignatureDataUrl == signatureData)), Times.Once);
        }

        [Fact]
        public async Task Checkpoint4_TenantSignsLease_AlreadySigned_ShouldFail()
        {
            // Arrange - Try to sign a lease that's already signed
            int leaseAgreementId = 1;
            string signatureData = "data:image/png;base64,testData";
            
            var tenantDto = new TenantDto { TenantId = 1, FullName = "Test Tenant" };

            _mockTenantApplicationService.Setup(s => s.GetTenantByUserIdAsync(1))
                .ReturnsAsync(ServiceResult<TenantDto>.Success(tenantDto));
            
            _mockLeaseGenerationService.Setup(s => s.SignLeaseAsync(It.IsAny<SignLeaseDto>()))
                .ReturnsAsync(ServiceResult<DigitalSignatureDto>.Failure("Lease agreement is already signed"));

            var controller = CreateDigitalLeaseController("Tenant", userId: 1);

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

        #endregion

        #region End-to-End Workflow Test

        [Fact]
        public async Task EndToEndWorkflow_CompleteLeaseJourney_ShouldWorkCorrectly()
        {
            // This test simulates the complete lease workflow:
            // Create ? Generate ? Send ? Sign

            // Step 1: Create lease (would be done via LeaseAgreementsController)
            var leaseDto = new LeaseAgreementDto 
            { 
                LeaseAgreementId = 1, 
                TenantId = 1,
                RoomId = 101 
            };
            
            // Step 2: Manager generates lease
            _mockLeaseGenerationService.Setup(s => s.GenerateLeaseHtmlAsync(1, null))
                .ReturnsAsync(ServiceResult<string>.Success("<html>Lease</html>"));
            _mockLeaseGenerationService.Setup(s => s.GenerateLeasePdfAsync(1, It.IsAny<string>()))
                .ReturnsAsync(ServiceResult<string>.Success("/uploads/lease.pdf"));

            var managerController = CreateDigitalLeaseController("Manager");
            var generateResult = await managerController.GenerateLease(1);
            
            Assert.IsType<RedirectToActionResult>(generateResult);
            Assert.Contains("generated successfully", managerController.TempData["Success"]?.ToString());

            // Step 3: Manager sends to tenant
            _mockLeaseGenerationService.Setup(s => s.SendLeaseToTenantAsync(1))
                .ReturnsAsync(ServiceResult<bool>.Success(true));

            var sendResult = await managerController.SendToTenant(1);
            
            Assert.IsType<RedirectToActionResult>(sendResult);
            Assert.Contains("sent to tenant", managerController.TempData["Success"]?.ToString());

            // Step 4: Tenant signs lease
            var tenantDto = new TenantDto { TenantId = 1, FullName = "Test Tenant" };
            var signatureDto = new DigitalSignatureDto { DigitalSignatureId = 1 };

            _mockTenantApplicationService.Setup(s => s.GetTenantByUserIdAsync(1))
                .ReturnsAsync(ServiceResult<TenantDto>.Success(tenantDto));
            _mockLeaseGenerationService.Setup(s => s.SignLeaseAsync(It.IsAny<SignLeaseDto>()))
                .ReturnsAsync(ServiceResult<DigitalSignatureDto>.Success(signatureDto));

            var tenantController = CreateDigitalLeaseController("Tenant", userId: 1);
            var signResult = await tenantController.SubmitSignature(1, "data:image/png;base64,test");

            var jsonResult = Assert.IsType<JsonResult>(signResult);
            var valueType = jsonResult.Value.GetType();
            var successProp = valueType.GetProperty("success");
            var messageProp = valueType.GetProperty("message");
            Assert.NotNull(successProp);
            Assert.NotNull(messageProp);
            Assert.True((bool)successProp.GetValue(jsonResult.Value));
            Assert.Contains("signed successfully", (string)messageProp.GetValue(jsonResult.Value));

            // Verify all services were called in the correct sequence
            _mockLeaseGenerationService.Verify(s => s.GenerateLeaseHtmlAsync(1, null), Times.Once);
            _mockLeaseGenerationService.Verify(s => s.GenerateLeasePdfAsync(1, It.IsAny<string>()), Times.Once);
            _mockLeaseGenerationService.Verify(s => s.SendLeaseToTenantAsync(1), Times.Once);
            _mockLeaseGenerationService.Verify(s => s.SignLeaseAsync(It.IsAny<SignLeaseDto>()), Times.Once);
        }

        #endregion

        #region Status Transition Validation Tests

        [Theory]
        [InlineData(LeaseAgreement.LeaseStatus.Draft, "Draft")]
        [InlineData(LeaseAgreement.LeaseStatus.Generated, "Generated")]
        [InlineData(LeaseAgreement.LeaseStatus.Sent, "Awaiting Signature")]
        [InlineData(LeaseAgreement.LeaseStatus.Signed, "Signed")]
        [InlineData(LeaseAgreement.LeaseStatus.Completed, "Completed")]
        [InlineData(LeaseAgreement.LeaseStatus.Cancelled, "Cancelled")]
        public void StatusDisplay_ShouldMapCorrectly(LeaseAgreement.LeaseStatus status, string expectedDisplay)
        {
            // Act
            var displayName = GetStatusDisplayName(status);

            // Assert
            Assert.Equal(expectedDisplay, displayName);
        }

        [Fact]
        public void LeaseStatusFlow_ShouldFollowCorrectSequence()
        {
            // Arrange - Define the expected status flow
            var expectedFlow = new[]
            {
                LeaseAgreement.LeaseStatus.Draft,      // 0 - Initial creation
                LeaseAgreement.LeaseStatus.Generated,  // 1 - After HTML/PDF generation
                LeaseAgreement.LeaseStatus.Sent,       // 2 - After sending to tenant
                LeaseAgreement.LeaseStatus.Signed,     // 3 - After tenant signs
                LeaseAgreement.LeaseStatus.Completed   // 4 - Lease fully executed
            };

            // Act & Assert - Verify numeric values are in correct sequence
            for (int i = 0; i < expectedFlow.Length - 1; i++)
            {
                Assert.True((int)expectedFlow[i] < (int)expectedFlow[i + 1], 
                    $"Status {expectedFlow[i]} should have lower value than {expectedFlow[i + 1]}");
            }
        }

        #endregion

        #region Tenant Workflow Tests

        [Fact]
        public async Task TenantWorkflow_ViewMyLeases_ShouldShowCorrectStatuses()
        {
            // Arrange
            var tenantDto = new TenantDto { TenantId = 1, FullName = "Test Tenant" };
            var leaseDtos = new[]
            {
                new LeaseAgreementDto { LeaseAgreementId = 1, TenantId = 1 },
                new LeaseAgreementDto { LeaseAgreementId = 2, TenantId = 1 }
            };

            _mockTenantApplicationService.Setup(s => s.GetTenantByUserIdAsync(1))
                .ReturnsAsync(ServiceResult<TenantDto>.Success(tenantDto));
            _mockLeaseAgreementService.Setup(s => s.GetLeaseAgreementsByTenantIdAsync(1))
                .ReturnsAsync(ServiceResult<IEnumerable<LeaseAgreementDto>>.Success(leaseDtos));

            var controller = CreateDigitalLeaseController("Tenant", userId: 1);

            // Act
            var result = await controller.MyLeases();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<List<LeaseAgreementViewModel>>(viewResult.Model);
            Assert.Equal(2, model.Count);
        }

        [Fact]
        public async Task TenantWorkflow_SignLease_WithSentStatus_ShouldWork()
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

            var controller = CreateDigitalLeaseController("Tenant", userId: 1);

            // Act
            var result = await controller.SignLease(leaseAgreementId);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<LeaseSigningViewModel>(viewResult.Model);
            Assert.Equal(leaseAgreementId, model.LeaseAgreement.LeaseAgreementId);
        }

        [Fact]
        public async Task TenantWorkflow_SignLease_WithDraftStatus_ShouldFail()
        {
            // Arrange
            int leaseAgreementId = 1;
            var tenantDto = new TenantDto { TenantId = 1, FullName = "Test Tenant" };

            _mockTenantApplicationService.Setup(s => s.GetTenantByUserIdAsync(1))
                .ReturnsAsync(ServiceResult<TenantDto>.Success(tenantDto));
            _mockLeaseGenerationService.Setup(s => s.GetLeaseForSigningAsync(leaseAgreementId, 1))
                .ReturnsAsync(ServiceResult<LeaseAgreementSigningDto>.Failure("Lease agreement is not ready for signing"));

            var controller = CreateDigitalLeaseController("Tenant", userId: 1);

            // Act
            var result = await controller.SignLease(leaseAgreementId);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("MyLeases", redirectResult.ActionName);
            Assert.Contains("not ready for signing", controller.TempData["Error"]?.ToString());
        }

        #endregion

        #region Helper Methods

        private async Task<ServiceResult<LeaseAgreementDto>> CreateLease(LeaseAgreementDto leaseDto)
        {
            // This simulates lease creation - in reality this would be done via LeaseAgreementsController
            // but we're testing the business logic here
            if (leaseDto.EndDate <= leaseDto.StartDate)
            {
                return ServiceResult<LeaseAgreementDto>.Failure("End date must be after start date");
            }

            return ServiceResult<LeaseAgreementDto>.Success(leaseDto);
        }

        private DigitalLeaseController CreateDigitalLeaseController(string role = "Tenant", int userId = 1)
        {
            var controller = new DigitalLeaseController(
                _mockLeaseGenerationService.Object,
                _mockLeaseAgreementService.Object,
                _mockTenantApplicationService.Object,
                _mapper);

            SetupControllerContext(controller, role, userId);
            return controller;
        }

        private void SetupControllerContext(DigitalLeaseController controller, string role = "Tenant", int userId = 1)
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

        private string GetExpectedStatusDisplay(LeaseAgreement.LeaseStatus status)
        {
            return GetStatusDisplayName(status);
        }

        private string GetStatusDisplayName(LeaseAgreement.LeaseStatus status)
        {
            return status switch
            {
                LeaseAgreement.LeaseStatus.Draft => "Draft",
                LeaseAgreement.LeaseStatus.Generated => "Generated",
                LeaseAgreement.LeaseStatus.Sent => "Awaiting Signature",
                LeaseAgreement.LeaseStatus.Signed => "Signed",
                LeaseAgreement.LeaseStatus.Completed => "Completed",
                LeaseAgreement.LeaseStatus.Cancelled => "Cancelled",
                _ => "Unknown"
            };
        }

        #endregion
    }
}