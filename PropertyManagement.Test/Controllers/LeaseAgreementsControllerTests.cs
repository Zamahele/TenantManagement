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
        expr.CreateMap<Tenant, TenantViewModel>().ReverseMap();
        expr.CreateMap<Room, RoomViewModel>().ReverseMap();
        var config = new MapperConfiguration(expr, NullLoggerFactory.Instance);
        return config.CreateMapper();
    }

    private LeaseAgreementsController GetController(ApplicationDbContext context, string webRootPath = "wwwroot")
    {
        // Mock repositories
        var leaseRepo = new Mock<IGenericRepository<LeaseAgreement>>();
        leaseRepo.Setup(r => r.UpdateAsync(It.IsAny<LeaseAgreement>())).Returns(Task.CompletedTask);

        var tenantRepo = new Mock<IGenericRepository<Tenant>>();
        var roomRepo = new Mock<IGenericRepository<Room>>();

        // Setup Query() to return DbSet as IQueryable for each repo
        leaseRepo.Setup(r => r.Query()).Returns(context.LeaseAgreements);
        tenantRepo.Setup(r => r.Query()).Returns(context.Tenants);
        roomRepo.Setup(r => r.Query()).Returns(context.Rooms);

        var envMock = new Mock<IWebHostEnvironment>();
        envMock.Setup(e => e.WebRootPath).Returns(webRootPath);

        var mapper = GetMapper();

        var controller = new LeaseAgreementsController(
            leaseRepo.Object,
            tenantRepo.Object,
            roomRepo.Object,
            envMock.Object,
            mapper
        );

        var tempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>());
        controller.TempData = tempData;
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
        context.SaveChanges();

        var controller = GetController(context);

        var result = await controller.Index();

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.NotNull(viewResult.Model);
    }

    [Fact]
    public async Task GetAgreement_ValidId_ReturnsJson()
    {
        var context = GetDbContext();
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
        context.SaveChanges();

        var controller = GetController(context);

        var result = await controller.GetAgreement(1);

        var json = Assert.IsType<JsonResult>(result);
        Assert.NotNull(json.Value);
    }

    [Fact]
    public async Task Edit_ValidId_ReturnsViewWithAgreement()
    {
        var context = GetDbContext();
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
        context.SaveChanges();

        var controller = GetController(context);

        var result = await controller.Edit(1);

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Equal("CreateOrEdit", viewResult.ViewName);
        Assert.NotNull(viewResult.Model);
    }

    [Fact]
    public async Task CreateOrEdit_CreatesAgreementAndRedirects()
    {
        var context = GetDbContext();
        context.Rooms.Add(new Room { RoomId = 1, Number = "101", Type = "Single", Status = "Available" });
        context.Tenants.Add(new Tenant { TenantId = 1, RoomId = 1, FullName = "Tenant", Contact = "123", EmergencyContactName = "EC", EmergencyContactNumber = "123" });
        context.SaveChanges();

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
        Assert.Equal("Lease agreement created successfully.", controller.TempData["Success"]);
    }

    [Fact]
    public async Task CreateOrEdit_UpdatesAgreementAndRedirects()
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
        context.SaveChanges();

        var controller = GetController(context);

        var updatedAgreement = new LeaseAgreement
        {
            LeaseAgreementId = 1,
            TenantId = 1,
            RoomId = 1,
            StartDate = DateTime.UtcNow.AddMonths(-1),
            EndDate = DateTime.UtcNow.AddMonths(2),
            RentAmount = 1200,
            ExpectedRentDay = 2
        };

    var agreementVm = GetMapper().Map<LeaseAgreementViewModel>(updatedAgreement);
    var result = await controller.CreateOrEdit(agreementVm, null);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
        Assert.Equal("Lease agreement updated successfully.", controller.TempData["Success"]);
    }

    [Fact]
    public async Task Delete_DeletesAgreementAndRedirects()
    {
        var context = GetDbContext();
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
        context.SaveChanges();

        var controller = GetController(context);

        var result = await controller.Delete(1);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
        Assert.Equal("Lease agreement deleted successfully.", controller.TempData["Success"]);
    }

    [Fact]
    public async Task LeaseAgreementModal_New_ReturnsPartialView()
    {
        var context = GetDbContext();
        context.Tenants.Add(new Tenant { TenantId = 1, RoomId = 1, FullName = "Tenant", Contact = "123", EmergencyContactName = "EC", EmergencyContactNumber = "123" });
        context.SaveChanges();

        var controller = GetController(context);

        var result = await controller.LeaseAgreementModal(null);

        var partial = Assert.IsType<PartialViewResult>(result);
        Assert.Equal("_LeaseAgreementModal", partial.ViewName);
        Assert.NotNull(partial.Model);
    }

    [Fact]
    public async Task LeaseAgreementModal_Edit_ReturnsPartialView()
    {
        var context = GetDbContext();
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
        context.SaveChanges();

        var controller = GetController(context);

        var result = await controller.LeaseAgreementModal(1);

        var partial = Assert.IsType<PartialViewResult>(result);
        Assert.Equal("_LeaseAgreementModal", partial.ViewName);
        Assert.NotNull(partial.Model);
    }

    [Fact]
    public async Task GetRoomIdByTenant_ReturnsJsonWithRoomId()
    {
        var context = GetDbContext();
        context.Rooms.Add(new Room { RoomId = 1, Number = "101", Type = "Single", Status = "Available" });
        context.Tenants.Add(new Tenant { TenantId = 1, RoomId = 1, FullName = "Tenant", Contact = "123", EmergencyContactName = "EC", EmergencyContactNumber = "123" });
        context.SaveChanges();

        var controller = GetController(context);

        var result = await controller.GetRoomIdByTenant(1);

        var json = Assert.IsType<JsonResult>(result);
        var value = json.Value;
        var roomId = (int)value.GetType().GetProperty("roomId")!.GetValue(value)!;
        Assert.Equal(1, roomId);
    }
}