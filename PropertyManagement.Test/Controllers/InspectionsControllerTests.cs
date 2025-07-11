using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Moq;
using PropertyManagement.Domain.Entities;
using PropertyManagement.Infrastructure.Data;
using PropertyManagement.Web.Controllers;
using Xunit;
using Assert = Xunit.Assert;

namespace PropertyManagement.Test.Controllers
{
  public class InspectionsControllerTests
  {
    private ApplicationDbContext GetDbContext()
    {
      var options = new DbContextOptionsBuilder<ApplicationDbContext>()
          .UseInMemoryDatabase(Guid.NewGuid().ToString())
          .Options;
      var context = new ApplicationDbContext(options);

      // Seed with sample rooms
      context.Rooms.Add(new Room { RoomId = 1, Number = "101", Type = "Single", Status = "Available" });
      context.Rooms.Add(new Room { RoomId = 2, Number = "102", Type = "Double", Status = "Available" });
      context.SaveChanges();

      return context;
    }

    [Fact]
    public async Task Index_ReturnsViewWithInspections()
    {
      // Arrange
      var context = GetDbContext();
      context.Inspections.Add(new Inspection { RoomId = 1, Date = DateTime.Today });
      context.SaveChanges();

      var controller = new InspectionsController(context);

      // Act
      var result = await controller.Index();

      // Assert
      var viewResult = Assert.IsType<ViewResult>(result);
      var model = Assert.IsAssignableFrom<IEnumerable<Inspection>>(viewResult.Model);
      Assert.Single(model);
    }

    [Fact]
    public async Task InspectionModal_Get_ReturnsPartialViewForAdd()
    {
      // Arrange
      var context = GetDbContext();
      var controller = new InspectionsController(context);

      // Act
      var result = await controller.InspectionModal(null);

      // Assert
      var partial = Assert.IsType<PartialViewResult>(result);
      Assert.Equal("_InspectionModal", partial.ViewName);
      Assert.IsType<Inspection>(partial.Model);
    }

    [Fact]
    public async Task InspectionModal_Get_ReturnsPartialViewForEdit()
    {
      // Arrange
      var context = GetDbContext();
      var inspection = new Inspection { RoomId = 1, Date = DateTime.Today, Result = "Passed", Notes = "Test" };
      context.Inspections.Add(inspection);
      context.SaveChanges();

      var controller = new InspectionsController(context);

      // Act
      var result = await controller.InspectionModal(inspection.InspectionId);

      // Assert
      var partial = Assert.IsType<PartialViewResult>(result);
      Assert.Equal("_InspectionModal", partial.ViewName);
      var model = Assert.IsType<Inspection>(partial.Model);
      Assert.Equal(inspection.InspectionId, model.InspectionId);
    }

    [Fact]
    public async Task SaveInspection_Post_InvalidModel_ReturnsPartialView()
    {
      // Arrange
      var context = GetDbContext();
      var controller = new InspectionsController(context);
      controller.ModelState.AddModelError("Date", "Required");

      // Act
      var result = await controller.SaveInspection(new Inspection());

      // Assert
      var partial = Assert.IsType<PartialViewResult>(result);
      Assert.Equal("_InspectionModal", partial.ViewName);
      Assert.False(controller.ModelState.IsValid);
    }

    [Fact]
    public async Task SaveInspection_Post_AddsInspection_AndReturnsView()
    {
      // Arrange
      var context = GetDbContext();
      var controller = new InspectionsController(context);
      controller.TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>());
      var inspection = new Inspection
      {
        RoomId = 1,
        Date = DateTime.Today,
        Result = "Passed",
        Notes = "All good"
      };

      // Act
      var result = await controller.SaveInspection(inspection);

      // Assert
      var viewResult = Assert.IsType<ViewResult>(result);
      Assert.Contains("Success", controller.TempData.Keys);
      Assert.Single(context.Inspections.ToList());
    }

    [Fact]
    public async Task SaveInspection_Post_UpdatesInspection_AndReturnsView()
    {
      // Arrange
      var context = GetDbContext();
      var inspection = new Inspection
      {
        RoomId = 1,
        Date = DateTime.Today,
        Result = "Initial",
        Notes = "Initial"
      };
      context.Inspections.Add(inspection);
      context.SaveChanges();

      var controller = new InspectionsController(context);
      controller.TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>());
      var updated = new Inspection
      {
        InspectionId = inspection.InspectionId,
        RoomId = 2,
        Date = DateTime.Today.AddDays(1),
        Result = "Updated",
        Notes = "Updated"
      };

      // Act
      var result = await controller.SaveInspection(updated);

      // Assert
      var viewResult = Assert.IsType<ViewResult>(result);
      Assert.Contains("Success", controller.TempData.Keys);

      var dbInspection = context.Inspections.First();
      Assert.Equal(2, dbInspection.RoomId);
      Assert.Equal("Updated", dbInspection.Result);
      Assert.Equal("Updated", dbInspection.Notes);
    }

    [Fact]
    public async Task Delete_RemovesInspectionAndReturnsView()
    {
      // Arrange
      var context = GetDbContext();
      var inspection = new Inspection { RoomId = 1, Date = DateTime.Today };
      context.Inspections.Add(inspection);
      context.SaveChanges();

      var controller = new InspectionsController(context);
      controller.TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>());

      // Act
      var result = await controller.Delete(inspection.InspectionId);

      // Assert
      var viewResult = Assert.IsType<ViewResult>(result);
      Assert.Contains("Success", controller.TempData.Keys);
      Assert.Empty(context.Inspections);
    }
  }
}