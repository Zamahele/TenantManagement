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

namespace PropertyManagement.Test.Controllers;

public class MaintenanceControllerTests
{
  private ApplicationDbContext GetDbContext()
  {
    var options = new DbContextOptionsBuilder<ApplicationDbContext>()
        .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
        .Options;
    return new ApplicationDbContext(options);
  }

  private IMapper GetMapper()
  {
    var expr = new MapperConfigurationExpression();
    expr.CreateMap<MaintenanceRequest, MaintenanceRequestViewModel>()
      .ForMember(dest => dest.Room, opt => opt.MapFrom(src => src.Room))
      .ReverseMap();
    expr.CreateMap<Room, RoomViewModel>().ReverseMap();
    expr.CreateMap<Tenant, TenantViewModel>().ReverseMap();
    expr.CreateMap<MaintenanceRequestViewModel, MaintenanceRequest>()
      .ForMember(dest => dest.Room, opt => opt.Ignore());
    
    var config = new MapperConfiguration(expr, NullLoggerFactory.Instance);
    return config.CreateMapper();
  }

  private MaintenanceController CreateController(ApplicationDbContext context, ClaimsPrincipal user)
  {
    var mapper = GetMapper();
    
    var maintenanceRepo = new Mock<IGenericRepository<MaintenanceRequest>>();
    var roomRepo = new Mock<IGenericRepository<Room>>();
    var tenantRepo = new Mock<IGenericRepository<Tenant>>();
    
    maintenanceRepo.Setup(r => r.Query()).Returns(context.MaintenanceRequests);
    maintenanceRepo.Setup(r => r.GetByIdAsync(It.IsAny<int>()))
        .ReturnsAsync((int id) => context.MaintenanceRequests.Find(id));
    maintenanceRepo.Setup(r => r.AddAsync(It.IsAny<MaintenanceRequest>()))
        .Callback((MaintenanceRequest req) => { 
          req.MaintenanceRequestId = context.MaintenanceRequests.Count() + 1;
          context.MaintenanceRequests.Add(req); 
          context.SaveChanges(); 
        })
        .Returns(Task.CompletedTask);
    maintenanceRepo.Setup(r => r.UpdateAsync(It.IsAny<MaintenanceRequest>()))
        .Callback((MaintenanceRequest req) => { 
          var existing = context.MaintenanceRequests.Find(req.MaintenanceRequestId);
          if (existing != null) {
            context.Entry(existing).CurrentValues.SetValues(req);
            context.SaveChanges();
          }
        })
        .Returns(Task.CompletedTask);
    maintenanceRepo.Setup(r => r.DeleteAsync(It.IsAny<MaintenanceRequest>()))
        .Callback((MaintenanceRequest req) => { context.MaintenanceRequests.Remove(req); context.SaveChanges(); })
        .Returns(Task.CompletedTask);
    
    roomRepo.Setup(r => r.Query()).Returns(context.Rooms);
    roomRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(() => context.Rooms.ToList());
    roomRepo.Setup(r => r.GetByIdAsync(It.IsAny<int>()))
        .ReturnsAsync((int id) => context.Rooms.Find(id));
    roomRepo.Setup(r => r.UpdateAsync(It.IsAny<Room>()))
        .Callback((Room room) => { 
          var existing = context.Rooms.Find(room.RoomId);
          if (existing != null) {
            context.Entry(existing).CurrentValues.SetValues(room);
            context.SaveChanges();
          }
        })
        .Returns(Task.CompletedTask);
    
    tenantRepo.Setup(r => r.Query()).Returns(context.Tenants);
    tenantRepo.Setup(r => r.GetByIdAsync(It.IsAny<int>()))
        .ReturnsAsync((int id) => context.Tenants.Find(id));
    
    var controller = new MaintenanceController(
        maintenanceRepo.Object,
        roomRepo.Object,
        tenantRepo.Object,
        mapper
    );
    
    controller.ControllerContext = new ControllerContext
    {
      HttpContext = new DefaultHttpContext { User = user }
    };
    controller.TempData = new TempDataDictionary(controller.ControllerContext.HttpContext, Mock.Of<ITempDataProvider>());
    return controller;
  }

