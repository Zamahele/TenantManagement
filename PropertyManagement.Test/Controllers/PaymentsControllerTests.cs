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
using Assert = Xunit.Assert;

namespace PropertyManagement.Test.Controllers;

public class PaymentsControllerTests : BaseControllerTest
{
    private PaymentsController GetController(
        Mock<IPaymentApplicationService> mockPaymentService,
        Mock<ITenantApplicationService> mockTenantService,
        Mock<ILeaseAgreementApplicationService> mockLeaseService = null)
    {
        mockLeaseService ??= new Mock<ILeaseAgreementApplicationService>();

        var controller = new PaymentsController(
            mockPaymentService.Object,
            mockTenantService.Object,
            mockLeaseService.Object,
            Mapper);

        SetupControllerContext(controller, GetManagerUser());
        return controller;
    }

    [Fact]
    public async Task Index_ReturnsViewWithPayments()
    {
        // Arrange
        var mockPaymentService = new Mock<IPaymentApplicationService>();
        var mockTenantService = new Mock<ITenantApplicationService>();
        var mockLeaseService = new Mock<ILeaseAgreementApplicationService>();

        var payments = new List<PaymentDto>
        {
            new PaymentDto
            {
                PaymentId = 1,
                TenantId = 1,
                Amount = 1000,
                Type = "Rent",
                PaymentMonth = 1,
                PaymentYear = 2024,
                PaymentDate = DateTime.Now
            }
        };

        var tenants = new List<TenantDto>
        {
            new TenantDto { TenantId = 1, FullName = "Test Tenant", Contact = "0123456789", RoomId = 1, UserId = 1 }
        };

        mockPaymentService.Setup(s => s.GetAllPaymentsAsync())
            .ReturnsAsync(ServiceResult<IEnumerable<PaymentDto>>.Success(payments));
        mockTenantService.Setup(s => s.GetAllTenantsAsync())
            .ReturnsAsync(ServiceResult<IEnumerable<TenantDto>>.Success(tenants));
        mockLeaseService.Setup(s => s.GetAllLeaseAgreementsAsync())
            .ReturnsAsync(ServiceResult<IEnumerable<LeaseAgreementDto>>.Success(new List<LeaseAgreementDto>()));

        var controller = GetController(mockPaymentService, mockTenantService, mockLeaseService);

        // Act
        var result = await controller.Index();

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.IsAssignableFrom<System.Collections.IEnumerable>(viewResult.Model);
    }

    [Fact]
    public async Task Create_ValidModel_CreatesPaymentAndRedirects()
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

        var controller = GetController(mockPaymentService, mockTenantService);
        var payment = new PaymentViewModel
        {
            TenantId = 1,
            Amount = 1200,
            Type = "Rent",
            PaymentMonth = 2,
            PaymentYear = 2024,
            Date = DateTime.Now
        };

        // Act
        var result = await controller.Create(payment);

