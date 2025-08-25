using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;
using PropertyManagement.Application.Common;
using PropertyManagement.Application.DTOs;
using PropertyManagement.Application.Services;
using PropertyManagement.Test.Infrastructure;
using PropertyManagement.Web.Controllers;
using PropertyManagement.Web.ViewModels;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace PropertyManagement.Test.Controllers
{
    public class AdditionalControllerTests : BaseControllerTest
    {
        [Fact]
        public async Task TenantsController_CreateOrEdit_UpdateExisting_UpdatesSuccessfully()
        {
            // Arrange
            var mockTenantService = new Mock<ITenantApplicationService>();

            var existingTenant = new TenantDto
            {
                TenantId = 1,
                UserId = 2,
                RoomId = 2,
                FullName = "John Doe",
                Contact = "1234567890",
                EmergencyContactName = "Jane Doe",
                EmergencyContactNumber = "0987654321"
            };

            var updatedTenant = new TenantDto
            {
                TenantId = 1,
                UserId = 2,
                RoomId = 2,
                FullName = "Updated Name",
                Contact = "9999999999",
                EmergencyContactName = "Updated Emergency",
                EmergencyContactNumber = "8888888888"
            };

            mockTenantService.Setup(s => s.UpdateTenantAsync(It.IsAny<int>(), It.IsAny<UpdateTenantDto>()))
                .ReturnsAsync(ServiceResult<TenantDto>.Success(updatedTenant));

            var mockRoomService = new Mock<IRoomApplicationService>();
            // Setup required room service methods to avoid NullReferenceException
            mockRoomService.Setup(s => s.GetAvailableRoomsAsync())
                .ReturnsAsync(ServiceResult<IEnumerable<RoomDto>>.Success(new List<RoomDto>()));
            mockRoomService.Setup(s => s.GetRoomByIdAsync(It.IsAny<int>()))
                .ReturnsAsync(ServiceResult<RoomDto>.Success(new RoomDto
                {
                    RoomId = existingTenant.RoomId,
                    Number = "101",
                    Type = "Single",
                    Status = "Available"
                }));

            var mockMaintenanceService = new Mock<IMaintenanceRequestApplicationService>();
            var controller = new TenantsController(
                mockTenantService.Object, 
                mockRoomService.Object, 
                mockMaintenanceService.Object,
                Mapper);
            SetupControllerContext(controller, GetManagerUser());
            // Ensure TempData is set up for the controller
            controller.TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>());

            var updatedTenantVm = new TenantViewModel
            {
                TenantId = 1,
                UserId = 2,
                RoomId = 2,
                FullName = "Updated Name",
                Contact = "9999999999",
                EmergencyContactName = "Updated Emergency",
                EmergencyContactNumber = "8888888888"
            };

            // Act
            var result = await controller.CreateOrEdit(updatedTenantVm, "uniqueuser", "validpassword123");

            // Assert
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirect.ActionName);
            mockTenantService.Verify(s => s.UpdateTenantAsync(1, It.IsAny<UpdateTenantDto>()), Times.Once);
        }

        [Fact]
        public async Task RoomsController_CreateOrEdit_ValidModel_CreatesRoomSuccessfully()
        {
            // Arrange
            var mockRoomService = new Mock<IRoomApplicationService>();
            var mockBookingService = new Mock<IBookingRequestApplicationService>();

            var createdRoom = new RoomDto
            {
                RoomId = 1,
                Number = "201",
                Type = "Single",
                Status = "Available"
            };

            mockRoomService.Setup(s => s.CreateRoomAsync(It.IsAny<CreateRoomDto>()))
                .ReturnsAsync(ServiceResult<RoomDto>.Success(createdRoom));

            var mockTenantService = new Mock<ITenantApplicationService>();
            var mockMaintenanceService = new Mock<IMaintenanceRequestApplicationService>();
            var controller = new RoomsController(
                mockRoomService.Object, 
                mockBookingService.Object, 
                mockTenantService.Object,
                mockMaintenanceService.Object,
                Mapper);
            SetupControllerContext(controller, GetManagerUser());

            var roomModel = new RoomFormViewModel
            {
                Number = "201",
                Type = "Single",
                Status = "Available"
            };

            // Act
            var result = await controller.CreateOrEdit(roomModel);

            // Assert
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirect.ActionName);
            mockRoomService.Verify(s => s.CreateRoomAsync(It.IsAny<CreateRoomDto>()), Times.Once);
        }

        [Fact]
        public async Task PaymentsController_Delete_NonExistingPayment_ReturnsNotFound()
        {
            // Arrange
            var mockPaymentService = new Mock<IPaymentApplicationService>();
            var mockTenantService = new Mock<ITenantApplicationService>();

            mockPaymentService.Setup(s => s.DeletePaymentAsync(999))
                .ReturnsAsync(ServiceResult<bool>.Failure("Payment not found"));

            var mockLeaseService = new Mock<ILeaseAgreementApplicationService>();
            var controller = new PaymentsController(mockPaymentService.Object, mockTenantService.Object, mockLeaseService.Object, Mapper);
            SetupControllerContext(controller, GetManagerUser());

            // Act
            var result = await controller.Delete(999);

            // Assert
            Assert.IsType<NotFoundResult>(result);
            mockPaymentService.Verify(s => s.DeletePaymentAsync(999), Times.Once);
        }

        [Fact]
        public async Task PaymentsController_Create_ValidModel_CreatesPaymentSuccessfully()
        {
            // Arrange
            var mockPaymentService = new Mock<IPaymentApplicationService>();
            var mockTenantService = new Mock<ITenantApplicationService>();

            var createdPayment = new PaymentDto
            {
                PaymentId = 1,
                TenantId = 1,
                Amount = 1200,
                Type = "Rent",
                PaymentMonth = 2,
                PaymentYear = 2024,
                PaymentDate = DateTime.Now
            };

            mockPaymentService.Setup(s => s.CreatePaymentAsync(It.IsAny<CreatePaymentDto>()))
                .ReturnsAsync(ServiceResult<PaymentDto>.Success(createdPayment));

            var mockLeaseService = new Mock<ILeaseAgreementApplicationService>();
            var controller = new PaymentsController(mockPaymentService.Object, mockTenantService.Object, mockLeaseService.Object, Mapper);
            SetupControllerContext(controller, GetManagerUser());

            var paymentModel = new PaymentViewModel
            {
                TenantId = 1,
                Amount = 1200,
                Type = "Rent",
                PaymentMonth = 2,
                PaymentYear = 2024,
                Date = DateTime.Now
            };

            // Act
            var result = await controller.Create(paymentModel);

            // Assert
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirect.ActionName);
            mockPaymentService.Verify(s => s.CreatePaymentAsync(It.IsAny<CreatePaymentDto>()), Times.Once);
        }

        [Fact]
        public async Task PaymentsController_Edit_NonExistingPayment_ReturnsIndexView()
        {
            // Arrange
            var mockPaymentService = new Mock<IPaymentApplicationService>();
            var mockTenantService = new Mock<ITenantApplicationService>();

            mockPaymentService.Setup(s => s.UpdatePaymentAsync(999, It.IsAny<UpdatePaymentDto>()))
                .ReturnsAsync(ServiceResult<PaymentDto>.Failure("Payment not found"));
            mockPaymentService.Setup(s => s.GetAllPaymentsAsync())
                .ReturnsAsync(ServiceResult<IEnumerable<PaymentDto>>.Success(new List<PaymentDto>()));
            mockTenantService.Setup(s => s.GetAllTenantsAsync())
                .ReturnsAsync(ServiceResult<IEnumerable<TenantDto>>.Success(new List<TenantDto>()));

            var mockLeaseService = new Mock<ILeaseAgreementApplicationService>();
            var controller = new PaymentsController(mockPaymentService.Object, mockTenantService.Object, mockLeaseService.Object, Mapper);
            SetupControllerContext(controller, GetManagerUser());

            var paymentViewModel = new PaymentViewModel
            {
                PaymentId = 999,
                TenantId = 1,
                Amount = 1000,
                Type = "Rent",
                PaymentMonth = 1,
                PaymentYear = 2024,
                Date = DateTime.Now
            };

            // Act
            var result = await controller.Edit(paymentViewModel);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal("Index", viewResult.ViewName);
            mockPaymentService.Verify(s => s.UpdatePaymentAsync(999, It.IsAny<UpdatePaymentDto>()), Times.Once);
        }
    }
}