  private ClaimsPrincipal GetUser(string role, int userId = 1)
  {
    var claims = new List<Claim>
    {
      new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
      new Claim(ClaimTypes.Role, role)
    };
    var identity = new ClaimsIdentity(claims, "TestAuth");
    return new ClaimsPrincipal(identity);
  }

  [Fact]
  public async Task Index_Manager_ReturnsViewWithRequests()
  {
    var context = GetDbContext();
    var room = new Room { RoomId = 1, Number = "101", Type = "Single", Status = "Available" };
    var request = new MaintenanceRequest 
    { 
      MaintenanceRequestId = 1, 
      RoomId = 1, 
      Room = room,
      Description = "Test maintenance",
      RequestDate = DateTime.Now,
      Status = "Pending",
      TenantId = "1"
    };
    
    context.Rooms.Add(room);
    context.MaintenanceRequests.Add(request);
    context.SaveChanges();

    var user = GetUser("Manager");
    var controller = CreateController(context, user);

    var result = await controller.Index();

    var viewResult = Assert.IsType<ViewResult>(result);
    Assert.IsAssignableFrom<List<MaintenanceRequestViewModel>>(viewResult.Model);
    Assert.True((bool)viewResult.ViewData["IsManager"]);
  }

  [Fact]
  public async Task Index_Tenant_ReturnsViewWithTenantRequests()
  {
    var context = GetDbContext();
    var user = new User { UserId = 2, Username = "tenant", Role = "Tenant" };
    var room = new Room { RoomId = 1, Number = "101", Type = "Single", Status = "Available" };
    var tenant = new Tenant { TenantId = 5, UserId = 2, RoomId = 1, FullName = "Test Tenant", Contact = "123", EmergencyContactName = "EC", EmergencyContactNumber = "123" };
    var request = new MaintenanceRequest { MaintenanceRequestId = 2, RoomId = 1, Room = room, TenantId = "5", Description = "Test", RequestDate = DateTime.Now, Status = "Pending" };

    context.Users.Add(user);
    context.Rooms.Add(room);
    context.Tenants.Add(tenant);
    context.MaintenanceRequests.Add(request);
    context.SaveChanges();

    var userClaims = GetUser("Tenant", 2);
    var controller = CreateController(context, userClaims);

    var result = await controller.Index();

    var viewResult = Assert.IsType<ViewResult>(result);
    Assert.IsAssignableFrom<List<MaintenanceRequestViewModel>>(viewResult.Model);
    Assert.False((bool)viewResult.ViewData["IsManager"]);
  }

  [Fact]
  public async Task Index_UnknownRole_ReturnsForbid()
  {
    var context = GetDbContext();
    var user = GetUser("UnknownRole");
    var controller = CreateController(context, user);

    var result = await controller.Index();

    Assert.IsType<ForbidResult>(result);
  }

  [Fact]
  public async Task SubmitTenantRequest_Get_TenantNotFound_RedirectsToIndex()
  {
    var context = GetDbContext();
    var user = GetUser("Tenant", 3);
    var controller = CreateController(context, user);

    var result = await controller.SubmitTenantRequest();

    var redirect = Assert.IsType<RedirectToActionResult>(result);
    Assert.Equal("Index", redirect.ActionName);
  }

  [Fact]
  public async Task SubmitTenantRequest_Get_TenantFound_ReturnsView()
  {
    var context = GetDbContext();
    var room = new Room { RoomId = 1, Number = "101", Type = "Single", Status = "Available" };
    var tenant = new Tenant { TenantId = 5, UserId = 2, RoomId = 1, Room = room, FullName = "Test Tenant", Contact = "123", EmergencyContactName = "EC", EmergencyContactNumber = "123" };
    
    context.Rooms.Add(room);
    context.Tenants.Add(tenant);
    context.SaveChanges();

    var user = GetUser("Tenant", 2);
    var controller = CreateController(context, user);

    var result = await controller.SubmitTenantRequest();

    var viewResult = Assert.IsType<ViewResult>(result);
    var model = Assert.IsType<MaintenanceRequestViewModel>(viewResult.Model);
    Assert.Equal(tenant.RoomId, model.RoomId);
    Assert.Equal(tenant.TenantId.ToString(), model.TenantId);
  }