        // Assert
        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
        Assert.Equal("Payment recorded successfully.", controller.TempData["Success"]);
        mockPaymentService.Verify(s => s.CreatePaymentAsync(It.IsAny<CreatePaymentDto>()), Times.Once);
    }

    [Fact]
    public async Task Edit_ValidModel_UpdatesPaymentAndRedirects()
    {
        // Arrange
        var mockPaymentService = new Mock<IPaymentApplicationService>();
        var mockTenantService = new Mock<ITenantApplicationService>();

        var updatedPayment = new PaymentDto
        {
            PaymentId = 1,
            TenantId = 1,
            Amount = 1500,
            Type = "Deposit",
            PaymentMonth = 3,
            PaymentYear = 2024,
            PaymentDate = DateTime.Now
        };

        mockPaymentService.Setup(s => s.UpdatePaymentAsync(It.IsAny<int>(), It.IsAny<UpdatePaymentDto>()))
            .ReturnsAsync(ServiceResult<PaymentDto>.Success(updatedPayment));

        var controller = GetController(mockPaymentService, mockTenantService);
        var paymentViewModel = new PaymentViewModel
        {
            PaymentId = 1,
            Amount = 1500,
            Type = "Deposit",
            PaymentMonth = 3,
            PaymentYear = 2024,
            Date = DateTime.Now
        };

        // Act
        var result = await controller.Edit(paymentViewModel);

        // Assert
        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
        Assert.Equal("Payment updated successfully.", controller.TempData["Success"]);
        mockPaymentService.Verify(s => s.UpdatePaymentAsync(1, It.IsAny<UpdatePaymentDto>()), Times.Once);
    }

    [Fact]
    public async Task Delete_DeletesPaymentAndRedirects()
    {
        // Arrange
        var mockPaymentService = new Mock<IPaymentApplicationService>();
        var mockTenantService = new Mock<ITenantApplicationService>();

        mockPaymentService.Setup(s => s.DeletePaymentAsync(It.IsAny<int>()))
            .ReturnsAsync(ServiceResult<bool>.Success(true));

        var controller = GetController(mockPaymentService, mockTenantService);

        // Act
        var result = await controller.Delete(1);

        // Assert
        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
        Assert.Equal("Payment deleted successfully.", controller.TempData["Success"]);
        mockPaymentService.Verify(s => s.DeletePaymentAsync(1), Times.Once);
    }

    [Fact]
    public async Task Receipt_ValidId_ReturnsPartialViewWithPaymentViewModel()
    {
        // Arrange
        var mockPaymentService = new Mock<IPaymentApplicationService>();
        var mockTenantService = new Mock<ITenantApplicationService>();

        var payment = new PaymentDto
        {
            PaymentId = 1,
            TenantId = 1,
            Amount = 1000,
            Type = "Rent",
            PaymentMonth = 1,
            PaymentYear = 2024,
            PaymentDate = DateTime.Now,
            Tenant = new TenantDto
            {
                TenantId = 1,
                FullName = "Test Tenant",
                Contact = "0123456789",
                RoomId = 1,
                UserId = 1,
                Room = new RoomDto { RoomId = 1, Number = "101", Type = "Single", Status = "Available" }
            }
        };

        mockPaymentService.Setup(s => s.GetPaymentByIdAsync(It.IsAny<int>()))
            .ReturnsAsync(ServiceResult<PaymentDto>.Success(payment));

        var controller = GetController(mockPaymentService, mockTenantService);

        // Act
        var result = await controller.Receipt(1);

        // Assert
        var partial = Assert.IsType<PartialViewResult>(result);
        Assert.Equal("_PaymentReceipt", partial.ViewName);
        Assert.IsType<PaymentViewModel>(partial.Model);
        mockPaymentService.Verify(s => s.GetPaymentByIdAsync(1), Times.Once);
    }

    [Fact]
    public async Task ReceiptPartial_ValidId_ReturnsPartialViewWithPaymentViewModel()
    {
        // Arrange
        var mockPaymentService = new Mock<IPaymentApplicationService>();
        var mockTenantService = new Mock<ITenantApplicationService>();

        var payment = new PaymentDto
        {
            PaymentId = 1,
            TenantId = 1,
            Amount = 1000,
            Type = "Rent",
            PaymentMonth = 1,
            PaymentYear = 2024,
            PaymentDate = DateTime.Now,
            Tenant = new TenantDto
            {
                TenantId = 1,
                FullName = "Test Tenant",
                Contact = "0123456789",
                RoomId = 1,
                UserId = 1,
                Room = new RoomDto { RoomId = 1, Number = "101", Type = "Single", Status = "Available" }
            }
        };

        mockPaymentService.Setup(s => s.GetPaymentByIdAsync(It.IsAny<int>()))
            .ReturnsAsync(ServiceResult<PaymentDto>.Success(payment));

        var controller = GetController(mockPaymentService, mockTenantService);

        // Act
        var result = await controller.ReceiptPartial(1);

        // Assert
        var partial = Assert.IsType<PartialViewResult>(result);
        Assert.Equal("_ReceiptPartial", partial.ViewName);
        Assert.IsType<PaymentViewModel>(partial.Model);
        mockPaymentService.Verify(s => s.GetPaymentByIdAsync(1), Times.Once);
    }

    [Fact]
    public async Task Create_InvalidModel_ReturnsViewWithErrors()
    {
        // Arrange
        var mockPaymentService = new Mock<IPaymentApplicationService>();
        var mockTenantService = new Mock<ITenantApplicationService>();

        // Setup to return empty lists for the error view
        mockPaymentService.Setup(s => s.GetAllPaymentsAsync())
            .ReturnsAsync(ServiceResult<IEnumerable<PaymentDto>>.Success(new List<PaymentDto>()));
        mockTenantService.Setup(s => s.GetAllTenantsAsync())
            .ReturnsAsync(ServiceResult<IEnumerable<TenantDto>>.Success(new List<TenantDto>()));

        var controller = GetController(mockPaymentService, mockTenantService);
        
        // Add a model error to simulate invalid model state
        controller.ModelState.AddModelError("Amount", "Amount is required");

        var payment = new PaymentViewModel
        {
            TenantId = 1,
            Type = "Rent",
            PaymentMonth = 2,
            PaymentYear = 2024
            // Missing Amount to make model invalid
        };

        // Act
        var result = await controller.Create(payment);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Equal("Index", viewResult.ViewName);
        Assert.Equal("Please correct the errors in the form.", controller.TempData["Error"]);
        
        // Verify CreatePaymentAsync was never called due to invalid model
        mockPaymentService.Verify(s => s.CreatePaymentAsync(It.IsAny<CreatePaymentDto>()), Times.Never);
    }

    [Fact]
    public async Task Delete_ServiceFailure_ReturnsNotFound()
    {
        // Arrange
        var mockPaymentService = new Mock<IPaymentApplicationService>();
        var mockTenantService = new Mock<ITenantApplicationService>();

        mockPaymentService.Setup(s => s.DeletePaymentAsync(It.IsAny<int>()))
            .ReturnsAsync(ServiceResult<bool>.Failure("Payment not found"));

        var controller = GetController(mockPaymentService, mockTenantService);

        // Act
        var result = await controller.Delete(1);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundResult>(result);
        Assert.Equal("Payment not found", controller.TempData["Error"]);
        mockPaymentService.Verify(s => s.DeletePaymentAsync(1), Times.Once);
    }
}