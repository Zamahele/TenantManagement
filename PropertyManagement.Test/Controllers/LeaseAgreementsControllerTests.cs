using AutoMapper;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using PropertyManagement.Domain.Entities;
using PropertyManagement.Infrastructure.Data;
using PropertyManagement.Infrastructure.Repositories;
using PropertyManagement.Web.Controllers;
using PropertyManagement.Web.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Assert = Xunit.Assert;

namespace PropertyManagement.Test.Controllers;

public class LeaseAgreementsControllerTests
{
    private ApplicationDbContext GetDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    private IMapper GetMapper()
    {
        var expr = new MapperConfigurationExpression();
        expr.CreateMap<LeaseAgreement, LeaseAgreementViewModel>().ReverseMap();
        expr.CreateMap<Tenant, TenantViewModel>()
            .ForMember(dest => dest.Room, opt => opt.MapFrom(src => src.Room))
            .ReverseMap();
        expr.CreateMap<Room, RoomViewModel>().ReverseMap();
        var config = new MapperConfiguration(expr, NullLoggerFactory.Instance);
        return config.CreateMapper();
    }

    private LeaseAgreementsController GetController(ApplicationDbContext context, string webRootPath = "wwwroot")
    {
        // Use actual repository implementations with the in-memory database
        var leaseRepo = new GenericRepository<LeaseAgreement>(context);
        var tenantRepo = new GenericRepository<Tenant>(context);
        var roomRepo = new GenericRepository<Room>(context);

        var envMock = new Mock<IWebHostEnvironment>();
        envMock.Setup(e => e.WebRootPath).Returns(webRootPath);

        var mapper = GetMapper();

        var controller = new LeaseAgreementsController(
            leaseRepo,
            tenantRepo,
            roomRepo,
            envMock.Object,
            mapper
        );

        // Setup TempData properly
        var tempDataProvider = new Mock<ITempDataProvider>();
        var tempDataDictionary = new TempDataDictionary(new DefaultHttpContext(), tempDataProvider.Object);
        controller.TempData = tempDataDictionary;
        
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };

        // Add this to prevent NullReferenceException in TryValidateModel
        var objectValidator = new Mock<IObjectModelValidator>();
        objectValidator.Setup(o => o.Validate(
            It.IsAny<ActionContext>(),
            It.IsAny<ValidationStateDictionary>(),
            It.IsAny<string>(),
            It.IsAny<object>()));
        controller.ObjectValidator = objectValidator.Object;

