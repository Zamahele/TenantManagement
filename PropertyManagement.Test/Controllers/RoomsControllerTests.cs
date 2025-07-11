using Xunit;
using Microsoft.EntityFrameworkCore;
using PropertyManagement.Web.Controllers;
using PropertyManagement.Infrastructure.Data;
using PropertyManagement.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Http;
using Moq;
using System.Threading.Tasks;
using PropertyManagement.Domain.Entities;
using System.Linq;
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

  private RoomsController GetController(ApplicationDbContext context)
  {
    var controller = new RoomsController(context);

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
    Assert.Equal("Booking request deleted!", controller.TempData["Success"]);
  }
}