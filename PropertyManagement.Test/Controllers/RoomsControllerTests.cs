using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
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
using System.Linq.Expressions;
using System.Threading.Tasks;
using Xunit;
using Assert = Xunit.Assert;

namespace PropertyManagement.Test.Controllers;

public class RoomsControllerTests
{
  private ApplicationDbContext GetDbContext()
  {
    var options = new DbContextOptionsBuilder<ApplicationDbContext>()
        .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()) // Unique DB per test
        .Options;
    return new ApplicationDbContext(options);
  }

  private IMapper GetMapper()
  {
    var config = new MapperConfiguration(cfg =>
    {
      cfg.CreateMap<Room, RoomFormViewModel>().ReverseMap();
      cfg.CreateMap<Room, RoomViewModel>().ReverseMap();
      cfg.CreateMap<BookingRequest, BookingRequestViewModel>().ReverseMap();
      cfg.CreateMap<Tenant, TenantViewModel>().ReverseMap();
      // Add other mappings as needed
    }, NullLoggerFactory.Instance);
    return config.CreateMapper();
  }

  private RoomsController GetController(ApplicationDbContext context)
  {
    var mapper = GetMapper();
    
    // Mock repositories
    var roomRepo = new Mock<IGenericRepository<Room>>();
    var bookingRepo = new Mock<IGenericRepository<BookingRequest>>();
    var tenantRepo = new Mock<IGenericRepository<Tenant>>();
    
    // Setup repository methods to use actual context data
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
    roomRepo.Setup(r => r.AddAsync(It.IsAny<Room>()))
           .Callback((Room room) => { context.Rooms.Add(room); context.SaveChanges(); })
           .Returns(Task.CompletedTask);
    roomRepo.Setup(r => r.UpdateAsync(It.IsAny<Room>()))
           .Callback((Room room) => { context.Entry(room).State = EntityState.Modified; context.SaveChanges(); })
           .Returns(Task.CompletedTask);
    roomRepo.Setup(r => r.DeleteAsync(It.IsAny<Room>()))
           .Callback((Room room) => { context.Rooms.Remove(room); context.SaveChanges(); })
           .Returns(Task.CompletedTask);

    bookingRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(() => context.BookingRequests.ToList());
    bookingRepo.Setup(r => r.GetAllAsync(It.IsAny<Expression<Func<BookingRequest, bool>>>(), It.IsAny<Expression<Func<BookingRequest, object>>[]>()))
               .ReturnsAsync((Expression<Func<BookingRequest, bool>> filter, Expression<Func<BookingRequest, object>>[] includes) => 
               {
                   var query = context.BookingRequests.AsQueryable();
                   if (filter != null) query = query.Where(filter);
                   return query.ToList();
               });
    bookingRepo.Setup(r => r.GetByIdAsync(It.IsAny<int>()))
               .ReturnsAsync((int id) => context.BookingRequests.Find(id));
    bookingRepo.Setup(r => r.AddAsync(It.IsAny<BookingRequest>()))
               .Callback((BookingRequest booking) => { context.BookingRequests.Add(booking); context.SaveChanges(); })
               .Returns(Task.CompletedTask);
    bookingRepo.Setup(r => r.UpdateAsync(It.IsAny<BookingRequest>()))
               .Callback((BookingRequest booking) => { context.Entry(booking).State = EntityState.Modified; context.SaveChanges(); })
               .Returns(Task.CompletedTask);
    bookingRepo.Setup(r => r.DeleteAsync(It.IsAny<BookingRequest>()))
               .Callback((BookingRequest booking) => { context.BookingRequests.Remove(booking); context.SaveChanges(); })
               .Returns(Task.CompletedTask);

    tenantRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(() => context.Tenants.ToList());
    tenantRepo.Setup(r => r.GetAllAsync(It.IsAny<Expression<Func<Tenant, bool>>>(), It.IsAny<Expression<Func<Tenant, object>>[]>()))
              .ReturnsAsync((Expression<Func<Tenant, bool>> filter, Expression<Func<Tenant, object>>[] includes) => 
              {
                  var query = context.Tenants.AsQueryable();
                  if (filter != null) query = query.Where(filter);
                  return query.ToList();
              });

    var controller = new RoomsController(roomRepo.Object, bookingRepo.Object, tenantRepo.Object, context, mapper);

    // Initialize TempData with a mock provider and a default HttpContext
    var tempData = new TempDataDictionary(
        new DefaultHttpContext(),
        Mock.Of<ITempDataProvider>()
    );
    controller.TempData = tempData;

    return controller;
  }

  [Fact]
  public async Task Index_ReturnsViewWithModel()
  {
    var context = GetDbContext();
    context.Rooms.Add(new Room { RoomId = 1, Number = "101", Type = "Single", Status = "Available" });
    context.SaveChanges();
    var controller = GetController(context);

    var result = await controller.Index();

    var viewResult = Assert.IsType<ViewResult>(result);
    Assert.IsType<RoomsTabViewModel>(viewResult.Model);
  }

  [Fact]
  public async Task Create_Post_ValidModel_CreatesRoomAndRedirects()
  {
    var context = GetDbContext();
    var controller = GetController(context);
    var model = new RoomFormViewModel { Number = "102", Type = "Double", Status = "Available" };

    var result = await controller.CreateOrEdit(model);

    var redirect = Assert.IsType<RedirectToActionResult>(result);
    Assert.Equal("Index", redirect.ActionName);
    Assert.Single(context.Rooms);
    Assert.Equal("Room created successfully.", controller.TempData["Success"]);
  }

  [Fact]
  public async Task Edit_Post_ValidModel_UpdatesRoomAndRedirects()
  {
    var context = GetDbContext();
    var room = new Room { RoomId = 2, Number = "103", Type = "Suite", Status = "Available" };
    context.Rooms.Add(room);
    context.SaveChanges();
    var controller = GetController(context);
    var model = new RoomFormViewModel { RoomId = 2, Number = "103A", Type = "Suite", Status = "Occupied" };

    var result = await controller.CreateOrEdit(model);

    var redirect = Assert.IsType<RedirectToActionResult>(result);
    Assert.Equal("Index", redirect.ActionName);
    var updatedRoom = context.Rooms.First(r => r.RoomId == 2);
    Assert.Equal("103A", updatedRoom.Number);
    Assert.Equal("Occupied", updatedRoom.Status);
    Assert.Equal("Room updated successfully.", controller.TempData["Success"]);
  }

  [Fact]
  public async Task DeleteBookingRequest_DeletesBookingAndRedirects()
  {
    var context = GetDbContext();
    var booking = new BookingRequest { BookingRequestId = 1, RoomId = 1, FullName = "Test", Contact = "123", Status = "Pending" };
    context.BookingRequests.Add(booking);
    context.SaveChanges();
    var controller = GetController(context);

    var result = await controller.DeleteBookingRequest(1);

    var redirect = Assert.IsType<RedirectToActionResult>(result);
    Assert.Equal("Index", redirect.ActionName);
    Assert.Empty(context.BookingRequests);
    Assert.Equal("Booking request deleted successfully.", controller.TempData["Success"]);
  }

  [Fact]
  public async Task Delete_ValidRoom_DeletesRoomAndRedirects()
  {
    var context = GetDbContext();
    var room = new Room { RoomId = 1, Number = "101", Type = "Single", Status = "Available" };
    context.Rooms.Add(room);
    context.SaveChanges();
    var controller = GetController(context);

    var result = await controller.Delete(1);

    var redirect = Assert.IsType<RedirectToActionResult>(result);
    Assert.Equal("Index", redirect.ActionName);
    Assert.Empty(context.Rooms);
    Assert.Equal("Room deleted successfully.", controller.TempData["Success"]);
  }

  [Fact]
  public async Task Delete_RoomNotFound_ReturnsNotFound()
  {
    var context = GetDbContext();
    var controller = GetController(context);

    var result = await controller.Delete(999);

    Assert.IsType<NotFoundResult>(result);
    Assert.Equal("Room not found.", controller.TempData["Error"]);
  }

  [Fact]
  public async Task BookRoom_ValidRoom_ReturnsPartialView()
  {
    var context = GetDbContext();
    var room = new Room { RoomId = 1, Number = "101", Type = "Single", Status = "Available" };
    context.Rooms.Add(room);
    context.SaveChanges();
    var controller = GetController(context);

    var result = await controller.BookRoom(1);

    var partial = Assert.IsType<PartialViewResult>(result);
    Assert.Equal("_BookingModal", partial.ViewName);
    Assert.IsType<BookingRequestViewModel>(partial.Model);
  }

  [Fact]
  public async Task BookRoom_Post_ValidModel_CreatesBookingAndRedirects()
  {
    var context = GetDbContext();
    var room = new Room { RoomId = 1, Number = "101", Type = "Single", Status = "Available" };
    context.Rooms.Add(room);
    context.SaveChanges();
    var controller = GetController(context);

    var model = new BookingRequestViewModel
    {
      RoomId = 1,
      FullName = "Test User",
      Contact = "123456789",
      Note = "Test booking"
    };

    var result = await controller.BookRoom(model, null);

    var redirect = Assert.IsType<RedirectToActionResult>(result);
    Assert.Equal("Index", redirect.ActionName);
    Assert.Single(context.BookingRequests);
    Assert.Equal("Booking request submitted successfully.", controller.TempData["Success"]);
  }

  [Fact]
  public async Task CreateOrEdit_DuplicateRoomNumber_ReturnsViewWithError()
  {
    var context = GetDbContext();
    var existingRoom = new Room { RoomId = 1, Number = "101", Type = "Single", Status = "Available" };
    context.Rooms.Add(existingRoom);
    context.SaveChanges();
    var controller = GetController(context);

    var model = new RoomFormViewModel { Number = "101", Type = "Double", Status = "Available" };

    var result = await controller.CreateOrEdit(model);

    var viewResult = Assert.IsType<ViewResult>(result);
    Assert.Equal("Room number already exists.", controller.TempData["Error"]);
  }

  [Fact]
  public async Task GetRoom_ValidId_ReturnsJsonResult()
  {
    var context = GetDbContext();
    var room = new Room { RoomId = 1, Number = "101", Type = "Single", Status = "Available", CottageId = 1 };
    context.Rooms.Add(room);
    context.SaveChanges();
    var controller = GetController(context);

    var result = await controller.GetRoom(1);

    var jsonResult = Assert.IsType<JsonResult>(result);
    Assert.NotNull(jsonResult.Value);
  }

  [Fact]
  public async Task GetRoom_InvalidId_ReturnsNotFound()
  {
    var context = GetDbContext();
    var controller = GetController(context);

    var result = await controller.GetRoom(999);

    Assert.IsType<NotFoundResult>(result);
  }
}