  [Fact]
  public async Task SubmitTenantRequest_Post_InvalidModel_ReturnsView()
  {
    var context = GetDbContext();
    var room = new Room { RoomId = 1, Number = "101", Type = "Single", Status = "Available" };
    var tenant = new Tenant { TenantId = 5, UserId = 2, RoomId = 1, Room = room, FullName = "Test Tenant", Contact = "123", EmergencyContactName = "EC", EmergencyContactNumber = "123" };
    
    context.Rooms.Add(room);
    context.Tenants.Add(tenant);
    context.SaveChanges();

    var user = GetUser("Tenant", 2);
    var controller = CreateController(context, user);
    controller.ModelState.AddModelError("Description", "Required");

    var model = new MaintenanceRequestViewModel();
    var result = await controller.SubmitTenantRequest(model);

    var viewResult = Assert.IsType<ViewResult>(result);
    Assert.Equal(model, viewResult.Model);
  }

  [Fact]
  public async Task SubmitTenantRequest_Post_ValidModel_AddsRequestAndRedirects()
  {
    var context = GetDbContext();
    var room = new Room { RoomId = 1, Number = "101", Type = "Single", Status = "Available" };
    var tenant = new Tenant { TenantId = 5, UserId = 2, RoomId = 1, Room = room, FullName = "Test Tenant", Contact = "123", EmergencyContactName = "EC", EmergencyContactNumber = "123" };
    
    context.Rooms.Add(room);
    context.Tenants.Add(tenant);
    context.SaveChanges();

    var user = GetUser("Tenant", 2);
    var controller = CreateController(context, user);

    var model = new MaintenanceRequestViewModel 
    { 
      Description = "Test maintenance request",
      RoomId = 1,
      TenantId = "5"
    };

    var result = await controller.SubmitTenantRequest(model);

    var redirect = Assert.IsType<RedirectToActionResult>(result);
    Assert.Equal("Index", redirect.ActionName);
    Assert.Single(context.MaintenanceRequests);
  }

  [Fact]
  public async Task Create_Manager_ReturnsViewWithRooms()
  {
    var context = GetDbContext();
    var room = new Room { RoomId = 1, Number = "101", Type = "Single", Status = "Available" };
    context.Rooms.Add(room);
    context.SaveChanges();

    var user = GetUser("Manager");
    var controller = CreateController(context, user);

    var result = await controller.Create();

    var viewResult = Assert.IsType<ViewResult>(result);
    Assert.IsType<MaintenanceRequestViewModel>(viewResult.Model);
    Assert.NotNull(controller.ViewBag.Rooms);
  }

  [Fact]
  public async Task Complete_RequestNotFound_ReturnsNotFound()
  {
    var context = GetDbContext();
    var user = GetUser("Manager");
    var controller = CreateController(context, user);

    var result = await controller.Complete(99);

    Assert.IsType<NotFoundResult>(result);
  }

  [Fact]
  public async Task Complete_RequestFound_UpdatesStatusAndRedirects()
  {
    var context = GetDbContext();
    var request = new MaintenanceRequest 
    { 
      MaintenanceRequestId = 1, 
      RoomId = 1,
      Status = "Pending",
      Description = "Test",
      RequestDate = DateTime.Now,
      TenantId = "1"
    };
    context.MaintenanceRequests.Add(request);
    context.SaveChanges();

    var user = GetUser("Manager");
    var controller = CreateController(context, user);

    var result = await controller.Complete(1);

    var redirect = Assert.IsType<RedirectToActionResult>(result);
    Assert.Equal("Index", redirect.ActionName);
    Assert.Equal("Completed", request.Status);
  }

