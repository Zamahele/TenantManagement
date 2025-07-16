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
using PropertyManagement.Web.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Security.Claims;
using System.Threading.Tasks;

namespace PropertyManagement.Test
{
    public abstract class TestBaseClass
    {
        protected ApplicationDbContext GetDbContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            return new ApplicationDbContext(options);
        }

        protected IMapper GetMapper()
        {
            var config = new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<Room, RoomViewModel>().ReverseMap();
                cfg.CreateMap<Room, RoomFormViewModel>().ReverseMap();
                cfg.CreateMap<Tenant, TenantViewModel>().ReverseMap();
                cfg.CreateMap<User, UserViewModel>().ReverseMap();
                cfg.CreateMap<Payment, PaymentViewModel>().ReverseMap();
                cfg.CreateMap<LeaseAgreement, LeaseAgreementViewModel>().ReverseMap();
                cfg.CreateMap<MaintenanceRequest, MaintenanceRequestViewModel>().ReverseMap();
                cfg.CreateMap<BookingRequest, BookingRequestViewModel>().ReverseMap();
                cfg.CreateMap<Inspection, InspectionViewModel>().ReverseMap();
                cfg.CreateMap<UtilityBill, UtilityBill>().ReverseMap();
            }, NullLoggerFactory.Instance);
            return config.CreateMapper();
        }

        protected Mock<IGenericRepository<T>> GetRepositoryMock<T>(ApplicationDbContext context) where T : class
        {
            var mock = new Mock<IGenericRepository<T>>();
            var dbSet = context.Set<T>();
            
            mock.Setup(r => r.Query()).Returns(dbSet);
            mock.Setup(r => r.GetAllAsync()).ReturnsAsync(() => dbSet.ToList());
            mock.Setup(r => r.GetAllAsync(It.IsAny<Expression<Func<T, bool>>>(), It.IsAny<Expression<Func<T, object>>[]>()))
                .ReturnsAsync((Expression<Func<T, bool>> filter, Expression<Func<T, object>>[] includes) => 
                {
                    var query = dbSet.AsQueryable();
                    if (filter != null) query = query.Where(filter);
                    return query.ToList();
                });
            mock.Setup(r => r.GetByIdAsync(It.IsAny<object>()))
                .ReturnsAsync((object id) => dbSet.Find(id));
            mock.Setup(r => r.AddAsync(It.IsAny<T>()))
                .Callback((T entity) => { dbSet.Add(entity); context.SaveChanges(); })
                .Returns(Task.CompletedTask);
            mock.Setup(r => r.UpdateAsync(It.IsAny<T>()))
                .Callback((T entity) => { context.Entry(entity).State = EntityState.Modified; context.SaveChanges(); })
                .Returns(Task.CompletedTask);
            mock.Setup(r => r.DeleteAsync(It.IsAny<T>()))
                .Callback((T entity) => { dbSet.Remove(entity); context.SaveChanges(); })
                .Returns(Task.CompletedTask);
            
            return mock;
        }

        protected ClaimsPrincipal GetUser(string role, int userId = 1, string username = "testuser")
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Name, username),
                new Claim(ClaimTypes.Role, role)
            };
            var identity = new ClaimsIdentity(claims, "TestAuthentication");
            return new ClaimsPrincipal(identity);
        }

        protected void SetupControllerContext(Controller controller, ClaimsPrincipal user)
        {
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };
            controller.TempData = new TempDataDictionary(controller.ControllerContext.HttpContext, Mock.Of<ITempDataProvider>());
        }

        protected void SeedTestData(ApplicationDbContext context)
        {
            // Add sample users
            var users = new List<User>
            {
                new User { UserId = 1, Username = "manager", Role = "Manager", PasswordHash = "hash" },
                new User { UserId = 2, Username = "tenant1", Role = "Tenant", PasswordHash = "hash" },
                new User { UserId = 3, Username = "tenant2", Role = "Tenant", PasswordHash = "hash" }
            };
            context.Users.AddRange(users);

            // Add sample rooms
            var rooms = new List<Room>
            {
                new Room { RoomId = 1, Number = "101", Type = "Single", Status = "Available" },
                new Room { RoomId = 2, Number = "102", Type = "Double", Status = "Occupied" },
                new Room { RoomId = 3, Number = "103", Type = "Suite", Status = "Under Maintenance" }
            };
            context.Rooms.AddRange(rooms);

            // Add sample tenants
            var tenants = new List<Tenant>
            {
                new Tenant { TenantId = 1, UserId = 2, RoomId = 2, FullName = "John Doe", Contact = "1234567890", EmergencyContactName = "Jane Doe", EmergencyContactNumber = "0987654321" },
                new Tenant { TenantId = 2, UserId = 3, RoomId = 1, FullName = "Bob Smith", Contact = "5555555555", EmergencyContactName = "Alice Smith", EmergencyContactNumber = "4444444444" }
            };
            context.Tenants.AddRange(tenants);

            // Add sample lease agreements
            var leases = new List<LeaseAgreement>
            {
                new LeaseAgreement { LeaseAgreementId = 1, TenantId = 1, RoomId = 2, StartDate = DateTime.Now.AddMonths(-1), EndDate = DateTime.Now.AddMonths(11), RentAmount = 1000, ExpectedRentDay = 1 },
                new LeaseAgreement { LeaseAgreementId = 2, TenantId = 2, RoomId = 1, StartDate = DateTime.Now.AddMonths(-2), EndDate = DateTime.Now.AddMonths(10), RentAmount = 800, ExpectedRentDay = 5 }
            };
            context.LeaseAgreements.AddRange(leases);

            // Add sample payments
            var payments = new List<Payment>
            {
                new Payment { PaymentId = 1, TenantId = 1, LeaseAgreementId = 1, Amount = 1000, Type = "Rent", PaymentMonth = DateTime.Now.Month, PaymentYear = DateTime.Now.Year, Date = DateTime.Now },
                new Payment { PaymentId = 2, TenantId = 2, LeaseAgreementId = 2, Amount = 800, Type = "Rent", PaymentMonth = DateTime.Now.Month, PaymentYear = DateTime.Now.Year, Date = DateTime.Now.AddDays(-5) }
            };
            context.Payments.AddRange(payments);

            // Add sample maintenance requests
            var maintenanceRequests = new List<MaintenanceRequest>
            {
                new MaintenanceRequest { MaintenanceRequestId = 1, RoomId = 3, TenantId = "1", Description = "Leaky faucet", RequestDate = DateTime.Now.AddDays(-3), Status = "Pending" },
                new MaintenanceRequest { MaintenanceRequestId = 2, RoomId = 2, TenantId = "2", Description = "Broken window", RequestDate = DateTime.Now.AddDays(-1), Status = "In Progress" }
            };
            context.MaintenanceRequests.AddRange(maintenanceRequests);

            context.SaveChanges();
        }
    }
}