        return controller;
    }

    [Fact]
    public async Task Index_ReturnsViewWithAgreements()
    {
        var context = GetDbContext();
        context.Rooms.Add(new Room { RoomId = 1, Number = "101", Type = "Single", Status = "Available" });
        context.Tenants.Add(new Tenant { TenantId = 1, RoomId = 1, FullName = "Tenant", Contact = "123", EmergencyContactName = "EC", EmergencyContactNumber = "123" });
        context.LeaseAgreements.Add(new LeaseAgreement
        {
            LeaseAgreementId = 1,
            TenantId = 1,
            RoomId = 1,
            StartDate = DateTime.UtcNow.AddMonths(-1),
            EndDate = DateTime.UtcNow.AddMonths(1),
            RentAmount = 1000,
            ExpectedRentDay = 1
        });
        await context.SaveChangesAsync();

        var controller = GetController(context);

        var result = await controller.Index();

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.NotNull(viewResult.Model);
    }

    [Fact]
    public async Task Create_ReturnsViewWithNewModel()
    {
        var context = GetDbContext();
        context.Tenants.Add(new Tenant { TenantId = 1, RoomId = 1, FullName = "Tenant", Contact = "123", EmergencyContactName = "EC", EmergencyContactNumber = "123" });
        await context.SaveChangesAsync();

        var controller = GetController(context);

        var result = await controller.Create();

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.NotNull(viewResult.Model);
        var model = Assert.IsType<LeaseAgreementViewModel>(viewResult.Model);
        Assert.Equal(0, model.LeaseAgreementId); // New model should have default ID
    }

    [Fact]
    public async Task GetAgreement_InvalidId_ReturnsNotFound()
    {
        var context = GetDbContext();
        var controller = GetController(context);

        var result = await controller.GetAgreement(999);

        var notFoundResult = Assert.IsType<NotFoundResult>(result);
        Assert.NotNull(notFoundResult);
    }

    [Fact]
    public async Task Edit_ValidId_ReturnsViewWithAgreement()
    {
        var context = GetDbContext();
        var room = new Room { RoomId = 1, Number = "101", Type = "Single", Status = "Available" };
        var tenant = new Tenant { TenantId = 1, RoomId = 1, FullName = "Tenant", Contact = "123", EmergencyContactName = "EC", EmergencyContactNumber = "123", Room = room };
        var leaseAgreement = new LeaseAgreement
        {
            LeaseAgreementId = 1,
            TenantId = 1,
            RoomId = 1,
            StartDate = DateTime.UtcNow.AddMonths(-1),
            EndDate = DateTime.UtcNow.AddMonths(1),
            RentAmount = 1000,
            ExpectedRentDay = 1,
            Tenant = tenant,
            Room = room
        };
        
        context.Rooms.Add(room);
        context.Tenants.Add(tenant);
        context.LeaseAgreements.Add(leaseAgreement);
        await context.SaveChangesAsync();

        var controller = GetController(context);

        var result = await controller.Edit(1);

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Equal("Create", viewResult.ViewName); // Updated to use "Create" view
        Assert.NotNull(viewResult.Model);
        var model = Assert.IsType<LeaseAgreementViewModel>(viewResult.Model);
        Assert.Equal(1, model.LeaseAgreementId);
    }

    [Fact]
    public async Task Edit_InvalidId_ReturnsNotFound()
    {
        var context = GetDbContext();
        var controller = GetController(context);

        var result = await controller.Edit(999); // Non-existent ID

        var notFoundResult = Assert.IsType<NotFoundResult>(result);
        Assert.NotNull(notFoundResult);
    }

    [Fact]
    public async Task CreateOrEdit_CreatesAgreementAndRedirects()
    {
        var context = GetDbContext();
        context.Rooms.Add(new Room { RoomId = 1, Number = "101", Type = "Single", Status = "Available" });
        context.Tenants.Add(new Tenant { TenantId = 1, RoomId = 1, FullName = "Tenant", Contact = "123", EmergencyContactName = "EC", EmergencyContactNumber = "123" });
        await context.SaveChangesAsync();

        var controller = GetController(context);

        var agreement = new LeaseAgreement
        {
            TenantId = 1,
            RoomId = 1,
            StartDate = DateTime.UtcNow.AddDays(-1),
            EndDate = DateTime.UtcNow.AddMonths(1),
            RentAmount = 1000,
            ExpectedRentDay = 1
        };

        // Map to ViewModel
        var agreementVm = GetMapper().Map<LeaseAgreementViewModel>(agreement);

        var result = await controller.CreateOrEdit(agreementVm, null);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
        // Note: Don't test TempData due to BaseController override behavior
    }

    [Fact]
    public async Task CreateOrEdit_UpdatesAgreementAndRedirects()
    {
        var context = GetDbContext();
        context.Rooms.Add(new Room { RoomId = 1, Number = "101", Type = "Single", Status = "Available" });
        context.Tenants.Add(new Tenant { TenantId = 1, RoomId = 1, FullName = "Tenant", Contact = "123", EmergencyContactName = "EC", EmergencyContactNumber = "123" });
        var existingAgreement = new LeaseAgreement
        {
            LeaseAgreementId = 1,
            TenantId = 1,
            RoomId = 1,
            StartDate = DateTime.UtcNow.AddMonths(-1),
            EndDate = DateTime.UtcNow.AddMonths(1),
            RentAmount = 1000,
            ExpectedRentDay = 1
        };
        context.LeaseAgreements.Add(existingAgreement);
        await context.SaveChangesAsync();

        var controller = GetController(context);

        var updatedAgreement = new LeaseAgreementViewModel
        {
            LeaseAgreementId = 1,
            TenantId = 1,
            RoomId = 1,
            StartDate = DateTime.UtcNow.AddMonths(-1),
            EndDate = DateTime.UtcNow.AddMonths(2),
            RentAmount = 1200,
            ExpectedRentDay = 2
        };

        var result = await controller.CreateOrEdit(updatedAgreement, null);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
        // Note: Don't test TempData due to BaseController override behavior
    }

    [Fact]
    public async Task CreateOrEdit_InvalidModel_ReturnsViewWithErrors()
    {
        var context = GetDbContext();
        var controller = GetController(context);

        // Create invalid model (end date before start date)
        var agreementVm = new LeaseAgreementViewModel
        {
            TenantId = 1,
            RoomId = 1,
            StartDate = DateTime.UtcNow.AddMonths(1),
            EndDate = DateTime.UtcNow.AddDays(-1), // Invalid: end before start
            RentAmount = 1000,
            ExpectedRentDay = 1
        };

        var result = await controller.CreateOrEdit(agreementVm, null);

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Equal("Create", viewResult.ViewName);
        Assert.False(controller.ModelState.IsValid);
    }

    [Fact]
    public async Task Delete_NonExistentId_ReturnsRedirectToIndex()
    {
        var context = GetDbContext();
        var controller = GetController(context);

        var result = await controller.Delete(999);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
        
        // No entity should exist with this ID
        var entity = await context.LeaseAgreements.FindAsync(999);
        Assert.Null(entity);
    }

    [Fact]
    public async Task LeaseAgreementModal_New_ReturnsPartialView()
    {
        var context = GetDbContext();
        context.Tenants.Add(new Tenant { TenantId = 1, RoomId = 1, FullName = "Tenant", Contact = "123", EmergencyContactName = "EC", EmergencyContactNumber = "123" });
        await context.SaveChangesAsync();

        var controller = GetController(context);

        var result = await controller.LeaseAgreementModal(null);

        var partial = Assert.IsType<PartialViewResult>(result);
        Assert.Equal("_LeaseAgreementModal", partial.ViewName);
        Assert.NotNull(partial.Model);
    }

    [Fact]
    public async Task LeaseAgreementModal_InvalidId_ReturnsNotFound()
    {
        var context = GetDbContext();
        var controller = GetController(context);

        var result = await controller.LeaseAgreementModal(999);

        var notFoundResult = Assert.IsType<NotFoundResult>(result);
        Assert.NotNull(notFoundResult);
    }

    [Fact]
    public async Task GetRoomIdByTenant_ReturnsJsonWithRoomId()
    {
        var context = GetDbContext();
        context.Rooms.Add(new Room { RoomId = 1, Number = "101", Type = "Single", Status = "Available" });
        context.Tenants.Add(new Tenant { TenantId = 1, RoomId = 1, FullName = "Tenant", Contact = "123", EmergencyContactName = "EC", EmergencyContactNumber = "123" });
        await context.SaveChangesAsync();

        var controller = GetController(context);

        var result = await controller.GetRoomIdByTenant(1);

        var json = Assert.IsType<JsonResult>(result);
        var value = json.Value;
        var roomId = (int)value.GetType().GetProperty("roomId")!.GetValue(value)!;
        Assert.Equal(1, roomId);
    }

    [Fact]
    public async Task GetRoomIdByTenant_NonExistentTenant_ReturnsJsonWithZeroRoomId()
    {
        var context = GetDbContext();
        var controller = GetController(context);

        var result = await controller.GetRoomIdByTenant(999);

        var json = Assert.IsType<JsonResult>(result);
        var value = json.Value;
        var roomId = (int)value.GetType().GetProperty("roomId")!.GetValue(value)!;
        
        // When no tenant is found, FirstOrDefaultAsync returns 0 for RoomId (int default)
        Assert.Equal(0, roomId);
    }
}