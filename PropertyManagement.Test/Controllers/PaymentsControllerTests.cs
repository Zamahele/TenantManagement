using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
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
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Assert = Xunit.Assert;

namespace PropertyManagement.Test.Controllers;

public class PaymentsControllerTests
{
    private IMapper GetMapper()
    {
        var expr = new MapperConfigurationExpression();
        expr.CreateMap<PaymentViewModel, Payment>().ReverseMap();
        expr.CreateMap<Tenant, TenantViewModel>().ReverseMap();
        expr.CreateMap<Room, RoomViewModel>().ReverseMap();
        expr.CreateMap<LeaseAgreement, LeaseAgreementViewModel>().ReverseMap();

        var config = new MapperConfiguration(expr, NullLoggerFactory.Instance);
        return config.CreateMapper();
    }

    private ApplicationDbContext GetDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    private PaymentsController GetController(
        ApplicationDbContext context,
        IMapper autoMapper)
    {
        // Mock repositories
        var paymentRepo = new Mock<IGenericRepository<Payment>>();
        paymentRepo.Setup(r => r.Query()).Returns(context.Payments);

        var tenantRepo = new Mock<IGenericRepository<Tenant>>();
        tenantRepo.Setup(r => r.Query()).Returns(context.Tenants);

        var leaseAgreementRepo = new Mock<IGenericRepository<LeaseAgreement>>();
        leaseAgreementRepo.Setup(r => r.Query()).Returns(context.LeaseAgreements);

        var controller = new PaymentsController(
            paymentRepo.Object,
            tenantRepo.Object,
            leaseAgreementRepo.Object,
            autoMapper);

        var tempData = new TempDataDictionary(
            new DefaultHttpContext(),
            Mock.Of<ITempDataProvider>()
        );
        controller.TempData = tempData;
        return controller;
    }

    [Fact]
    public async Task Index_ReturnsViewWithPayments()
    {
        var context = GetDbContext();
        var mapper = GetMapper();
        context.Payments.Add(new Payment
        {
            PaymentId = 1,
            TenantId = 1,
            Amount = 1000,
            Type = "Rent",
            PaymentMonth = 1,
            PaymentYear = 2024,
            Date = DateTime.Now
        });
        context.Tenants.Add(new Tenant { TenantId = 1, FullName = "Test Tenant", Contact = "0123456789", RoomId = 1, UserId = 1, EmergencyContactName = "EC", EmergencyContactNumber = "0123456789" });
        context.SaveChanges();

        var controller = GetController(context, mapper);

        var result = await controller.Index();

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.IsAssignableFrom<System.Collections.IEnumerable>(viewResult.Model);
    }

    [Fact]
    public async Task Create_ValidModel_CreatesPaymentAndRedirects()
    {
        var context = GetDbContext();
        var mapper = GetMapper();
        context.Tenants.Add(new Tenant { TenantId = 1, FullName = "Test Tenant", Contact = "0123456789", RoomId = 1, UserId = 1, EmergencyContactName = "EC", EmergencyContactNumber = "0123456789" });
        context.SaveChanges();

        var controller = GetController(context, mapper);
        var payment = new PaymentViewModel
        {
            TenantId = 1,
            Amount = 1200,
            Type = "Rent",
            PaymentMonth = 2,
            PaymentYear = 2024,
            Date = DateTime.Now
        };

        var result = await controller.Create(payment);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
        Assert.Single(context.Payments);
        Assert.Equal("Payment recorded successfully.", controller.TempData["Success"]);
    }

