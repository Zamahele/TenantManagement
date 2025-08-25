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
using PropertyManagement.Application.Services;
using PropertyManagement.Application.Common;
using PropertyManagement.Application.DTOs;
using Xunit;
using Assert = Xunit.Assert;

namespace PropertyManagement.Test.Controllers
{
  public class UtilityBillsControllerTests : TestBaseClass
  {

    private UtilityBillsController GetController(ApplicationDbContext context, decimal waterRate = 0.02m, decimal electricityRate = 1.50m)
    {
      var inMemorySettings = new Dictionary<string, string> {
                {"UtilityRates:WaterPerLiter", waterRate.ToString(System.Globalization.CultureInfo.InvariantCulture)},
                {"UtilityRates:ElectricityPerKwh", electricityRate.ToString(System.Globalization.CultureInfo.InvariantCulture)}
            };
      var configuration = new ConfigurationBuilder()
          .AddInMemoryCollection(inMemorySettings)
          .Build();

      var mockUtilityBillService = new Mock<IUtilityBillApplicationService>();
      var mockRoomService = new Mock<IRoomApplicationService>();
      var mockTenantService = new Mock<ITenantApplicationService>();
      var mockMaintenanceService = new Mock<IMaintenanceRequestApplicationService>();
      var mapper = GetMapper();

      var controller = new UtilityBillsController(
          mockUtilityBillService.Object,
          mockRoomService.Object,
          mockTenantService.Object,
          mockMaintenanceService.Object,
          mapper,
          configuration);
      controller.TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>());
      return controller;
    }

    [Fact]
    public async Task Index_ReturnsViewWithUtilityBills()
    {
      // Arrange
      var mockUtilityBillService = new Mock<IUtilityBillApplicationService>();
      var utilityBills = new List<UtilityBillDto>
      {
        new UtilityBillDto
        {
          UtilityBillId = 1,
          RoomId = 1,
          BillingDate = DateTime.Today,
          WaterUsage = 10,
          ElectricityUsage = 20,
          TotalAmount = 100
        }
      };
      mockUtilityBillService.Setup(s => s.GetAllUtilityBillsAsync())
        .ReturnsAsync(ServiceResult<IEnumerable<UtilityBillDto>>.Success(utilityBills));

      var mockRoomService = new Mock<IRoomApplicationService>();
      var mockTenantService = new Mock<ITenantApplicationService>();
      var mockMaintenanceService = new Mock<IMaintenanceRequestApplicationService>();
      var mapper = GetMapper();
      var inMemorySettings = new Dictionary<string, string> {
        {"UtilityRates:WaterPerLiter", "0.02"},
        {"UtilityRates:ElectricityPerKwh", "1.50"}
      };
      var configuration = new ConfigurationBuilder()
        .AddInMemoryCollection(inMemorySettings)
        .Build();

      var controller = new UtilityBillsController(
        mockUtilityBillService.Object,
        mockRoomService.Object,
        mockTenantService.Object,
        mockMaintenanceService.Object,
        mapper,
        configuration);
      controller.TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>());

      // Act
      var result = await controller.Index();

