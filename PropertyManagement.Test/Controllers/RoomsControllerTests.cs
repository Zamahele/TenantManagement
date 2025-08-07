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
using System.Threading.Tasks;
using Xunit;
using Assert = Xunit.Assert;

namespace PropertyManagement.Test.Controllers;

public class RoomsControllerTests
{
    private IMapper GetMapper()
    {
        var expr = new MapperConfigurationExpression();
        expr.CreateMap<RoomDto, RoomViewModel>().ReverseMap();
        expr.CreateMap<RoomWithTenantsDto, RoomViewModel>().ReverseMap();
        expr.CreateMap<RoomFormViewModel, CreateRoomDto>();
        expr.CreateMap<RoomFormViewModel, UpdateRoomDto>();
        expr.CreateMap<BookingRequestDto, BookingRequestViewModel>().ReverseMap();
        expr.CreateMap<BookingRequestViewModel, CreateBookingRequestDto>();
        expr.CreateMap<BookingRequestViewModel, UpdateBookingRequestDto>();
        var config = new MapperConfiguration(expr, NullLoggerFactory.Instance);
        return config.CreateMapper();
    }

    private RoomsController GetController(
        Mock<IRoomApplicationService> mockRoomService,
        Mock<IBookingRequestApplicationService> mockBookingService,
        IMapper mapper)
    {
        var mockTenantService = new Mock<ITenantApplicationService>();
        var mockMaintenanceService = new Mock<IMaintenanceRequestApplicationService>();
        var controller = new RoomsController(
            mockRoomService.Object, 
            mockBookingService.Object, 
            mockTenantService.Object,
            mockMaintenanceService.Object,
            mapper);
        
        var tempData = new TempDataDictionary(
            new DefaultHttpContext(),
            Mock.Of<ITempDataProvider>()
        );
        controller.TempData = tempData;
        
        return controller;
    }

    [Fact]
    public async Task Index_ReturnsViewWithModel()
    {
        // Arrange
        var mapper = GetMapper();
        var mockRoomService = new Mock<IRoomApplicationService>();
        var mockBookingService = new Mock<IBookingRequestApplicationService>();

        var rooms = new List<RoomWithTenantsDto>
        {
            new RoomWithTenantsDto { RoomId = 1, Number = "101", Type = "Single", Status = "Available", Tenants = new List<TenantDto>() }
        };

        mockRoomService.Setup(s => s.GetAllRoomsWithTenantsAsync())
            .ReturnsAsync(ServiceResult<IEnumerable<RoomWithTenantsDto>>.Success(rooms));
        mockRoomService.Setup(s => s.GetOccupiedRoomsAsync())
            .ReturnsAsync(ServiceResult<IEnumerable<RoomDto>>.Success(new List<RoomDto>()));
        mockRoomService.Setup(s => s.GetAvailableRoomsAsync())
            .ReturnsAsync(ServiceResult<IEnumerable<RoomDto>>.Success(new List<RoomDto>()));
        mockBookingService.Setup(s => s.GetPendingBookingRequestsAsync())
            .ReturnsAsync(ServiceResult<IEnumerable<BookingRequestDto>>.Success(new List<BookingRequestDto>()));
        mockBookingService.Setup(s => s.GetConfirmedBookingRequestsAsync())
            .ReturnsAsync(ServiceResult<IEnumerable<BookingRequestDto>>.Success(new List<BookingRequestDto>()));

        var controller = GetController(mockRoomService, mockBookingService, mapper);

        // Act
        var result = await controller.Index();

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.IsType<RoomsTabViewModel>(viewResult.Model);
    }

