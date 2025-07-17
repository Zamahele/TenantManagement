using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using PropertyManagement.Application.Common;
using PropertyManagement.Application.DTOs;
using PropertyManagement.Application.Services;
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
    public class AdditionalControllerTests
    {
        private IMapper GetMapper()
        {
            var expr = new MapperConfigurationExpression();
            expr.CreateMap<TenantDto, TenantViewModel>().ReverseMap();
            expr.CreateMap<TenantViewModel, CreateTenantDto>();
            expr.CreateMap<TenantViewModel, UpdateTenantDto>();

            // Add the missing UserDto to UserViewModel mapping
            expr.CreateMap<UserDto, UserViewModel>()
                .ForMember(dest => dest.PasswordHash, opt => opt.Ignore()) // PasswordHash is not in UserDto
                .ReverseMap()
                .ForMember(dest => dest.Username, opt => opt.MapFrom(src => src.Username))
                .ForMember(dest => dest.UserId, opt => opt.MapFrom(src => src.UserId))
                .ForMember(dest => dest.Role, opt => opt.MapFrom(src => src.Role));

            expr.CreateMap<PaymentDto, PaymentViewModel>().ReverseMap();
            expr.CreateMap<PaymentViewModel, CreatePaymentDto>();
            expr.CreateMap<PaymentViewModel, UpdatePaymentDto>();
            expr.CreateMap<RoomDto, RoomViewModel>().ReverseMap();
            expr.CreateMap<RoomFormViewModel, CreateRoomDto>();
            expr.CreateMap<RoomFormViewModel, UpdateRoomDto>();
            var config = new MapperConfiguration(expr, NullLoggerFactory.Instance);
            return config.CreateMapper();
        }

        private ClaimsPrincipal GetUser(string role, int userId = 1, string username = "testuser")
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Name, username),
                new Claim(ClaimTypes.Role, role)
            };
            var identity = new ClaimsIdentity(claims, "TestAuthentication");
            return new ClaimsPrincipal(identity);
        }

        private void SetupControllerContext(Controller controller, ClaimsPrincipal user)
        {
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };
            controller.TempData = new TempDataDictionary(controller.ControllerContext.HttpContext, Mock.Of<ITempDataProvider>());
        }

        [Fact]
        public async Task TenantsController_CreateOrEdit_UpdateExisting_UpdatesSuccessfully()
        {
            // Arrange
            var mapper = GetMapper();
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

            var controller = new TenantsController(mockTenantService.Object, mapper);
            SetupControllerContext(controller, GetUser("Manager"));

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
            var mapper = GetMapper();
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

            var controller = new RoomsController(mockRoomService.Object, mockBookingService.Object, mapper);
            SetupControllerContext(controller, GetUser("Manager"));

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
            var mapper = GetMapper();
            var mockPaymentService = new Mock<IPaymentApplicationService>();
            var mockTenantService = new Mock<ITenantApplicationService>();

            mockPaymentService.Setup(s => s.DeletePaymentAsync(999))
                .ReturnsAsync(ServiceResult<bool>.Failure("Payment not found"));

            var controller = new PaymentsController(mockPaymentService.Object, mockTenantService.Object, mapper);
            SetupControllerContext(controller, GetUser("Manager"));

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
            var mapper = GetMapper();
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

            var controller = new PaymentsController(mockPaymentService.Object, mockTenantService.Object, mapper);
            SetupControllerContext(controller, GetUser("Manager"));

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
            var mapper = GetMapper();
            var mockPaymentService = new Mock<IPaymentApplicationService>();
            var mockTenantService = new Mock<ITenantApplicationService>();

            mockPaymentService.Setup(s => s.UpdatePaymentAsync(999, It.IsAny<UpdatePaymentDto>()))
                .ReturnsAsync(ServiceResult<PaymentDto>.Failure("Payment not found"));
            mockPaymentService.Setup(s => s.GetAllPaymentsAsync())
                .ReturnsAsync(ServiceResult<IEnumerable<PaymentDto>>.Success(new List<PaymentDto>()));
            mockTenantService.Setup(s => s.GetAllTenantsAsync())
                .ReturnsAsync(ServiceResult<IEnumerable<TenantDto>>.Success(new List<TenantDto>()));

            var controller = new PaymentsController(mockPaymentService.Object, mockTenantService.Object, mapper);
            SetupControllerContext(controller, GetUser("Manager"));

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