  [Fact]
  public async Task Delete_RequestNotFound_ReturnsNotFound()
  {
    var context = GetDbContext();
    var user = GetUser("Manager");
    var controller = CreateController(context, user);

    var result = await controller.Delete(1);

    Assert.IsType<NotFoundResult>(result);
  }

  [Fact]
  public async Task Delete_RequestFound_DeletesRequestAndRedirects()
  {
    var context = GetDbContext();
    var room = new Room { RoomId = 1, Number = "101", Type = "Single", Status = "Under Maintenance" };
    var request = new MaintenanceRequest 
    { 
      MaintenanceRequestId = 1, 
      RoomId = 1, 
      Room = room,
      Description = "Test",
      RequestDate = DateTime.Now,
      Status = "Pending",
      TenantId = "1"
    };
    
    context.Rooms.Add(room);
    context.MaintenanceRequests.Add(request);
    context.SaveChanges();

    var user = GetUser("Manager");
    var controller = CreateController(context, user);

    var result = await controller.Delete(1);

    var redirect = Assert.IsType<RedirectToActionResult>(result);
    Assert.Equal("Index", redirect.ActionName);
    Assert.Empty(context.MaintenanceRequests);
  }

  [Fact]
  public async Task History_ReturnsViewWithHistory()
  {
    var context = GetDbContext();
    var room = new Room { RoomId = 1, Number = "101", Type = "Single", Status = "Available" };
    var request = new MaintenanceRequest 
    { 
      MaintenanceRequestId = 1, 
      RoomId = 1, 
      Room = room,
      Description = "Test",
      RequestDate = DateTime.Now,
      Status = "Completed",
      TenantId = "1"
    };
    
    context.Rooms.Add(room);
    context.MaintenanceRequests.Add(request);
    context.SaveChanges();

    var user = GetUser("Manager");
    var controller = CreateController(context, user);

    var result = await controller.History(1);

    var viewResult = Assert.IsType<ViewResult>(result);
    Assert.IsAssignableFrom<List<MaintenanceRequestViewModel>>(viewResult.Model);
  }

  [Fact]
  public async Task GetRequest_RequestNotFound_ReturnsNotFound()
  {
    var context = GetDbContext();
    var user = GetUser("Manager");
    var controller = CreateController(context, user);

    var result = await controller.GetRequest(1);

    Assert.IsType<NotFoundResult>(result);
  }

  [Fact]
  public async Task GetRequest_RequestFound_ReturnsJson()
  {
    var context = GetDbContext();
    var request = new MaintenanceRequest 
    { 
      MaintenanceRequestId = 1, 
      RoomId = 1,
      Description = "Test",
      RequestDate = DateTime.Now,
      Status = "Pending",
      TenantId = "1"
    };
    context.MaintenanceRequests.Add(request);
    context.SaveChanges();

    var user = GetUser("Manager");
    var controller = CreateController(context, user);

    var result = await controller.GetRequest(1);

    var json = Assert.IsType<JsonResult>(result);
    Assert.IsType<MaintenanceRequestViewModel>(json.Value);
  }

  [Fact]
  public async Task Index_Tenant_NoTenantRecord_ReturnsViewWithEmptyModel()
  {
    var context = GetDbContext();
    var user = GetUser("Tenant", 999);
    var controller = CreateController(context, user);

    var result = await controller.Index();

    var viewResult = Assert.IsType<ViewResult>(result);
    Assert.IsAssignableFrom<IEnumerable<MaintenanceRequestViewModel>>(viewResult.Model);
    Assert.Empty((IEnumerable<MaintenanceRequestViewModel>)viewResult.Model);
  }

