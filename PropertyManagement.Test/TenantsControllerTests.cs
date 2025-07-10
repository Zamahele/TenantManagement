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
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace PropertyManagement.Test;

public class TenantsControllerTests
{
    private ApplicationDbContext GetDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    private TenantsController GetController(ApplicationDbContext context, ClaimsPrincipal user)
    {
        var controller = new TenantsController(context);
        var tempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>());
        controller.TempData = tempData;
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };
        // For model validation
        var objectValidator = new Mock<IObjectModelValidator>();
        objectValidator.Setup(o => o.Validate(
            It.IsAny<ActionContext>(),
            It.IsAny<ValidationStateDictionary>(),
            It.IsAny<string>(),
            It.IsAny<object>()));
        controller.ObjectValidator = objectValidator.Object;
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
    public async Task Index_Manager_ReturnsViewWithTenants()
    {
        var context = GetDbContext();
        context.Rooms.Add(new Room { RoomId = 1, Number = "101", Type = "Single", Status = "Available" });
        context.Tenants.Add(new Tenant { TenantId = 1, RoomId = 1, FullName = "Tenant", Contact = "123", EmergencyContactName = "EC", EmergencyContactNumber = "123" });
        context.SaveChanges();

        var controller = GetController(context, GetUser("Manager"));

        var result = await controller.Index();

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.IsAssignableFrom<IEnumerable<Tenant>>(viewResult.Model);
    }

    [Fact]
    public async Task Profile_Tenant_ReturnsViewWithProfile()
    {
        var context = GetDbContext();
        var user = new User { UserId = 2, Username = "tenant", PasswordHash = "hash", Role = "Tenant" };
        context.Users.Add(user);
        context.Rooms.Add(new Room { RoomId = 1, Number = "101", Type = "Single", Status = "Available" });
        context.Tenants.Add(new Tenant { TenantId = 1, UserId = 2, RoomId = 1, FullName = "Tenant", Contact = "123", EmergencyContactName = "EC", EmergencyContactNumber = "123" });
        context.SaveChanges();

        var controller = GetController(context, GetUser("Tenant", 2));

        var result = await controller.Profile(null);

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Equal("Profile", viewResult.ViewName);
        Assert.IsType<Tenant>(viewResult.Model);
    }

    [Fact]
    public async Task EditProfile_Manager_ReturnsPartialView()
    {
        var context = GetDbContext();
        context.Tenants.Add(new Tenant { TenantId = 1, UserId = 2, RoomId = 1, FullName = "Tenant", Contact = "123", EmergencyContactName = "EC", EmergencyContactNumber = "123" });
        context.SaveChanges();

        var controller = GetController(context, GetUser("Manager"));

        var result = await controller.EditProfile(1);

        var partial = Assert.IsType<PartialViewResult>(result);
        Assert.Equal("_EditProfileModal", partial.ViewName);
        Assert.IsType<Tenant>(partial.Model);
    }

    [Fact]
    public async Task EditProfile_Post_Manager_UpdatesProfileAndRedirects()
    {
        var context = GetDbContext();
        context.Users.Add(new User { UserId = 2, Username = "tenant", PasswordHash = "hash", Role = "Tenant" });
        context.Tenants.Add(new Tenant { TenantId = 1, UserId = 2, RoomId = 1, FullName = "Tenant", Contact = "123", EmergencyContactName = "EC", EmergencyContactNumber = "123" });
        context.SaveChanges();

        var controller = GetController(context, GetUser("Manager"));

        var updatedTenant = new Tenant
        {
            TenantId = 1,
            FullName = "Updated Name",
            Contact = "456",
            EmergencyContactName = "NewEC",
            EmergencyContactNumber = "456"
        };

        var result = await controller.EditProfile(updatedTenant);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Profile", redirect.ActionName);
        Assert.Equal("Profile updated successfully.", controller.TempData["Success"]);
        Assert.Equal("Updated Name", context.Tenants.First().FullName);
    }

    [Fact]
    public async Task TenantForm_New_ReturnsPartialView()
    {
        var context = GetDbContext();
        context.SaveChanges();

        var controller = GetController(context, GetUser("Manager"));

        var result = await controller.TenantForm(null);

        var partial = Assert.IsType<PartialViewResult>(result);
        Assert.Equal("_TenantForm", partial.ViewName);
        Assert.IsType<Tenant>(partial.Model);
    }

    [Fact]
    public async Task CreateOrEdit_Manager_CreatesTenantAndRedirects()
    {
        var context = GetDbContext();
        context.Rooms.Add(new Room { RoomId = 1, Number = "101", Type = "Single", Status = "Available" });
        context.SaveChanges();

        var controller = GetController(context, GetUser("Manager"));

        var tenant = new Tenant
        {
            FullName = "New Tenant",
            Contact = "123",
            RoomId = 1,
            EmergencyContactName = "EC",
            EmergencyContactNumber = "123"
        };

        var result = await controller.CreateOrEdit(tenant, "newuser", "password");

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
        Assert.Single(context.Tenants);
        Assert.Equal("Tenant created successfully.", controller.TempData["Success"]);
    }

    [Fact]
    public async Task GetTenant_Manager_ReturnsJson()
    {
        var context = GetDbContext();
        context.Rooms.Add(new Room { RoomId = 1, Number = "101", Type = "Single", Status = "Available" });
        context.Tenants.Add(new Tenant { TenantId = 1, RoomId = 1, FullName = "Tenant", Contact = "123", EmergencyContactName = "EC", EmergencyContactNumber = "123" });
        context.SaveChanges();

        var controller = GetController(context, GetUser("Manager"));

        var result = await controller.GetTenant(1);

        var json = Assert.IsType<JsonResult>(result);
        Assert.IsType<Tenant>(json.Value);
    }

    [Fact]
    public async Task Delete_Manager_DeletesTenantAndRedirects()
    {
        var context = GetDbContext();
        context.Tenants.Add(new Tenant { TenantId = 1, RoomId = 1, FullName = "Tenant", Contact = "123", EmergencyContactName = "EC", EmergencyContactNumber = "123" });
        context.SaveChanges();

        var controller = GetController(context, GetUser("Manager"));

        var result = await controller.Delete(1);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
        Assert.Empty(context.Tenants);
        Assert.Equal("Tenant deleted successfully.", controller.TempData["Success"]);
    }
}