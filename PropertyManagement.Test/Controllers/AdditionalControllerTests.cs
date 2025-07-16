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
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Xunit;

namespace PropertyManagement.Test.Controllers
{
    public class AdditionalControllerTests : TestBaseClass
    {
        [Fact]
        public async Task TenantsController_CreateOrEdit_UpdateExisting_UpdatesSuccessfully()
        {
            var context = GetDbContext();
            var mapper = GetMapper();
            SeedTestData(context);

            var tenantRepo = GetRepositoryMock<Tenant>(context);
            var userRepo = GetRepositoryMock<User>(context);
            var roomRepo = GetRepositoryMock<Room>(context);

            var controller = new TenantsController(tenantRepo.Object, userRepo.Object, roomRepo.Object, mapper);
            SetupControllerContext(controller, GetUser("Manager"));

            var existingTenant = context.Tenants.First();
            var updatedTenant = new TenantViewModel
            {
                TenantId = existingTenant.TenantId,
                UserId = existingTenant.UserId,
                RoomId = existingTenant.RoomId,
                FullName = "Updated Name",
                Contact = "9999999999",
                EmergencyContactName = "Updated Emergency",
                EmergencyContactNumber = "8888888888",
                User = new UserViewModel { UserId = existingTenant.UserId, Username = "testuser", Role = "Tenant" }
            };

            var result = await controller.CreateOrEdit(updatedTenant, "uniqueuser", "validpassword123");

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirect.ActionName);
            Assert.Equal("Updated Name", existingTenant.FullName);
        }

        [Fact]
        public async Task RoomsController_CreateOrEdit_ValidModel_CreatesRoomSuccessfully()
        {
            var context = GetDbContext();
            var mapper = GetMapper();

            var roomRepo = GetRepositoryMock<Room>(context);
            var bookingRepo = GetRepositoryMock<BookingRequest>(context);
            var tenantRepo = GetRepositoryMock<Tenant>(context);

            var controller = new RoomsController(roomRepo.Object, bookingRepo.Object, tenantRepo.Object, context, mapper);
            SetupControllerContext(controller, GetUser("Manager"));

            var roomModel = new RoomFormViewModel
            {
                Number = "201",
                Type = "Single",
                Status = "Available"
            };

            var result = await controller.CreateOrEdit(roomModel);

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirect.ActionName);
        }

        [Fact]
        public async Task TenantsController_GetTenant_NonExistingTenant_ReturnsNotFound()
        {
            var context = GetDbContext();
            var mapper = GetMapper();

            var tenantRepo = GetRepositoryMock<Tenant>(context);
            var userRepo = GetRepositoryMock<User>(context);
            var roomRepo = GetRepositoryMock<Room>(context);

            var controller = new TenantsController(tenantRepo.Object, userRepo.Object, roomRepo.Object, mapper);
            SetupControllerContext(controller, GetUser("Manager"));

            var result = await controller.GetTenant(999);

            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task TenantsController_EditProfile_NonExistingTenant_ReturnsRedirect()
        {
            var context = GetDbContext();
            var mapper = GetMapper();

            var tenantRepo = GetRepositoryMock<Tenant>(context);
            var userRepo = GetRepositoryMock<User>(context);
            var roomRepo = GetRepositoryMock<Room>(context);

            var controller = new TenantsController(tenantRepo.Object, userRepo.Object, roomRepo.Object, mapper);
            SetupControllerContext(controller, GetUser("Manager"));

            var result = await controller.EditProfile(999);

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Profile", redirect.ActionName);
        }

        [Fact]
        public async Task TenantsController_Delete_NonExistingTenant_ReturnsRedirect()
        {
            var context = GetDbContext();
            var mapper = GetMapper();

            var tenantRepo = GetRepositoryMock<Tenant>(context);
            var userRepo = GetRepositoryMock<User>(context);
            var roomRepo = GetRepositoryMock<Room>(context);

            var controller = new TenantsController(tenantRepo.Object, userRepo.Object, roomRepo.Object, mapper);
            SetupControllerContext(controller, GetUser("Manager"));

            var result = await controller.Delete(999);

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirect.ActionName);
        }

        [Fact]
        public async Task PaymentsController_Delete_NonExistingPayment_ReturnsNotFound()
        {
            var context = GetDbContext();
            var mapper = GetMapper();

            var paymentRepo = GetRepositoryMock<Payment>(context);
            var tenantRepo = GetRepositoryMock<Tenant>(context);
            var leaseRepo = GetRepositoryMock<LeaseAgreement>(context);

            var controller = new PaymentsController(paymentRepo.Object, tenantRepo.Object, leaseRepo.Object, mapper);
            SetupControllerContext(controller, GetUser("Manager"));

            var result = await controller.Delete(999);

            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task PaymentsController_Edit_NonExistingPayment_ReturnsNotFound()
        {
            var context = GetDbContext();
            var mapper = GetMapper();

            var paymentRepo = GetRepositoryMock<Payment>(context);
            var tenantRepo = GetRepositoryMock<Tenant>(context);
            var leaseRepo = GetRepositoryMock<LeaseAgreement>(context);

            var controller = new PaymentsController(paymentRepo.Object, tenantRepo.Object, leaseRepo.Object, mapper);
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

            var result = await controller.Edit(paymentViewModel);

            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task HomeController_Index_WithEmptyDatabase_ReturnsViewWithZeroStats()
        {
            var context = GetDbContext();
            var mapper = GetMapper();

            var roomRepo = GetRepositoryMock<Room>(context);
            var tenantRepo = GetRepositoryMock<Tenant>(context);
            var leaseRepo = GetRepositoryMock<LeaseAgreement>(context);
            var maintenanceRepo = GetRepositoryMock<MaintenanceRequest>(context);

            var logger = new Mock<Microsoft.Extensions.Logging.ILogger<HomeController>>();
            var controller = new HomeController(roomRepo.Object, tenantRepo.Object, leaseRepo.Object, maintenanceRepo.Object, logger.Object, mapper);

            var result = await controller.Index();

            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<DashboardViewModel>(viewResult.Model);

            Assert.Equal(0, model.TotalRooms);
            Assert.Equal(0, model.TotalTenants);
            Assert.Equal(0, model.ActiveLeases);
            Assert.Equal(0, model.PendingRequests);
        }

        [Fact]
        public async Task PaymentsController_Create_ValidModel_CreatesPaymentSuccessfully()
        {
            var context = GetDbContext();
            var mapper = GetMapper();
            SeedTestData(context);

            var paymentRepo = GetRepositoryMock<Payment>(context);
            var tenantRepo = GetRepositoryMock<Tenant>(context);
            var leaseRepo = GetRepositoryMock<LeaseAgreement>(context);

            var controller = new PaymentsController(paymentRepo.Object, tenantRepo.Object, leaseRepo.Object, mapper);
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

            var result = await controller.Create(paymentModel);

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirect.ActionName);
        }
    }
}