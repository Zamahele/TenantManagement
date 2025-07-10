using Xunit;
using Microsoft.EntityFrameworkCore;
using PropertyManagement.Web.Controllers;
using PropertyManagement.Infrastructure.Data;
using PropertyManagement.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Http;
using Moq;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System;
using Assert = Xunit.Assert;

namespace PropertyManagement.Test;

public class MaintenanceControllerTests
{
    private ApplicationDbContext GetDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    private MaintenanceController GetController(ApplicationDbContext context, ClaimsPrincipal user)
    {
        var controller = new MaintenanceController(context);
        var tempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>());
        controller.TempData = tempData;
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };
        return controller;
    }

    private ClaimsPrincipal GetUser(string role, int userId = 1)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Role, role)
        };
        var identity = new ClaimsIdentity(claims, "TestAuthType");
        return new ClaimsPrincipal(identity);
    }

    [Fact]
    public async Task Index_Manager_ReturnsViewWithRequests()
    {
        var context = GetDbContext();
        context.Rooms.Add(new Room { RoomId = 1, Number = "101", Type = "Single", Status = "Available" });
        context.MaintenanceRequests.Add(new MaintenanceRequest { MaintenanceRequestId = 1, RoomId = 1, TenantId = "1", Description = "Fix", RequestDate = DateTime.Now, Status = "Pending" });
        context.SaveChanges();

        var controller = GetController(context, GetUser("Manager"));

        var result = await controller.Index();

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.IsAssignableFrom<IEnumerable<MaintenanceRequest>>(viewResult.Model);
    }

    [Fact]
    public async Task Index_Tenant_ReturnsViewWithTenantRequests()
    {
        var context = GetDbContext();
        context.Rooms.Add(new Room { RoomId = 1, Number = "101", Type = "Single", Status = "Available" });
        context.Tenants.Add(new Tenant { TenantId = 1, UserId = 2, RoomId = 1, FullName = "Tenant", Contact = "123", EmergencyContactName = "EC", EmergencyContactNumber = "123" });
        context.MaintenanceRequests.Add(new MaintenanceRequest { MaintenanceRequestId = 1, RoomId = 1, TenantId = "1", Description = "Fix", RequestDate = DateTime.Now, Status = "Pending" });
        context.SaveChanges();

        var controller = GetController(context, GetUser("Tenant", 2));

        var result = await controller.Index();

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.IsAssignableFrom<IEnumerable<MaintenanceRequest>>(viewResult.Model);
    }

    [Fact]
    public async Task TenantCreate_Get_ReturnsViewWithModel()
    {
        var context = GetDbContext();
        context.Rooms.Add(new Room { RoomId = 1, Number = "101", Type = "Single", Status = "Available" });
        context.Tenants.Add(new Tenant { TenantId = 1, UserId = 2, RoomId = 1, FullName = "Tenant", Contact = "123", EmergencyContactName = "EC", EmergencyContactNumber = "123" });
        context.SaveChanges();

        var controller = GetController(context, GetUser("Tenant", 2));

        var result = await controller.TenantCreate();

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.IsType<MaintenanceRequest>(viewResult.Model);
    }

    [Fact]
    public async Task TenantCreate_Post_ValidModel_CreatesRequestAndRedirects()
    {
        var context = GetDbContext();
        context.Rooms.Add(new Room { RoomId = 1, Number = "101", Type = "Single", Status = "Available" });
        context.Tenants.Add(new Tenant { TenantId = 1, UserId = 2, RoomId = 1, FullName = "Tenant", Contact = "123", EmergencyContactName = "EC", EmergencyContactNumber = "123" });
        context.SaveChanges();

        var controller = GetController(context, GetUser("Tenant", 2));
        var model = new MaintenanceRequest { Description = "Leaking tap" };

        var result = await controller.TenantCreate(model);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
        Assert.Single(context.MaintenanceRequests);
        Assert.Contains("Reference Number", controller.TempData["Success"].ToString());
    }

    [Fact]
    public async Task Create_Manager_ReturnsView()
    {
        var context = GetDbContext();
        context.Rooms.Add(new Room { RoomId = 1, Number = "101", Type = "Single", Status = "Available" });
        context.SaveChanges();

        var controller = GetController(context, GetUser("Manager"));

        var result = await controller.Create();

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.True(controller.ViewBag.Rooms.Count > 0);
    }

    [Fact]
    public async Task RequestModal_Manager_ReturnsPartialView()
    {
        var context = GetDbContext();
        context.Rooms.Add(new Room { RoomId = 1, Number = "101", Type = "Single", Status = "Available" });
        context.MaintenanceRequests.Add(new MaintenanceRequest { MaintenanceRequestId = 1, RoomId = 1, TenantId = "1", Description = "Fix", RequestDate = DateTime.Now, Status = "Pending" });
        context.SaveChanges();

        var controller = GetController(context, GetUser("Manager"));

        var result = await controller.RequestModal(1);

        var partial = Assert.IsType<PartialViewResult>(result);
        Assert.Equal("_RequestModal", partial.ViewName);
        Assert.IsType<MaintenanceRequest>(partial.Model);
    }

    [Fact]
    public async Task CreateOrEdit_Manager_CreatesRequestAndRedirects()
    {
        var context = GetDbContext();
        context.Rooms.Add(new Room { RoomId = 1, Number = "101", Type = "Single", Status = "Available" });
        context.Tenants.Add(new Tenant { TenantId = 1, RoomId = 1, FullName = "Tenant", Contact = "123", EmergencyContactName = "EC", EmergencyContactNumber = "123" });
        context.SaveChanges();

        var controller = GetController(context, GetUser("Manager"));
        var request = new MaintenanceRequest
        {
            RoomId = 1,
            Description = "Broken window",
            Status = "Pending", // <-- Required property
            AssignedTo = "Manager" // Optional, but matches controller logic
        };

        var result = await controller.CreateOrEdit(request);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
        Assert.Single(context.MaintenanceRequests);
        Assert.Equal("Maintenance request created successfully.", controller.TempData["Success"]);
    }

    [Fact]
    public async Task CreateOrEdit_Manager_UpdatesRequestAndRedirects()
    {
        var context = GetDbContext();
        context.Rooms.Add(new Room { RoomId = 1, Number = "101", Type = "Single", Status = "Available" });
        context.Tenants.Add(new Tenant { TenantId = 1, RoomId = 1, FullName = "Tenant", Contact = "123", EmergencyContactName = "EC", EmergencyContactNumber = "123" });
        context.MaintenanceRequests.Add(new MaintenanceRequest { MaintenanceRequestId = 1, RoomId = 1, TenantId = "1", Description = "Old desc", RequestDate = DateTime.Now, Status = "Pending" });
        context.SaveChanges();

        var controller = GetController(context, GetUser("Manager"));
        var request = new MaintenanceRequest { MaintenanceRequestId = 1, RoomId = 1, TenantId = "1", Description = "Updated desc", Status = "In Progress", AssignedTo = "Manager" };

        var result = await controller.CreateOrEdit(request);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
        Assert.Equal("Maintenance request updated successfully.", controller.TempData["Success"]);
        Assert.Equal("Updated desc", context.MaintenanceRequests.First().Description);
    }

    [Fact]
    public async Task Complete_Manager_MarksRequestCompletedAndRedirects()
    {
        var context = GetDbContext();
        context.MaintenanceRequests.Add(new MaintenanceRequest { MaintenanceRequestId = 1, RoomId = 1, TenantId = "1", Description = "Fix", RequestDate = DateTime.Now, Status = "Pending" });
        context.SaveChanges();

        var controller = GetController(context, GetUser("Manager"));

        var result = await controller.Complete(1);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
        Assert.Equal("Completed", context.MaintenanceRequests.First().Status);
        Assert.Equal("Maintenance request marked as completed.", controller.TempData["Success"]);
    }

    [Fact]
    public async Task GetRequest_Manager_ReturnsJson()
    {
        var context = GetDbContext();
        context.MaintenanceRequests.Add(new MaintenanceRequest { MaintenanceRequestId = 1, RoomId = 1, TenantId = "1", Description = "Fix", RequestDate = DateTime.Now, Status = "Pending" });
        context.SaveChanges();

        var controller = GetController(context, GetUser("Manager"));

        var result = await controller.GetRequest(1);

        var json = Assert.IsType<JsonResult>(result);
        Assert.IsType<MaintenanceRequest>(json.Value);
    }

    [Fact]
    public async Task Delete_Manager_DeletesRequestAndRedirects()
    {
        var context = GetDbContext();
        context.MaintenanceRequests.Add(new MaintenanceRequest { MaintenanceRequestId = 1, RoomId = 1, TenantId = "1", Description = "Fix", RequestDate = DateTime.Now, Status = "Pending" });
        context.SaveChanges();

        var controller = GetController(context, GetUser("Manager"));

        var result = await controller.Delete(1);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
        Assert.Empty(context.MaintenanceRequests);
        Assert.Equal("Maintenance request deleted successfully.", controller.TempData["Success"]);
    }

    [Fact]
    public async Task History_Manager_ReturnsViewWithHistory()
    {
        var context = GetDbContext();
        context.Rooms.Add(new Room { RoomId = 1, Number = "101", Type = "Single", Status = "Available" });
        context.MaintenanceRequests.Add(new MaintenanceRequest { MaintenanceRequestId = 1, RoomId = 1, TenantId = "1", Description = "Fix", RequestDate = DateTime.Now, Status = "Completed" });
        context.SaveChanges();

        var controller = GetController(context, GetUser("Manager"));

        var result = await controller.History(1);

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.IsAssignableFrom<IEnumerable<MaintenanceRequest>>(viewResult.Model);
    }
}