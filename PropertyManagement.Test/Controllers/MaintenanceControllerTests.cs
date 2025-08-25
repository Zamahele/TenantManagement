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

namespace PropertyManagement.Test.Controllers;

public class MaintenanceControllerTests
{
    private IMapper GetMapper()
    {
        var expr = new MapperConfigurationExpression();
        expr.CreateMap<MaintenanceRequestDto, MaintenanceRequestViewModel>().ReverseMap();
        expr.CreateMap<RoomDto, RoomViewModel>().ReverseMap();
        expr.CreateMap<TenantDto, TenantViewModel>().ReverseMap();
        expr.CreateMap<MaintenanceRequestFormViewModel, CreateMaintenanceRequestDto>().ReverseMap();
        expr.CreateMap<MaintenanceRequestFormViewModel, UpdateMaintenanceRequestDto>().ReverseMap();
        expr.CreateMap<MaintenanceRequestDto, MaintenanceRequestFormViewModel>().ReverseMap();
        
        var config = new MapperConfiguration(expr, NullLoggerFactory.Instance);
        return config.CreateMapper();
    }

    private ClaimsPrincipal GetUser(string role)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, "1"),
            new Claim(ClaimTypes.Role, role)
        };
        return new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));
    }

    private MaintenanceController CreateController(
        Mock<IMaintenanceRequestApplicationService>? mockMaintenanceService = null,
        Mock<IRoomApplicationService>? mockRoomService = null,
        Mock<ITenantApplicationService>? mockTenantService = null,
        ClaimsPrincipal? user = null)
    {
        var mapper = GetMapper();
        
        mockMaintenanceService ??= new Mock<IMaintenanceRequestApplicationService>();
        mockRoomService ??= new Mock<IRoomApplicationService>();
        mockTenantService ??= new Mock<ITenantApplicationService>();
        
        var controller = new MaintenanceController(
            mockMaintenanceService.Object, 
            mockRoomService.Object, 
            mockTenantService.Object, 
            mapper);
            
        controller.ControllerContext = new ControllerContext();
        controller.ControllerContext.HttpContext = new DefaultHttpContext { User = user ?? GetUser("Manager") };
        controller.TempData = new TempDataDictionary(controller.HttpContext, Mock.Of<ITempDataProvider>());
        
        return controller;
    }

    [Fact]
    public async Task Index_Manager_ReturnsViewWithAllRequests()
    {
        // Arrange
        var mockMaintenanceService = new Mock<IMaintenanceRequestApplicationService>();
        var requests = new List<MaintenanceRequestDto>
        {
            new MaintenanceRequestDto { MaintenanceRequestId = 1, Description = "Test 1" },
            new MaintenanceRequestDto { MaintenanceRequestId = 2, Description = "Test 2" }
        };
        
        mockMaintenanceService.Setup(s => s.GetAllMaintenanceRequestsAsync())
            .ReturnsAsync(ServiceResult<IEnumerable<MaintenanceRequestDto>>.Success(requests));
        
        var user = GetUser("Manager");
        var controller = CreateController(mockMaintenanceService, user: user);

        // Act
        var result = await controller.Index();

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsAssignableFrom<List<MaintenanceRequestViewModel>>(viewResult.Model);
        Assert.Equal(2, model.Count);
        Assert.True((bool)controller.ViewBag.IsManager);
    }

    [Fact]
    public async Task Index_Tenant_ReturnsViewWithTenantRequests()
    {
        // Arrange
        var mockMaintenanceService = new Mock<IMaintenanceRequestApplicationService>();
        var mockTenantService = new Mock<ITenantApplicationService>();
        
        var tenant = new TenantDto { TenantId = 1, UserId = 1 };
        var requests = new List<MaintenanceRequestDto>
        {
            new MaintenanceRequestDto { MaintenanceRequestId = 1, TenantId = "1" },
            new MaintenanceRequestDto { MaintenanceRequestId = 2, TenantId = "2" }
        };
        
        mockTenantService.Setup(s => s.GetTenantByUserIdAsync(1))
            .ReturnsAsync(ServiceResult<TenantDto>.Success(tenant));
        mockMaintenanceService.Setup(s => s.GetAllMaintenanceRequestsAsync())
            .ReturnsAsync(ServiceResult<IEnumerable<MaintenanceRequestDto>>.Success(requests));
        
        var user = GetUser("Tenant");
        var controller = CreateController(mockMaintenanceService, mockTenantService: mockTenantService, user: user);

        // Act
        var result = await controller.Index();

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsAssignableFrom<List<MaintenanceRequestViewModel>>(viewResult.Model);
        Assert.Single(model); // Only tenant's own request
        Assert.False((bool)controller.ViewBag.IsManager);
    }

    [Fact]
    public async Task MaintenanceRequestForm_NewRequest_ReturnsViewWithEmptyModel()
    {
        // Arrange
        var mockRoomService = new Mock<IRoomApplicationService>();
        var mockTenantService = new Mock<ITenantApplicationService>();
        
        var rooms = new List<RoomDto> 
        { 
            new RoomDto { RoomId = 1, Number = "101" },
            new RoomDto { RoomId = 2, Number = "102" }
        };
        var tenants = new List<TenantDto>
        {
            new TenantDto { TenantId = 1, RoomId = 1 }
        };
        
        mockRoomService.Setup(s => s.GetAllRoomsAsync())
            .ReturnsAsync(ServiceResult<IEnumerable<RoomDto>>.Success(rooms));
        mockTenantService.Setup(s => s.GetAllTenantsAsync())
            .ReturnsAsync(ServiceResult<IEnumerable<TenantDto>>.Success(tenants));
        
        var controller = CreateController(mockRoomService: mockRoomService, mockTenantService: mockTenantService);

        // Act
        var result = await controller.MaintenanceRequestForm(null);

        // Assert
        var partialViewResult = Assert.IsType<PartialViewResult>(result);
        var model = Assert.IsType<MaintenanceRequestFormViewModel>(partialViewResult.Model);
        Assert.Equal(0, model.MaintenanceRequestId);
        Assert.Single(model.RoomOptions); // Only room with tenant
    }

    [Fact]
    public async Task MaintenanceRequestForm_EditRequest_ReturnsViewWithModel()
    {
        // Arrange
        var mockMaintenanceService = new Mock<IMaintenanceRequestApplicationService>();
        var mockRoomService = new Mock<IRoomApplicationService>();
        var mockTenantService = new Mock<ITenantApplicationService>();
        
        var request = new MaintenanceRequestDto 
        { 
            MaintenanceRequestId = 1, 
            Description = "Test maintenance",
            RoomId = 1
        };
        var rooms = new List<RoomDto> { new RoomDto { RoomId = 1, Number = "101" } };
        var tenants = new List<TenantDto> { new TenantDto { TenantId = 1, RoomId = 1 } };
        
        mockMaintenanceService.Setup(s => s.GetMaintenanceRequestByIdAsync(1))
            .ReturnsAsync(ServiceResult<MaintenanceRequestDto>.Success(request));
        mockRoomService.Setup(s => s.GetAllRoomsAsync())
            .ReturnsAsync(ServiceResult<IEnumerable<RoomDto>>.Success(rooms));
        mockTenantService.Setup(s => s.GetAllTenantsAsync())
            .ReturnsAsync(ServiceResult<IEnumerable<TenantDto>>.Success(tenants));
        
        var controller = CreateController(mockMaintenanceService, mockRoomService, mockTenantService);

        // Act
        var result = await controller.MaintenanceRequestForm(1);

        // Assert
        var partialViewResult = Assert.IsType<PartialViewResult>(result);
        var model = Assert.IsType<MaintenanceRequestFormViewModel>(partialViewResult.Model);
        Assert.Equal(1, model.MaintenanceRequestId);
        Assert.Equal("Test maintenance", model.Description);
    }

    [Fact]
    public async Task CreateOrEdit_ValidModel_ReturnsRedirectToIndex()
    {
        // Arrange
        var mockMaintenanceService = new Mock<IMaintenanceRequestApplicationService>();
        var mockRoomService = new Mock<IRoomApplicationService>();
        var mockTenantService = new Mock<ITenantApplicationService>();
        
        mockMaintenanceService.Setup(s => s.CreateMaintenanceRequestAsync(It.IsAny<CreateMaintenanceRequestDto>()))
            .ReturnsAsync(ServiceResult<MaintenanceRequestDto>.Success(new MaintenanceRequestDto()));
        
        // Setup room and tenant services to return successful results
        mockRoomService.Setup(s => s.GetAllRoomsAsync())
            .ReturnsAsync(ServiceResult<IEnumerable<RoomDto>>.Success(new List<RoomDto> 
            { 
                new RoomDto { RoomId = 1, Number = "101" } 
            }));
        mockTenantService.Setup(s => s.GetAllTenantsAsync())
            .ReturnsAsync(ServiceResult<IEnumerable<TenantDto>>.Success(new List<TenantDto> 
            { 
                new TenantDto { TenantId = 1, RoomId = 1 } 
            }));
        
        var controller = CreateController(mockMaintenanceService, mockRoomService, mockTenantService);
        var model = new MaintenanceRequestFormViewModel
        {
            RoomId = 1,
            Description = "Test request",
            Status = "Pending"
        };

        // Act
        var result = await controller.CreateOrEdit(model);

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirectResult.ActionName);
    }

    [Fact]
    public async Task Delete_ValidId_ReturnsRedirectToIndex()
    {
        // Arrange
        var mockMaintenanceService = new Mock<IMaintenanceRequestApplicationService>();
        mockMaintenanceService.Setup(s => s.GetMaintenanceRequestByIdAsync(1))
            .ReturnsAsync(ServiceResult<MaintenanceRequestDto>.Success(new MaintenanceRequestDto 
            { 
                MaintenanceRequestId = 1, 
                Room = new RoomDto { Number = "101" } 
            }));
        mockMaintenanceService.Setup(s => s.DeleteMaintenanceRequestAsync(1))
            .ReturnsAsync(ServiceResult<bool>.Success(true));
        
        var controller = CreateController(mockMaintenanceService);

        // Act
        var result = await controller.Delete(1);

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirectResult.ActionName);
    }

    [Fact]
    public async Task GetTenantForRoom_ValidRoomId_ReturnsJsonWithTenant()
    {
        // Arrange
        var mockTenantService = new Mock<ITenantApplicationService>();
        var tenants = new List<TenantDto>
        {
            new TenantDto { TenantId = 1, FullName = "John Doe", RoomId = 1 },
            new TenantDto { TenantId = 2, FullName = "Jane Smith", RoomId = 2 }
        };
        
        mockTenantService.Setup(s => s.GetAllTenantsAsync())
            .ReturnsAsync(ServiceResult<IEnumerable<TenantDto>>.Success(tenants));
        
        var controller = CreateController(mockTenantService: mockTenantService);

        // Act
        var result = await controller.GetTenantForRoom(1);

        // Assert
        var jsonResult = Assert.IsType<JsonResult>(result);
        var resultData = jsonResult.Value;
        Assert.NotNull(resultData);
    }

    [Fact]
    public async Task SubmitTenantRequest_ValidModel_ReturnsRedirectToIndex()
    {
        // Arrange
        var mockMaintenanceService = new Mock<IMaintenanceRequestApplicationService>();
        var mockTenantService = new Mock<ITenantApplicationService>();
        
        var tenant = new TenantDto { TenantId = 1, UserId = 1, RoomId = 1 };
        mockTenantService.Setup(s => s.GetTenantByUserIdAsync(1))
            .ReturnsAsync(ServiceResult<TenantDto>.Success(tenant));
        mockMaintenanceService.Setup(s => s.CreateMaintenanceRequestAsync(It.IsAny<CreateMaintenanceRequestDto>()))
            .ReturnsAsync(ServiceResult<MaintenanceRequestDto>.Success(new MaintenanceRequestDto()));
        
        var user = GetUser("Tenant");
        var controller = CreateController(mockMaintenanceService, mockTenantService: mockTenantService, user: user);
        var model = new MaintenanceRequestViewModel
        {
            Description = "Test tenant request"
        };

        // Act
        var result = await controller.SubmitTenantRequest(model);

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirectResult.ActionName);
    }
}