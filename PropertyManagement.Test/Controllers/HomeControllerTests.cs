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
        roomRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(context.Rooms.ToList());

        var tenantRepo = new Mock<IGenericRepository<Tenant>>();
        tenantRepo.Setup(r => r.Query()).Returns(context.Tenants);
        tenantRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(context.Tenants.ToList());

        var leaseRepo = new Mock<IGenericRepository<LeaseAgreement>>();
        leaseRepo.Setup(r => r.Query()).Returns(context.LeaseAgreements);
        leaseRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(context.LeaseAgreements.ToList());

        var maintenanceRepo = new Mock<IGenericRepository<MaintenanceRequest>>();
        maintenanceRepo.Setup(r => r.Query()).Returns(context.MaintenanceRequests);
        maintenanceRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(context.MaintenanceRequests.ToList());
        maintenanceRepo.Setup(r => r.GetAllAsync(It.IsAny<System.Linq.Expressions.Expression<Func<MaintenanceRequest, bool>>>(), It.IsAny<System.Linq.Expressions.Expression<Func<MaintenanceRequest, object>>[]>()))
            .ReturnsAsync((System.Linq.Expressions.Expression<Func<MaintenanceRequest, bool>> filter, System.Linq.Expressions.Expression<Func<MaintenanceRequest, object>>[] includes) => 
            {
                var query = context.MaintenanceRequests.AsQueryable();
                if (filter != null) query = query.Where(filter);
                return query.ToList();
            });

        // Add mock repositories for new dependencies
        var paymentRepo = new Mock<IGenericRepository<Payment>>();
        paymentRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Payment>());

        var inspectionRepo = new Mock<IGenericRepository<Inspection>>();
        inspectionRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Inspection>());

        var bookingRequestRepo = new Mock<IGenericRepository<BookingRequest>>();
        bookingRequestRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<BookingRequest>());

        var waitingListRepo = new Mock<IGenericRepository<WaitingListEntry>>();
        waitingListRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<WaitingListEntry>());

        var logger = GetLogger();
        var mapper = GetMapper();

        return new HomeController(
            roomRepo.Object,
            tenantRepo.Object,
            leaseRepo.Object,
            maintenanceRepo.Object,
            paymentRepo.Object,
            inspectionRepo.Object,
            bookingRequestRepo.Object,
            waitingListRepo.Object,
            context,
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

    [Fact]
    public async Task Health_ReturnsOkResult()
    {
        var context = GetDbContext();
        var controller = GetController(context);

        var result = controller.Health();

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task Ready_WithHealthyDatabase_ReturnsOkResult()
    {
        var context = GetDbContext();
        var controller = GetController(context);

        var result = await controller.Ready();

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public void Live_ReturnsOkResult()
    {
        var context = GetDbContext();
        var controller = GetController(context);

        var result = controller.Live();

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }
}