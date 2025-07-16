using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using PropertyManagement.Domain.Entities;
using PropertyManagement.Infrastructure.Data;
using PropertyManagement.Infrastructure.Repositories;
using PropertyManagement.Web.Controllers;
using PropertyManagement.Web.ViewModels;
using System;
using System.Threading.Tasks;
using Xunit;
using Assert = Xunit.Assert;

namespace PropertyManagement.Test.Controllers;

public class HomeControllerTests
{
    private ApplicationDbContext GetDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var context = new ApplicationDbContext(options);

        // Seed data for dashboard
        context.Rooms.AddRange(
            new Room { RoomId = 1, Number = "101", Type = "Single", Status = "Available" },
            new Room { RoomId = 2, Number = "102", Type = "Double", Status = "Occupied" },
            new Room { RoomId = 3, Number = "103", Type = "Suite", Status = "Under Maintenance" }
        );
        context.Tenants.Add(new Tenant { TenantId = 1, RoomId = 2, FullName = "Test Tenant", Contact = "123", EmergencyContactName = "EC", EmergencyContactNumber = "123" });
        context.LeaseAgreements.Add(new LeaseAgreement { LeaseAgreementId = 1, TenantId = 1, RoomId = 2, StartDate = DateTime.UtcNow.AddMonths(-1), EndDate = DateTime.UtcNow.AddMonths(1), RentAmount = 1000, ExpectedRentDay = 1 });
        context.MaintenanceRequests.Add(new MaintenanceRequest { MaintenanceRequestId = 1, RoomId = 3, TenantId = "1", Description = "Fix", RequestDate = DateTime.UtcNow, Status = "Pending" });
        context.SaveChanges();

        return context;
    }

    private ILogger<HomeController> GetLogger()
    {
        return new Mock<ILogger<HomeController>>().Object;
    }

    private IMapper GetMapper()
    {
        var expr = new MapperConfigurationExpression();
        expr.CreateMap<Room, RoomViewModel>().ReverseMap();
        expr.CreateMap<Tenant, TenantViewModel>().ReverseMap();
        expr.CreateMap<LeaseAgreement, LeaseAgreementViewModel>().ReverseMap();
        expr.CreateMap<MaintenanceRequest, MaintenanceRequestViewModel>().ReverseMap();
        var config = new MapperConfiguration(expr, NullLoggerFactory.Instance);
        return config.CreateMapper();
    }

    private HomeController GetController(ApplicationDbContext context)
    {
        var roomRepo = new Mock<IGenericRepository<Room>>();
        roomRepo.Setup(r => r.Query()).Returns(context.Rooms);

        var tenantRepo = new Mock<IGenericRepository<Tenant>>();
        tenantRepo.Setup(r => r.Query()).Returns(context.Tenants);

        var leaseRepo = new Mock<IGenericRepository<LeaseAgreement>>();
        leaseRepo.Setup(r => r.Query()).Returns(context.LeaseAgreements);

        var maintenanceRepo = new Mock<IGenericRepository<MaintenanceRequest>>();
        maintenanceRepo.Setup(r => r.Query()).Returns(context.MaintenanceRequests);

        var logger = GetLogger();
        var mapper = GetMapper();

        return new HomeController(
            roomRepo.Object,
            tenantRepo.Object,
            leaseRepo.Object,
            maintenanceRepo.Object,
            logger,
            mapper
        );
    }

    [Fact]
    public async Task Index_ReturnsViewWithDashboardViewModel()
    {
        var context = GetDbContext();
        var controller = GetController(context);

        var result = await controller.Index();

        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<DashboardViewModel>(viewResult.Model);

        Assert.Equal(3, model.TotalRooms);
        Assert.Equal(1, model.AvailableRooms);
        Assert.Equal(1, model.OccupiedRooms);
        Assert.Equal(1, model.UnderMaintenanceRooms);
        Assert.Equal(1, model.TotalTenants);
        Assert.Equal(1, model.ActiveLeases);
        Assert.True(model.ExpiringLeases >= 0);
        Assert.Equal(1, model.PendingRequests);
    }

    [Fact]
    public void Privacy_ReturnsView()
    {
        var context = GetDbContext();
        var controller = GetController(context);

        var result = controller.Privacy();

        Assert.IsType<ViewResult>(result);
    }

    [Fact]
    public void Error_ReturnsViewWithErrorViewModel()
    {
        var context = GetDbContext();
        var controller = GetController(context);

        // Setup fake HttpContext with TraceIdentifier
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
        controller.ControllerContext.HttpContext.TraceIdentifier = Guid.NewGuid().ToString();

        var result = controller.Error();

        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<ErrorViewModel>(viewResult.Model);
        Assert.False(string.IsNullOrEmpty(model.RequestId));
    }
}