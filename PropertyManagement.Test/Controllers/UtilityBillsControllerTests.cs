using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;
using PropertyManagement.Domain.Entities;
using PropertyManagement.Infrastructure.Data;
using PropertyManagement.Web.Controllers;
using Xunit;
using Assert = Xunit.Assert;

namespace PropertyManagement.Test.Controllers
{
  public class UtilityBillsControllerTests
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

    private UtilityBillsController GetController(ApplicationDbContext context, decimal waterRate = 0.02m, decimal electricityRate = 1.50m)
    {
      var inMemorySettings = new Dictionary<string, string> {
                {"UtilityRates:WaterPerLiter", waterRate.ToString(System.Globalization.CultureInfo.InvariantCulture)},
                {"UtilityRates:ElectricityPerKwh", electricityRate.ToString(System.Globalization.CultureInfo.InvariantCulture)}
            };
      var configuration = new ConfigurationBuilder()
          .AddInMemoryCollection(inMemorySettings)
          .Build();

      var controller = new UtilityBillsController(context, configuration);
      controller.TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>());
      return controller;
    }

    [Fact]
    public async Task Index_ReturnsViewWithUtilityBills()
    {
      // Arrange
      var context = GetDbContext();
      context.UtilityBills.Add(new UtilityBill
      {
        RoomId = 1,
        BillingDate = DateTime.Today,
        WaterUsage = 10,
        ElectricityUsage = 20,
        TotalAmount = 100
      });
      context.SaveChanges();

      var controller = GetController(context);

      // Act
      var result = await controller.Index();

      // Assert
      var viewResult = Assert.IsType<ViewResult>(result);
      var model = Assert.IsAssignableFrom<IEnumerable<UtilityBill>>(viewResult.Model);
      Assert.Single(model);
    }

    [Fact]
    public async Task UtilityBillModal_Get_ReturnsPartialViewForAdd()
    {
      // Arrange
      var context = GetDbContext();
      var controller = GetController(context);

      // Act
      var result = await controller.UtilityBillModal(null);

      // Assert
      var partial = Assert.IsType<PartialViewResult>(result);
      Assert.Equal("_UtilityBillModal", partial.ViewName);
      Assert.IsType<UtilityBill>(partial.Model);
    }

    [Fact]
    public async Task UtilityBillModal_Get_ReturnsPartialViewForEdit()
    {
      // Arrange
      var context = GetDbContext();
      var bill = new UtilityBill
      {
        RoomId = 1,
        BillingDate = DateTime.Today,
        WaterUsage = 10,
        ElectricityUsage = 20,
        TotalAmount = 100,
        Notes = "Test"
      };
      context.UtilityBills.Add(bill);
      context.SaveChanges();

      var controller = GetController(context);

      // Act
      var result = await controller.UtilityBillModal(bill.UtilityBillId);

      // Assert
      var partial = Assert.IsType<PartialViewResult>(result);
      Assert.Equal("_UtilityBillModal", partial.ViewName);
      var model = Assert.IsType<UtilityBill>(partial.Model);
      Assert.Equal(bill.UtilityBillId, model.UtilityBillId);
    }

    [Fact]
    public async Task SaveUtilityBill_Post_InvalidModel_ReturnsPartialView()
    {
      // Arrange
      var context = GetDbContext();
      var controller = GetController(context);
      controller.ModelState.AddModelError("BillingDate", "Required");

      // Act
      var result = await controller.SaveUtilityBill(new UtilityBill());

      // Assert
      var partial = Assert.IsType<PartialViewResult>(result);
      Assert.Equal("_UtilityBillModal", partial.ViewName);
      Assert.False(controller.ModelState.IsValid);
    }

    [Fact]
    public async Task SaveUtilityBill_Post_AddsUtilityBill_AndAutoCalculatesTotal_AndReturnsView()
    {
      // Arrange
      var context = GetDbContext();
      var waterRate = 0.02m;
      var electricityRate = 1.50m;
      var controller = GetController(context, waterRate, electricityRate);
      var bill = new UtilityBill
      {
        RoomId = 1,
        BillingDate = DateTime.Today,
        WaterUsage = 10,
        ElectricityUsage = 20,
        Notes = "Test"
      };

      // Act
      var result = await controller.SaveUtilityBill(bill);

      // Assert
      var viewResult = Assert.IsType<ViewResult>(result);
      Assert.Contains("Success", controller.TempData.Keys);
      Assert.Single(context.UtilityBills.ToList());
      var saved = context.UtilityBills.First();
      var expectedTotal = (bill.WaterUsage * waterRate) + (bill.ElectricityUsage * electricityRate);
      Assert.Equal(expectedTotal, saved.TotalAmount);
    }

    [Fact]
    public async Task SaveUtilityBill_Post_UpdatesUtilityBill_AndAutoCalculatesTotal_AndReturnsView()
    {
      // Arrange
      var context = GetDbContext();
      var waterRate = 0.02m;
      var electricityRate = 1.50m;
      var bill = new UtilityBill
      {
        RoomId = 1,
        BillingDate = DateTime.Today,
        WaterUsage = 10,
        ElectricityUsage = 20,
        TotalAmount = 100,
        Notes = "Initial"
      };
      context.UtilityBills.Add(bill);
      context.SaveChanges();

      var controller = GetController(context, waterRate, electricityRate);
      var updated = new UtilityBill
      {
        UtilityBillId = bill.UtilityBillId,
        RoomId = 2,
        BillingDate = DateTime.Today.AddDays(1),
        WaterUsage = 15,
        ElectricityUsage = 25,
        Notes = "Updated"
      };

      // Act
      var result = await controller.SaveUtilityBill(updated);

      // Assert
      var viewResult = Assert.IsType<ViewResult>(result);
      Assert.Contains("Success", controller.TempData.Keys);

      var dbBill = context.UtilityBills.First();
      var expectedTotal = (updated.WaterUsage * waterRate) + (updated.ElectricityUsage * electricityRate);
      Assert.Equal(2, dbBill.RoomId);
      Assert.Equal(15, dbBill.WaterUsage);
      Assert.Equal(25, dbBill.ElectricityUsage);
      Assert.Equal(expectedTotal, dbBill.TotalAmount);
      Assert.Equal("Updated", dbBill.Notes);
    }

    [Fact]
    public async Task Delete_RemovesUtilityBillAndReturnsView()
    {
      // Arrange
      var context = GetDbContext();
      var bill = new UtilityBill
      {
        RoomId = 1,
        BillingDate = DateTime.Today,
        WaterUsage = 10,
        ElectricityUsage = 20,
        TotalAmount = 100
      };
      context.UtilityBills.Add(bill);
      context.SaveChanges();

      var controller = GetController(context);

      // Act
      var result = await controller.Delete(bill.UtilityBillId);

      // Assert
      var viewResult = Assert.IsType<ViewResult>(result);
      Assert.Contains("Success", controller.TempData.Keys);
      Assert.Empty(context.UtilityBills);
    }
  }
}