    [Fact]
    public async Task CreateOrEdit_NewRoom_CreatesRoomAndRedirects()
    {
        // Arrange
        var mapper = GetMapper();
        var mockRoomService = new Mock<IRoomApplicationService>();
        var mockBookingService = new Mock<IBookingRequestApplicationService>();

        var createdRoom = new RoomDto
        {
            RoomId = 1,
            Number = "102",
            Type = "Double",
            Status = "Available"
        };

        mockRoomService.Setup(s => s.CreateRoomAsync(It.IsAny<CreateRoomDto>()))
            .ReturnsAsync(ServiceResult<RoomDto>.Success(createdRoom));

        var controller = GetController(mockRoomService, mockBookingService, mapper);
        var model = new RoomFormViewModel { Number = "102", Type = "Double", Status = "Available" };

        // Act
        var result = await controller.CreateOrEdit(model);

        // Assert
        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
        Assert.Equal("Room created successfully.", controller.TempData["Success"]);
        mockRoomService.Verify(s => s.CreateRoomAsync(It.IsAny<CreateRoomDto>()), Times.Once);
    }

    [Fact]
    public async Task CreateOrEdit_UpdateRoom_UpdatesRoomAndRedirects()
    {
        // Arrange
        var mapper = GetMapper();
        var mockRoomService = new Mock<IRoomApplicationService>();
        var mockBookingService = new Mock<IBookingRequestApplicationService>();

        var updatedRoom = new RoomDto
        {
            RoomId = 2,
            Number = "103A",
            Type = "Suite",
            Status = "Occupied"
        };

        mockRoomService.Setup(s => s.UpdateRoomAsync(It.IsAny<int>(), It.IsAny<UpdateRoomDto>()))
            .ReturnsAsync(ServiceResult<RoomDto>.Success(updatedRoom));

        var controller = GetController(mockRoomService, mockBookingService, mapper);
        var model = new RoomFormViewModel { RoomId = 2, Number = "103A", Type = "Suite", Status = "Occupied" };

        // Act
        var result = await controller.CreateOrEdit(model);

        // Assert
        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
        Assert.Equal("Room updated successfully.", controller.TempData["Success"]);
        mockRoomService.Verify(s => s.UpdateRoomAsync(2, It.IsAny<UpdateRoomDto>()), Times.Once);
    }

    [Fact]
    public async Task Delete_ValidRoom_DeletesRoomAndRedirects()
    {
        // Arrange
        var mapper = GetMapper();
        var mockRoomService = new Mock<IRoomApplicationService>();
        var mockBookingService = new Mock<IBookingRequestApplicationService>();

        mockRoomService.Setup(s => s.DeleteRoomAsync(1))
            .ReturnsAsync(ServiceResult<bool>.Success(true));

        var controller = GetController(mockRoomService, mockBookingService, mapper);

        // Act
        var result = await controller.Delete(1);

        // Assert
        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
        Assert.Equal("Room deleted successfully.", controller.TempData["Success"]);
        mockRoomService.Verify(s => s.DeleteRoomAsync(1), Times.Once);
    }

    [Fact]
    public async Task Delete_RoomNotFound_ReturnsRedirectWithError()
    {
        // Arrange
        var mapper = GetMapper();
        var mockRoomService = new Mock<IRoomApplicationService>();
        var mockBookingService = new Mock<IBookingRequestApplicationService>();

        mockRoomService.Setup(s => s.DeleteRoomAsync(999))
            .ReturnsAsync(ServiceResult<bool>.Failure("Room not found"));

        var controller = GetController(mockRoomService, mockBookingService, mapper);

        // Act
        var result = await controller.Delete(999);

        // Assert
        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
        Assert.Equal("Room not found", controller.TempData["Error"]);
        mockRoomService.Verify(s => s.DeleteRoomAsync(999), Times.Once);
    }

    [Fact]
    public async Task BookRoom_ValidRoom_ReturnsPartialView()
    {
        // Arrange
        var mapper = GetMapper();
        var mockRoomService = new Mock<IRoomApplicationService>();
        var mockBookingService = new Mock<IBookingRequestApplicationService>();

        var room = new RoomDto { RoomId = 1, Number = "101", Type = "Single", Status = "Available" };
        var availableRooms = new List<RoomDto> { room };

        mockRoomService.Setup(s => s.GetRoomByIdAsync(1))
            .ReturnsAsync(ServiceResult<RoomDto>.Success(room));
        mockRoomService.Setup(s => s.GetAvailableRoomsAsync())
            .ReturnsAsync(ServiceResult<IEnumerable<RoomDto>>.Success(availableRooms));

        var controller = GetController(mockRoomService, mockBookingService, mapper);

        // Act
        var result = await controller.BookRoom(1);

        // Assert
        var partial = Assert.IsType<PartialViewResult>(result);
        Assert.Equal("_BookingModal", partial.ViewName);
        Assert.IsType<BookingRequestViewModel>(partial.Model);
        mockRoomService.Verify(s => s.GetRoomByIdAsync(1), Times.Once);
    }

