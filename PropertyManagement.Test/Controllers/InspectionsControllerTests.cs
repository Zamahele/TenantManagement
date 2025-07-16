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
using System.Threading.Tasks;
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

    private IMapper GetMapper()
    {
      var expr = new MapperConfigurationExpression();
      expr.CreateMap<Inspection, InspectionViewModel>().ReverseMap();
      expr.CreateMap<Room, RoomViewModel>().ReverseMap();
      var config = new MapperConfiguration(expr, NullLoggerFactory.Instance);
      return config.CreateMapper();
    }

    private InspectionsController GetController(ApplicationDbContext context)
    {
      var inspectionRepo = new Mock<IGenericRepository<Inspection>>();
      var roomRepo = new Mock<IGenericRepository<Room>>();

      inspectionRepo.Setup(r => r.Query()).Returns(context.Inspections);
      inspectionRepo.Setup(r => r.GetByIdAsync(It.IsAny<int>()))
          .ReturnsAsync((int id) => context.Inspections.Find(id));
      inspectionRepo.Setup(r => r.AddAsync(It.IsAny<Inspection>()))
          .Callback((Inspection inspection) => { context.Inspections.Add(inspection); context.SaveChanges(); })
          .Returns(Task.CompletedTask);
      inspectionRepo.Setup(r => r.UpdateAsync(It.IsAny<Inspection>()))
          .Callback((Inspection inspection) => { context.Entry(inspection).State = EntityState.Modified; context.SaveChanges(); })
          .Returns(Task.CompletedTask);
      inspectionRepo.Setup(r => r.DeleteAsync(It.IsAny<Inspection>()))
          .Callback((Inspection inspection) => { context.Inspections.Remove(inspection); context.SaveChanges(); })
          .Returns(Task.CompletedTask);

      roomRepo.Setup(r => r.Query()).Returns(context.Rooms);
      roomRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(() => context.Rooms.ToList());

      var mapper = GetMapper();

      var controller = new InspectionsController(
          inspectionRepo.Object,
          roomRepo.Object,
          mapper
      );

      controller.TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>());
      return controller;
    }

    [Fact]
    public async Task Index_ReturnsViewWithInspections()
    {
      // Arrange
      var context = GetDbContext();
      context.Inspections.Add(new Inspection { InspectionId = 1, RoomId = 1, Date = DateTime.Today });
      context.SaveChanges();

      var controller = GetController(context);

      // Act
      var result = await controller.Index();

      // Assert
      var viewResult = Assert.IsType<ViewResult>(result);
      var model = Assert.IsAssignableFrom<IEnumerable<InspectionViewModel>>(viewResult.Model);
      Assert.Single(model);
    }

    [Fact]
    public async Task InspectionModal_Get_ReturnsPartialViewForAdd()
    {
      // Arrange
      var context = GetDbContext();
      var controller = GetController(context);

      // Act
      var result = await controller.InspectionModal(null);

      // Assert
      var partial = Assert.IsType<PartialViewResult>(result);
      Assert.Equal("_InspectionModal", partial.ViewName);
      Assert.IsType<InspectionViewModel>(partial.Model);
    }

    [Fact]
    public async Task InspectionModal_Get_ReturnsPartialViewForEdit()
    {
      // Arrange
      var context = GetDbContext();
      var inspection = new Inspection { InspectionId = 1, RoomId = 1, Date = DateTime.Today, Result = "Passed", Notes = "Test" };
      context.Inspections.Add(inspection);
      context.SaveChanges();

      var controller = GetController(context);

      // Act
      var result = await controller.InspectionModal(inspection.InspectionId);

      // Assert
      var partial = Assert.IsType<PartialViewResult>(result);
      Assert.Equal("_InspectionModal", partial.ViewName);
      var model = Assert.IsType<InspectionViewModel>(partial.Model);
      Assert.Equal(inspection.InspectionId, model.InspectionId);
    }

    [Fact]
    public async Task SaveInspection_Post_InvalidModel_ReturnsPartialView()
    {
      // Arrange
      var context = GetDbContext();
      var controller = GetController(context);
      controller.ModelState.AddModelError("Date", "Required");

      // Act

      var inspectionvw = GetMapper().Map<InspectionViewModel>(new Inspection());
      var result = await controller.SaveInspection(inspectionvw);

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
      var controller = GetController(context);
      var inspection = new Inspection
      {
        RoomId = 1,
        Date = DateTime.Today,
        Result = "Passed",
        Notes = "All good"
      };

      // Act
      var agreementVm = GetMapper().Map<InspectionViewModel>(inspection);
      var result = await controller.SaveInspection(agreementVm);

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

      var controller = GetController(context);
      var updated = new Inspection
      {
        InspectionId = inspection.InspectionId,
        RoomId = 2,
        Date = DateTime.Today.AddDays(1),
        Result = "Updated",
        Notes = "Updated"
      };

      // Act
      var updatedVm = GetMapper().Map<InspectionViewModel>(updated);
      var result = await controller.SaveInspection(updatedVm);

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

      var controller = GetController(context);

      // Act
      var result = await controller.Delete(inspection.InspectionId);

      // Assert
      var viewResult = Assert.IsType<ViewResult>(result);
      Assert.Contains("Success", controller.TempData.Keys);
      Assert.Empty(context.Inspections);
    }
  }
}