  [Fact]
  public async Task SubmitTenantRequest_Post_TenantNotFound_RedirectsToIndex()
  {
    var context = GetDbContext();
    var user = GetUser("Tenant", 999);
    var controller = CreateController(context, user);

    var model = new MaintenanceRequestViewModel();
    var result = await controller.SubmitTenantRequest(model);

    var redirect = Assert.IsType<RedirectToActionResult>(result);
    Assert.Equal("Index", redirect.ActionName);
  }

  [Fact]
  public async Task RequestModal_Get_NewRequest_ReturnsViewWithEmptyModel()
  {
    var context = GetDbContext();
    var room = new Room { RoomId = 1, Number = "101", Type = "Single", Status = "Available" };
    context.Rooms.Add(room);
    context.SaveChanges();

    var user = GetUser("Manager");
    var controller = CreateController(context, user);

    var result = await controller.RequestModal();

    var viewResult = Assert.IsType<PartialViewResult>(result);
    var model = Assert.IsType<MaintenanceRequestViewModel>(viewResult.Model);
    Assert.Equal(0, model.MaintenanceRequestId);
    Assert.NotNull(controller.ViewBag.Rooms);
  }

  [Fact]
  public async Task RequestModal_Get_EditRequest_ReturnsViewWithModel()
  {
    var context = GetDbContext();
    var room = new Room { RoomId = 1, Number = "101", Type = "Single", Status = "Available" };
    var request = new MaintenanceRequest 
    { 
      MaintenanceRequestId = 1, 
      RoomId = 1, 
      Room = room,
      Description = "Test maintenance",
      RequestDate = DateTime.Now,
      Status = "Pending",
      TenantId = "1"
    };
    
    context.Rooms.Add(room);
    context.MaintenanceRequests.Add(request);
    context.SaveChanges();

    var user = GetUser("Manager");
    var controller = CreateController(context, user);

    var result = await controller.RequestModal(1);

    var viewResult = Assert.IsType<PartialViewResult>(result);
    var model = Assert.IsType<MaintenanceRequestViewModel>(viewResult.Model);
    Assert.Equal(1, model.MaintenanceRequestId);
    Assert.Equal("Test maintenance", model.Description);
    Assert.NotNull(controller.ViewBag.Rooms);
  }

  [Fact]
  public async Task RequestModal_Get_RequestNotFound_ReturnsEmptyModel()
  {
    var context = GetDbContext();
    var user = GetUser("Manager");
    var controller = CreateController(context, user);

    var result = await controller.RequestModal(999);

    var viewResult = Assert.IsType<PartialViewResult>(result);
    var model = Assert.IsType<MaintenanceRequestViewModel>(viewResult.Model);
    Assert.Equal(0, model.MaintenanceRequestId);
  }

  [Fact]
  public async Task CreateOrEdit_Post_CreateNewRequest_AddsRequestAndRedirects()
  {
    var context = GetDbContext();
    var room = new Room { RoomId = 1, Number = "101", Type = "Single", Status = "Available" };
    var tenant = new Tenant { TenantId = 1, RoomId = 1, FullName = "Test Tenant", Contact = "123", EmergencyContactName = "EC", EmergencyContactNumber = "123" };
    context.Rooms.Add(room);
    context.Tenants.Add(tenant);
    context.SaveChanges();

    var user = GetUser("Manager");
    var controller = CreateController(context, user);

    var model = new MaintenanceRequestViewModel
    {
      MaintenanceRequestId = 0,
      RoomId = 1,
      Description = "New maintenance request",
      Status = "Pending"
    };

    var result = await controller.CreateOrEdit(model);

    var redirect = Assert.IsType<RedirectToActionResult>(result);
    Assert.Equal("Index", redirect.ActionName);
    Assert.Single(context.MaintenanceRequests);
    var addedRequest = context.MaintenanceRequests.First();
    Assert.Equal("New maintenance request", addedRequest.Description);
  }