    [Fact]
    public async Task BookRoom_Post_ValidModel_CreatesBookingAndRedirects()
    {
        // Arrange
        var mapper = GetMapper();
        var mockRoomService = new Mock<IRoomApplicationService>();
        var mockBookingService = new Mock<IBookingRequestApplicationService>();

        var createdBooking = new BookingRequestDto
        {
            BookingRequestId = 1,
            RoomId = 1,
            FullName = "Test User",
            Contact = "123456789",
            Status = "Pending"
        };

        mockBookingService.Setup(s => s.CreateBookingRequestAsync(It.IsAny<CreateBookingRequestDto>()))
            .ReturnsAsync(ServiceResult<BookingRequestDto>.Success(createdBooking));

        var controller = GetController(mockRoomService, mockBookingService, mapper);

        var model = new BookingRequestViewModel
        {
            RoomId = 1,
            FullName = "Test User",
            Contact = "123456789",
            Note = "Test booking"
        };

        // Act
        var result = await controller.BookRoom(model, null);

        // Assert
        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
        Assert.Equal("Booking request submitted successfully.", controller.TempData["Success"]);
        mockBookingService.Verify(s => s.CreateBookingRequestAsync(It.IsAny<CreateBookingRequestDto>()), Times.Once);
    }

    [Fact]
    public async Task DeleteBookingRequest_DeletesBookingAndRedirects()
    {
        // Arrange
        var mapper = GetMapper();
        var mockRoomService = new Mock<IRoomApplicationService>();
        var mockBookingService = new Mock<IBookingRequestApplicationService>();

        mockBookingService.Setup(s => s.DeleteBookingRequestAsync(1))
            .ReturnsAsync(ServiceResult<bool>.Success(true));

        var controller = GetController(mockRoomService, mockBookingService, mapper);

        // Act
        var result = await controller.DeleteBookingRequest(1);

        // Assert
        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
        Assert.Equal("Booking request deleted successfully.", controller.TempData["Success"]);
        mockBookingService.Verify(s => s.DeleteBookingRequestAsync(1), Times.Once);
    }

    [Fact]
    public async Task GetRoom_ValidId_ReturnsJsonResult()
    {
        // Arrange
        var mapper = GetMapper();
        var mockRoomService = new Mock<IRoomApplicationService>();
        var mockBookingService = new Mock<IBookingRequestApplicationService>();

        var room = new RoomDto { RoomId = 1, Number = "101", Type = "Single", Status = "Available", CottageId = 1 };

        mockRoomService.Setup(s => s.GetRoomByIdAsync(1))
            .ReturnsAsync(ServiceResult<RoomDto>.Success(room));

        var controller = GetController(mockRoomService, mockBookingService, mapper);

        // Act
        var result = await controller.GetRoom(1);

        // Assert
        var jsonResult = Assert.IsType<JsonResult>(result);
        Assert.NotNull(jsonResult.Value);
        mockRoomService.Verify(s => s.GetRoomByIdAsync(1), Times.Once);
    }

    [Fact]
    public async Task GetRoom_InvalidId_ReturnsNotFound()
    {
        // Arrange
        var mapper = GetMapper();
        var mockRoomService = new Mock<IRoomApplicationService>();
        var mockBookingService = new Mock<IBookingRequestApplicationService>();

        mockRoomService.Setup(s => s.GetRoomByIdAsync(999))
            .ReturnsAsync(ServiceResult<RoomDto>.Failure("Room not found"));

        var controller = GetController(mockRoomService, mockBookingService, mapper);

        // Act
        var result = await controller.GetRoom(999);

        // Assert
        Assert.IsType<NotFoundResult>(result);
        mockRoomService.Verify(s => s.GetRoomByIdAsync(999), Times.Once);
    }
}