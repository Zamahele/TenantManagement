using AutoMapper;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using PropertyManagement.Domain.Entities;
using PropertyManagement.Infrastructure.Data;
using PropertyManagement.Infrastructure.Repositories;
using PropertyManagement.Web.ViewModels;
using System.Linq.Expressions;
using System.Security.Claims;
using Xunit;
using Assert = Xunit.Assert;

namespace PropertyManagement.Test.Controllers;

public class TenantsControllerTests
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
    expr.CreateMap<Tenant, TenantViewModel>().ReverseMap();
    expr.CreateMap<User, UserViewModel>().ReverseMap();
    expr.CreateMap<Room, RoomViewModel>().ReverseMap();
    expr.CreateMap<Payment, PaymentViewModel>().ReverseMap();
    expr.CreateMap<LeaseAgreement, LeaseAgreementViewModel>().ReverseMap();
    var config = new MapperConfiguration(expr, NullLoggerFactory.Instance);
    return config.CreateMapper();
  }

  private TenantsController GetController(ApplicationDbContext context, ClaimsPrincipal user)
  {
    var tenantRepo = new Mock<IGenericRepository<Tenant>>();
    var userRepo = new Mock<IGenericRepository<User>>();
    var roomRepo = new Mock<IGenericRepository<Room>>();

    // Setup repository methods to use actual context data
    tenantRepo.Setup(r => r.Query()).Returns(context.Tenants);
    tenantRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(() => context.Tenants.ToList());
    tenantRepo.Setup(r => r.GetAllAsync(It.IsAny<Expression<Func<Tenant, bool>>>(), It.IsAny<Expression<Func<Tenant, object>>[]>()))
              .ReturnsAsync((Expression<Func<Tenant, bool>> filter, Expression<Func<Tenant, object>>[] includes) =>
              {
                var query = context.Tenants.AsQueryable();
                if (filter != null) query = query.Where(filter);
                return query.ToList();
              });
    tenantRepo.Setup(r => r.GetByIdAsync(It.IsAny<int>()))
              .ReturnsAsync((int id) => context.Tenants.Find(id));
    tenantRepo.Setup(r => r.AddAsync(It.IsAny<Tenant>()))
              .Callback((Tenant tenant) => { context.Tenants.Add(tenant); context.SaveChanges(); })
              .Returns(Task.CompletedTask);
    tenantRepo.Setup(r => r.UpdateAsync(It.IsAny<Tenant>()))
              .Callback((Tenant tenant) => { context.Entry(tenant).State = EntityState.Modified; context.SaveChanges(); })
              .Returns(Task.CompletedTask);
    tenantRepo.Setup(r => r.DeleteAsync(It.IsAny<Tenant>()))
              .Callback((Tenant tenant) =>
              {
                var trackedTenant = context.Tenants.Find(tenant.TenantId);
                if (trackedTenant != null)
                {
                  context.Tenants.Remove(trackedTenant);
                  context.SaveChanges();
                }
              })
              .Returns(Task.CompletedTask);

    userRepo.Setup(r => r.Query()).Returns(context.Users);
    userRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(() => context.Users.ToList());
    userRepo.Setup(r => r.GetAllAsync(It.IsAny<Expression<Func<User, bool>>>(), It.IsAny<Expression<Func<User, object>>[]>()))
            .ReturnsAsync((Expression<Func<User, bool>> filter, Expression<Func<User, object>>[] includes) =>
            {
              var query = context.Users.AsQueryable();
              if (filter != null) query = query.Where(filter);
              return query.ToList();
            });
    userRepo.Setup(r => r.GetByIdAsync(It.IsAny<int>()))
            .ReturnsAsync((int id) => context.Users.Find(id));
    userRepo.Setup(r => r.AddAsync(It.IsAny<User>()))
            .Callback((User user) => { context.Users.Add(user); context.SaveChanges(); })
            .Returns(Task.CompletedTask);
    userRepo.Setup(r => r.UpdateAsync(It.IsAny<User>()))
            .Callback((User user) => { context.Entry(user).State = EntityState.Modified; context.SaveChanges(); })
            .Returns(Task.CompletedTask);
    userRepo.Setup(r => r.DeleteAsync(It.IsAny<User>()))
            .Callback((User user) => { context.Users.Remove(user); context.SaveChanges(); })
            .Returns(Task.CompletedTask);

    roomRepo.Setup(r => r.Query()).Returns(context.Rooms);
    roomRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(() => context.Rooms.ToList());
    roomRepo.Setup(r => r.GetAllAsync(It.IsAny<Expression<Func<Room, bool>>>(), It.IsAny<Expression<Func<Room, object>>[]>()))
            .ReturnsAsync((Expression<Func<Room, bool>> filter, Expression<Func<Room, object>>[] includes) =>
            {
              var query = context.Rooms.AsQueryable();
              if (filter != null) query = query.Where(filter);
              return query.ToList();
            });
    roomRepo.Setup(r => r.GetByIdAsync(It.IsAny<int>()))
            .ReturnsAsync((int id) => context.Rooms.Find(id));

    var mapper = GetMapper();

    var controller = new TenantsController(
        tenantRepo.Object,
        userRepo.Object,
        roomRepo.Object,
        mapper
    );

    var tempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>());
    controller.TempData = tempData;
    var httpContext = new DefaultHttpContext();
    httpContext.User = user;

    // Register a mock IAuthenticationService
    var authServiceMock = new Mock<Microsoft.AspNetCore.Authentication.IAuthenticationService>();
    authServiceMock
        .Setup(x => x.SignInAsync(
            It.IsAny<HttpContext>(),
            It.IsAny<string>(),
            It.IsAny<ClaimsPrincipal>(),
            It.IsAny<Microsoft.AspNetCore.Authentication.AuthenticationProperties>()))
        .Returns(Task.CompletedTask);

    var services = new ServiceCollection();
    services.AddLogging();
    services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme);
    // Add this line to register MVC services, including IUrlHelperFactory:
    services.AddControllersWithViews();
    services.AddSingleton(authServiceMock.Object);
    var serviceProvider = services.BuildServiceProvider();
    httpContext.RequestServices = serviceProvider;

    var actionDescriptor = new Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor
    {
      ControllerName = "Tenants",
      ActionName = "Login"
    };
    controller.ControllerContext = new ControllerContext
    {
      HttpContext = httpContext,
      ActionDescriptor = actionDescriptor,
      RouteData = new Microsoft.AspNetCore.Routing.RouteData()
    };
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
    Assert.IsAssignableFrom<IEnumerable<TenantViewModel>>(viewResult.Model);
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
    Assert.IsType<TenantViewModel>(viewResult.Model);
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
    Assert.IsType<TenantViewModel>(partial.Model);
  }

  [Fact]
  public async Task EditProfile_Post_Manager_UpdatesProfileAndRedirects()
  {
    var context = GetDbContext();
    context.Users.Add(new User { UserId = 2, Username = "tenant", PasswordHash = "hash", Role = "Tenant" });
    context.Tenants.Add(new Tenant { TenantId = 1, UserId = 2, RoomId = 1, FullName = "Tenant", Contact = "123", EmergencyContactName = "EC", EmergencyContactNumber = "123" });
    context.SaveChanges();

    var controller = GetController(context, GetUser("Manager"));

    var updatedTenantVm = new TenantViewModel
    {
      TenantId = 1,
      FullName = "Updated Name",
      Contact = "456",
      EmergencyContactName = "NewEC",
      EmergencyContactNumber = "456",
      RoomId = 1,
      UserId = 2
    };

    var result = await controller.EditProfile(updatedTenantVm);

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
    Assert.IsType<TenantViewModel>(partial.Model);
  }

  [Fact]
  public async Task CreateOrEdit_Manager_CreatesTenantAndRedirects()
  {
    var context = GetDbContext();
    context.Rooms.Add(new Room { RoomId = 1, Number = "101", Type = "Single", Status = "Available" });
    context.SaveChanges();

    var controller = GetController(context, GetUser("Manager"));

    var tenantVm = new TenantViewModel
    {
      FullName = "New Tenant",
      Contact = "123",
      RoomId = 1,
      EmergencyContactName = "EC",
      EmergencyContactNumber = "123"
    };

    var result = await controller.CreateOrEdit(tenantVm, "newuser", "password123");

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
    Assert.IsType<TenantViewModel>(json.Value);
  }

  [Fact]
  public async Task Delete_Manager_DeletesTenantAndRedirects()
  {
    var context = GetDbContext();
    var user = new User { UserId = 1, Username = "testuser", PasswordHash = "hash", Role = "Tenant" };
    context.Users.Add(user);
    context.Tenants.Add(new Tenant { TenantId = 1, UserId = 1, RoomId = 1, FullName = "Tenant", Contact = "123", EmergencyContactName = "EC", EmergencyContactNumber = "123" });
    context.SaveChanges();

    var controller = GetController(context, GetUser("Manager"));

    var result = await controller.Delete(1);

    var redirect = Assert.IsType<RedirectToActionResult>(result);
    Assert.Equal("Index", redirect.ActionName);
    Assert.Empty(context.Tenants);
    Assert.Equal("Tenant deleted successfully.", controller.TempData["Success"]);
  }

  [Fact]
  public async Task CreateOrEdit_DuplicateUsername_ReturnsPartialViewWithError()
  {
    var context = GetDbContext();
    context.Users.Add(new User { UserId = 1, Username = "existing", PasswordHash = "hash", Role = "Tenant" });
    context.Rooms.Add(new Room { RoomId = 1, Number = "101", Type = "Single", Status = "Available" });
    context.SaveChanges();

    var controller = GetController(context, GetUser("Manager"));

    var tenantVm = new TenantViewModel
    {
      FullName = "New Tenant",
      Contact = "123",
      RoomId = 1,
      EmergencyContactName = "EC",
      EmergencyContactNumber = "123"
    };

    var result = await controller.CreateOrEdit(tenantVm, "existing", "password123");

    var partial = Assert.IsType<PartialViewResult>(result);
    Assert.Equal("_TenantForm", partial.ViewName);
    Assert.Equal("Username already exists.", controller.TempData["Error"]);
  }

  [Fact]
  public async Task CreateOrEdit_DuplicateContact_ReturnsPartialViewWithError()
  {
    var context = GetDbContext();
    context.Tenants.Add(new Tenant { TenantId = 1, Contact = "123456789", FullName = "Existing", EmergencyContactName = "EC", EmergencyContactNumber = "123" });
    context.Rooms.Add(new Room { RoomId = 1, Number = "101", Type = "Single", Status = "Available" });
    context.SaveChanges();

    var controller = GetController(context, GetUser("Manager"));

    var tenantVm = new TenantViewModel
    {
      FullName = "New Tenant",
      Contact = "123456789",
      RoomId = 1,
      EmergencyContactName = "EC",
      EmergencyContactNumber = "123"
    };

    var result = await controller.CreateOrEdit(tenantVm, "newuser", "password123");

    var partial = Assert.IsType<PartialViewResult>(result);
    Assert.Equal("_TenantForm", partial.ViewName);
    Assert.Equal("Contact number already exists.", controller.TempData["Error"]);
  }

  [Fact]
  public async Task CreateOrEdit_WeakPassword_ReturnsPartialViewWithError()
  {
    var context = GetDbContext();
    context.Rooms.Add(new Room { RoomId = 1, Number = "101", Type = "Single", Status = "Available" });
    context.SaveChanges();

    var controller = GetController(context, GetUser("Manager"));

    var tenantVm = new TenantViewModel
    {
      FullName = "New Tenant",
      Contact = "123",
      RoomId = 1,
      EmergencyContactName = "EC",
      EmergencyContactNumber = "123"
    };

    var result = await controller.CreateOrEdit(tenantVm, "newuser", "123"); // Short password

    var partial = Assert.IsType<PartialViewResult>(result);
    Assert.Equal("_TenantForm", partial.ViewName);
    Assert.Equal("Password must be at least 8 characters long.", controller.TempData["Error"]);
  }

  [Fact]
  public async Task Login_ValidCredentials_RedirectsToProfile()
  {
    var context = GetDbContext();
    var user = new User { UserId = 1, Username = "testuser", PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123"), Role = "Tenant" };
    context.Users.Add(user);
    context.SaveChanges();

    var controller = GetController(context, GetUser("Tenant", 1));

    var loginModel = new TenantLoginViewModel { Username = "testuser", Password = "password123" };

    var result = await controller.Login(loginModel);

    var redirect = Assert.IsType<RedirectToActionResult>(result);
    Assert.Equal("Profile", redirect.ActionName);
  }

  [Fact]
  public async Task Login_InvalidCredentials_ReturnsViewWithError()
  {
    var context = GetDbContext();
    var user = new User { UserId = 1, Username = "testuser", PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123"), Role = "Tenant" };
    context.Users.Add(user);
    context.SaveChanges();

    var controller = GetController(context, GetUser("Tenant"));

    var loginModel = new TenantLoginViewModel { Username = "testuser", Password = "wrongpassword" };

    var result = await controller.Login(loginModel);

    var viewResult = Assert.IsType<ViewResult>(result);
    Assert.Equal("Invalid username or password.", controller.TempData["Error"]);
  }

  [Fact]
  public async Task Register_ValidData_RedirectsToLogin()
  {
    var context = GetDbContext();
    context.Rooms.Add(new Room { RoomId = 1, Number = "101", Type = "Single", Status = "Available" });
    context.SaveChanges();

    var controller = GetController(context, GetUser("Tenant"));

    var tenantVm = new TenantViewModel
    {
      FullName = "New Tenant",
      Contact = "123456789",
      RoomId = 1,
      EmergencyContactName = "EC",
      EmergencyContactNumber = "123",
      User = new UserViewModel { Username = "newuser" }
    };

    var result = await controller.Register(tenantVm, "password123");

    var redirect = Assert.IsType<RedirectToActionResult>(result);
    Assert.Equal("Login", redirect.ActionName);
    Assert.Single(context.Tenants);
    Assert.Equal("Account created successfully. Please log in.", controller.TempData["Success"]);
  }

  [Fact]
  public async Task Register_DuplicateUsername_ReturnsViewWithError()
  {
    var context = GetDbContext();
    context.Users.Add(new User { UserId = 1, Username = "existing", PasswordHash = "hash", Role = "Tenant" });
    context.Rooms.Add(new Room { RoomId = 1, Number = "101", Type = "Single", Status = "Available" });
    context.SaveChanges();

    var controller = GetController(context, GetUser("Tenant"));

    var tenantVm = new TenantViewModel
    {
      FullName = "New Tenant",
      Contact = "123456789",
      RoomId = 1,
      EmergencyContactName = "EC",
      EmergencyContactNumber = "123",
      User = new UserViewModel { Username = "existing" }
    };

    var result = await controller.Register(tenantVm, "password123");

    var viewResult = Assert.IsType<ViewResult>(result);
    Assert.Equal("Username already exists.", controller.TempData["Error"]);
  }
}