  [Fact]
  public async Task CreateOrEdit_Post_UpdateExistingRequest_UpdatesRequestAndRedirects()
  {
    var context = GetDbContext();
    var room = new Room { RoomId = 1, Number = "101", Type = "Single", Status = "Available" };
    var tenant = new Tenant { TenantId = 1, RoomId = 1, FullName = "Test Tenant", Contact = "123", EmergencyContactName = "EC", EmergencyContactNumber = "123" };
    var request = new MaintenanceRequest 
    { 
      MaintenanceRequestId = 1, 
      RoomId = 1, 
      Room = room,
      Description = "Original description",
      RequestDate = DateTime.Now,
      Status = "Pending",
      TenantId = "1"
    };
    
    context.Rooms.Add(room);
    context.Tenants.Add(tenant);
    context.MaintenanceRequests.Add(request);
    context.SaveChanges();

    var user = GetUser("Manager");
    var controller = CreateController(context, user);

    var model = new MaintenanceRequestViewModel
    {
      MaintenanceRequestId = 1,
      RoomId = 1,
      Description = "Updated description",
      Status = "Pending"
    };

    var result = await controller.CreateOrEdit(model);

    var redirect = Assert.IsType<RedirectToActionResult>(result);
    Assert.Equal("Index", redirect.ActionName);
    Assert.Single(context.MaintenanceRequests);
    var updatedRequest = context.MaintenanceRequests.First();
    Assert.Equal("Updated description", updatedRequest.Description);
  }

  [Fact]
  public async Task CreateOrEdit_Post_NoTenantFound_ReturnsIndexView()
  {
    var context = GetDbContext();
    var room = new Room { RoomId = 1, Number = "101", Type = "Single", Status = "Available" };
    context.Rooms.Add(room);
    context.SaveChanges();

    var user = GetUser("Manager");
    var controller = CreateController(context, user);

    var model = new MaintenanceRequestViewModel
    {
      MaintenanceRequestId = 0,
      RoomId = 1,
      Description = "Test description"
    };

    var result = await controller.CreateOrEdit(model);

    var viewResult = Assert.IsType<ViewResult>(result);
    Assert.Equal("Index", viewResult.ViewName);
    Assert.NotNull(controller.ViewBag.Rooms);
  }

  [Fact]
  public async Task CreateOrEdit_Post_EditRequestNotFound_ReturnsNotFound()
  {
    var context = GetDbContext();
    var room = new Room { RoomId = 1, Number = "101", Type = "Single", Status = "Available" };
    var tenant = new Tenant { TenantId = 1, RoomId = 1, FullName = "Test Tenant", Contact = "123", EmergencyContactName = "EC", EmergencyContactNumber = "123" };
    context.Rooms.Add(room);
    context.Tenants.Add(tenant);
    context.SaveChanges();

    var user = GetUser("Manager");
    var controller = CreateController(context, user);

    var model = new MaintenanceRequestViewModel
    {
      MaintenanceRequestId = 999,
      RoomId = 1,
      Description = "Test description",
      Status = "Pending"
    };

    var result = await controller.CreateOrEdit(model);

    Assert.IsType<NotFoundResult>(result);
  }

  [Fact]
  public void DeleteModal_RequestFound_ReturnsPartialView()
  {
    var context = GetDbContext();
    var request = new MaintenanceRequest 
    { 
      MaintenanceRequestId = 1, 
      RoomId = 1,
      Description = "Test",
      RequestDate = DateTime.Now,
      Status = "Pending",
      TenantId = "1"
    };
    context.MaintenanceRequests.Add(request);
    context.SaveChanges();

    var user = GetUser("Manager");
    var controller = CreateController(context, user);

    var result = controller.DeleteModal(1);

    var viewResult = Assert.IsType<PartialViewResult>(result);
    Assert.Equal("_DeleteModal", viewResult.ViewName);
    Assert.IsType<MaintenanceRequestViewModel>(viewResult.Model);
  }

  [Fact]
  public void DeleteModal_RequestNotFound_ReturnsNotFound()
  {
    var context = GetDbContext();
    var user = GetUser("Manager");
    var controller = CreateController(context, user);

    var result = controller.DeleteModal(999);

    Assert.IsType<NotFoundResult>(result);
  }
}