      // Assert
      var viewResult = Assert.IsType<ViewResult>(result);
      var model = Assert.IsAssignableFrom<IEnumerable<PropertyManagement.Web.ViewModels.UtilityBillViewModel>>(viewResult.Model);
      Assert.Single(model);
    }

    [Fact]
    public async Task UtilityBillModal_Get_ReturnsPartialViewForAdd()
    {
      // Arrange
      var mockRoomService = new Mock<IRoomApplicationService>();
      mockRoomService.Setup(s => s.GetAllRoomsAsync())
        .ReturnsAsync(ServiceResult<IEnumerable<RoomDto>>.Success(new List<RoomDto>()));

      var mockUtilityBillService = new Mock<IUtilityBillApplicationService>();
      var mockTenantService = new Mock<ITenantApplicationService>();
      var mockMaintenanceService = new Mock<IMaintenanceRequestApplicationService>();
      var mapper = GetMapper();
      var inMemorySettings = new Dictionary<string, string> {
        {"UtilityRates:WaterPerLiter", "0.02"},
        {"UtilityRates:ElectricityPerKwh", "1.50"}
      };
      var configuration = new ConfigurationBuilder()
        .AddInMemoryCollection(inMemorySettings)
        .Build();

      var controller = new UtilityBillsController(
        mockUtilityBillService.Object,
        mockRoomService.Object,
        mockTenantService.Object,
        mockMaintenanceService.Object,
        mapper,
        configuration);
      controller.TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>());

      // Act
      var result = await controller.UtilityBillForm(null);

      // Assert
      var partial = Assert.IsType<PartialViewResult>(result);
      Assert.Equal("_UtilityBillForm", partial.ViewName);
      Assert.IsType<PropertyManagement.Web.ViewModels.UtilityBillFormViewModel>(partial.Model);
    }

    [Fact]
    public async Task UtilityBillModal_Get_ReturnsPartialViewForEdit()
    {
      // Arrange
      var billDto = new UtilityBillDto
      {
        UtilityBillId = 1,
        RoomId = 1,
        BillingDate = DateTime.Today,
        WaterUsage = 10,
        ElectricityUsage = 20,
        TotalAmount = 100,
        Notes = "Test"
      };
      var mockUtilityBillService = new Mock<IUtilityBillApplicationService>();
      mockUtilityBillService.Setup(s => s.GetUtilityBillByIdAsync(billDto.UtilityBillId))
        .ReturnsAsync(ServiceResult<UtilityBillDto>.Success(billDto));

      var mockRoomService = new Mock<IRoomApplicationService>();
      mockRoomService.Setup(s => s.GetAllRoomsAsync())
        .ReturnsAsync(ServiceResult<IEnumerable<RoomDto>>.Success(new List<RoomDto>()));

      var mockTenantService = new Mock<ITenantApplicationService>();
      var mockMaintenanceService = new Mock<IMaintenanceRequestApplicationService>();
      var mapper = GetMapper();
      var inMemorySettings = new Dictionary<string, string> {
        {"UtilityRates:WaterPerLiter", "0.02"},
        {"UtilityRates:ElectricityPerKwh", "1.50"}
      };
      var configuration = new ConfigurationBuilder()
        .AddInMemoryCollection(inMemorySettings)
        .Build();

      var controller = new UtilityBillsController(
        mockUtilityBillService.Object,
        mockRoomService.Object,
        mockTenantService.Object,
        mockMaintenanceService.Object,
        mapper,
        configuration);
      controller.TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>());

      // Act
      var result = await controller.UtilityBillForm(billDto.UtilityBillId);

      // Assert
      var partial = Assert.IsType<PartialViewResult>(result);
      Assert.Equal("_UtilityBillForm", partial.ViewName);
      var model = Assert.IsType<PropertyManagement.Web.ViewModels.UtilityBillFormViewModel>(partial.Model);
      Assert.Equal(billDto.UtilityBillId, model.UtilityBillId);
    }

    // TODO: Rewrite tests to work with new Application Service pattern and ViewModels
    /*
    [Fact]
    public async Task SaveUtilityBill_Post_InvalidModel_ReturnsPartialView()
    {
      // Arrange
      var context = GetDbContext();
      var controller = GetController(context);
      controller.ModelState.AddModelError("BillingDate", "Required");

      // Act
      // TODO: Update test to use new ViewModel-based CreateOrEdit method
      // var result = await controller.CreateOrEdit(new UtilityBillFormViewModel());

      // Assert
      var partial = Assert.IsType<PartialViewResult>(result);
      Assert.Equal("_UtilityBillForm", partial.ViewName);
      Assert.False(controller.ModelState.IsValid);
    }
    */

    /*
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
    */

    [Fact]
    public async Task Delete_RemovesUtilityBillAndReturnsView()
    {
      // Arrange
      var billDto = new UtilityBillDto
      {
        UtilityBillId = 1,
        RoomId = 1,
        BillingDate = DateTime.Today,
        WaterUsage = 10,
        ElectricityUsage = 20,
        TotalAmount = 100
      };

      var mockUtilityBillService = new Mock<IUtilityBillApplicationService>();
      mockUtilityBillService.Setup(s => s.GetUtilityBillByIdAsync(billDto.UtilityBillId))
          .ReturnsAsync(ServiceResult<UtilityBillDto>.Success(billDto));
      mockUtilityBillService.Setup(s => s.DeleteUtilityBillAsync(billDto.UtilityBillId))
          .ReturnsAsync(ServiceResult<bool>.Success(true));

      var mockRoomService = new Mock<IRoomApplicationService>();
      var mockTenantService = new Mock<ITenantApplicationService>();
      var mockMaintenanceService = new Mock<IMaintenanceRequestApplicationService>();
      var mapper = GetMapper();
      var inMemorySettings = new Dictionary<string, string> {
          {"UtilityRates:WaterPerLiter", "0.02"},
          {"UtilityRates:ElectricityPerKwh", "1.50"}
      };
      var configuration = new ConfigurationBuilder()
          .AddInMemoryCollection(inMemorySettings)
          .Build();

      var controller = new UtilityBillsController(
          mockUtilityBillService.Object,
          mockRoomService.Object,
          mockTenantService.Object,
          mockMaintenanceService.Object,
          mapper,
          configuration);
      controller.TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>());

      // Act
      var result = await controller.Delete(billDto.UtilityBillId);

      // Assert
      var redirectResult = Assert.IsType<RedirectToActionResult>(result);
      Assert.Equal("Index", redirectResult.ActionName);
      Assert.Contains("Success", controller.TempData.Keys);
    }

    private new AutoMapper.IMapper GetMapper()
    {
        var expr = new AutoMapper.MapperConfigurationExpression();
        expr.CreateMap<UtilityBillDto, PropertyManagement.Web.ViewModels.UtilityBillViewModel>().ReverseMap();
        expr.CreateMap<UtilityBillDto, PropertyManagement.Web.ViewModels.UtilityBillFormViewModel>().ReverseMap();
        expr.CreateMap<MaintenanceRequestDto, PropertyManagement.Web.ViewModels.MaintenanceRequestFormViewModel>().ReverseMap();
        expr.CreateMap<RoomDto, PropertyManagement.Web.ViewModels.RoomViewModel>().ReverseMap();
        
        var config = new AutoMapper.MapperConfiguration(expr, Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);
        return config.CreateMapper();
    }
  }
}