    [Fact]
    public async Task Edit_ValidModel_UpdatesPaymentAndRedirects()
    {
        var context = GetDbContext();
        var mapper = GetMapper();
        context.Payments.Add(new Payment
        {
            PaymentId = 1,
            TenantId = 1,
            Amount = 1000,
            Type = "Rent",
            PaymentMonth = 1,
            PaymentYear = 2024,
            Date = DateTime.Now
        });
        context.SaveChanges();

        var controller = GetController(context, mapper);
        var updatedPayment = new PaymentViewModel
        {
            PaymentId = 1,
            Amount = 1500,
            Type = "Deposit",
            PaymentMonth = 3,
            PaymentYear = 2024
        };

        var result = await controller.Edit(updatedPayment);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
        var payment = context.Payments.First();
        Assert.Equal(1500, payment.Amount);
        Assert.Equal("Deposit", payment.Type);
        Assert.Equal("Payment updated successfully.", controller.TempData["Success"]);
    }

    [Fact]
    public async Task Delete_DeletesPaymentAndRedirects()
    {
        var context = GetDbContext();
        var mapper = GetMapper();
        context.Payments.Add(new Payment
        {
            PaymentId = 1,
            TenantId = 1,
            Amount = 1000,
            Type = "Rent",
            PaymentMonth = 1,
            PaymentYear = 2024,
            Date = DateTime.Now
        });
        context.SaveChanges();

        var controller = GetController(context, mapper);

        var result = await controller.Delete(1);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
        Assert.Empty(context.Payments);
        Assert.Equal("Payment deleted successfully.", controller.TempData["Success"]);
    }

    [Fact]
    public void Receipt_ValidId_ReturnsPartialViewWithPaymentViewModel()
    {
        var context = GetDbContext();
        var mapper = GetMapper();

        // Create Room
        var room = new Room { RoomId = 1, Number = "101", Type = "Single", Status = "Available" };
        context.Rooms.Add(room);

        // Create Tenant with Room
        var tenant = new Tenant
        {
            TenantId = 1,
            FullName = "Test Tenant",
            Contact = "0123456789",
            RoomId = 1,
            Room = room,
            UserId = 1,
            EmergencyContactName = "EC",
            EmergencyContactNumber = "0123456789"
        };
        context.Tenants.Add(tenant);

        // Create Payment with Tenant
        var payment = new Payment
        {
            PaymentId = 1,
            TenantId = 1,
            Tenant = tenant,
            Amount = 1000,
            Type = "Rent",
            PaymentMonth = 1,
            PaymentYear = 2024,
            Date = DateTime.Now
        };
        context.Payments.Add(payment);

        context.SaveChanges();

        var controller = GetController(context, mapper);

        var result = controller.Receipt(1);

        var partial = Assert.IsType<PartialViewResult>(result);
        Assert.Equal("_PaymentReceipt", partial.ViewName);
        Assert.IsType<PaymentViewModel>(partial.Model);
    }

    [Fact]
    public async Task ReceiptPartial_ValidId_ReturnsPartialViewWithPaymentViewModel()
    {
        var context = GetDbContext();
        var mapper = GetMapper();

        // Create Room
        var room = new Room { RoomId = 1, Number = "101", Type = "Single", Status = "Available" };
        context.Rooms.Add(room);

        // Create Tenant with Room
        var tenant = new Tenant
        {
            TenantId = 1,
            FullName = "Test Tenant",
            Contact = "0123456789",
            RoomId = 1,
            Room = room,
            UserId = 1,
            EmergencyContactName = "EC",
            EmergencyContactNumber = "0123456789"
        };
        context.Tenants.Add(tenant);

        // Create Payment with Tenant
        var payment = new Payment
        {
            PaymentId = 1,
            TenantId = 1,
            Tenant = tenant,
            Amount = 1000,
            Type = "Rent",
            PaymentMonth = 1,
            PaymentYear = 2024,
            Date = DateTime.Now
        };
        context.Payments.Add(payment);

        context.SaveChanges();

        var controller = GetController(context, mapper);

        var result = await controller.ReceiptPartial(1);

        var partial = Assert.IsType<PartialViewResult>(result);
        Assert.Equal("_ReceiptPartial", partial.ViewName);
        Assert.IsType<PaymentViewModel>(partial.Model);
    }
}