using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using PropertyManagement.Application.Common;
using PropertyManagement.Application.DTOs;
using PropertyManagement.Application.Services;
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
    private IMapper GetMapper()
    {
      var expr = new MapperConfigurationExpression();
      expr.CreateMap<InspectionDto, InspectionViewModel>().ReverseMap();
      expr.CreateMap<InspectionViewModel, CreateInspectionDto>();
      expr.CreateMap<InspectionViewModel, UpdateInspectionDto>();
      expr.CreateMap<RoomDto, RoomViewModel>().ReverseMap();
      var config = new MapperConfiguration(expr, NullLoggerFactory.Instance);
      return config.CreateMapper();
    }

    private InspectionsController GetController(
        Mock<IInspectionApplicationService> mockInspectionService,
        Mock<IRoomApplicationService> mockRoomService,
        IMapper mapper)
    {
      var controller = new InspectionsController(
          mockInspectionService.Object,
          mockRoomService.Object,
          mapper
      );

      controller.TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>());
      return controller;
    }

    [Fact]
    public async Task Index_ReturnsViewWithInspections()
    {
      // Arrange
      var mapper = GetMapper();
      var mockInspectionService = new Mock<IInspectionApplicationService>();
      var mockRoomService = new Mock<IRoomApplicationService>();

      var inspections = new List<InspectionDto>
      {
          new InspectionDto 
          { 
              InspectionId = 1, 
              RoomId = 1, 
              Date = DateTime.Today, 
              Result = "Passed", 
              Notes = "Test" 
          }
      };

      mockInspectionService.Setup(s => s.GetAllInspectionsAsync())
          .ReturnsAsync(ServiceResult<IEnumerable<InspectionDto>>.Success(inspections));

      var controller = GetController(mockInspectionService, mockRoomService, mapper);

      // Act
      var result = await controller.Index();

      // Assert
      var viewResult = Assert.IsType<ViewResult>(result);
      var model = Assert.IsAssignableFrom<IEnumerable<InspectionViewModel>>(viewResult.Model);
      Assert.Single(model);
      mockInspectionService.Verify(s => s.GetAllInspectionsAsync(), Times.Once);
    }

    [Fact]
    public async Task Create_ReturnsPartialViewWithModel()
    {
      // Arrange
      var mapper = GetMapper();
      var mockInspectionService = new Mock<IInspectionApplicationService>();
      var mockRoomService = new Mock<IRoomApplicationService>();

      var rooms = new List<RoomDto>
      {
          new RoomDto { RoomId = 1, Number = "101", Type = "Single", Status = "Available" },
          new RoomDto { RoomId = 2, Number = "102", Type = "Double", Status = "Available" }
      };

      mockRoomService.Setup(s => s.GetAllRoomsAsync())
          .ReturnsAsync(ServiceResult<IEnumerable<RoomDto>>.Success(rooms));

      var controller = GetController(mockInspectionService, mockRoomService, mapper);

      // Act
      var result = await controller.Create();

      // Assert
      var partial = Assert.IsType<PartialViewResult>(result);
      Assert.Equal("_InspectionModal", partial.ViewName);
      Assert.IsType<InspectionViewModel>(partial.Model);
      mockRoomService.Verify(s => s.GetAllRoomsAsync(), Times.Once);
    }

    [Fact]
    public async Task Edit_ValidId_ReturnsPartialViewWithModel()
    {
      // Arrange
      var mapper = GetMapper();
      var mockInspectionService = new Mock<IInspectionApplicationService>();
      var mockRoomService = new Mock<IRoomApplicationService>();

      var inspection = new InspectionDto 
      { 
          InspectionId = 1, 
          RoomId = 1, 
          Date = DateTime.Today, 
          Result = "Passed", 
          Notes = "Test" 
      };

      var rooms = new List<RoomDto>
      {
          new RoomDto { RoomId = 1, Number = "101", Type = "Single", Status = "Available" }
      };

      mockInspectionService.Setup(s => s.GetInspectionByIdAsync(1))
          .ReturnsAsync(ServiceResult<InspectionDto>.Success(inspection));
      mockRoomService.Setup(s => s.GetAllRoomsAsync())
          .ReturnsAsync(ServiceResult<IEnumerable<RoomDto>>.Success(rooms));

      var controller = GetController(mockInspectionService, mockRoomService, mapper);

      // Act
      var result = await controller.Edit(1);

      // Assert
      var partial = Assert.IsType<PartialViewResult>(result);
      Assert.Equal("_InspectionModal", partial.ViewName);
      var model = Assert.IsType<InspectionViewModel>(partial.Model);
      Assert.Equal(1, model.InspectionId);
      mockInspectionService.Verify(s => s.GetInspectionByIdAsync(1), Times.Once);
    }

    [Fact]
    public async Task CreateOrEdit_NewInspection_CreatesAndRedirects()
    {
      // Arrange
      var mapper = GetMapper();
      var mockInspectionService = new Mock<IInspectionApplicationService>();
      var mockRoomService = new Mock<IRoomApplicationService>();

      var createdInspection = new InspectionDto
      {
          InspectionId = 1,
          RoomId = 1,
          Date = DateTime.Today,
          Result = "Passed",
          Notes = "All good"
      };

      mockInspectionService.Setup(s => s.CreateInspectionAsync(It.IsAny<CreateInspectionDto>()))
          .ReturnsAsync(ServiceResult<InspectionDto>.Success(createdInspection));

      var controller = GetController(mockInspectionService, mockRoomService, mapper);
      var inspectionVm = new InspectionViewModel
      {
          InspectionId = 0,
          RoomId = 1,
          Date = DateTime.Today,
          Result = "Passed",
          Notes = "All good"
      };

      // Act
      var result = await controller.CreateOrEdit(inspectionVm);

      // Assert
      var redirectResult = Assert.IsType<RedirectToActionResult>(result);
      Assert.Equal("Index", redirectResult.ActionName);
      Assert.Equal("Inspection created successfully.", controller.TempData["Success"]);
      mockInspectionService.Verify(s => s.CreateInspectionAsync(It.IsAny<CreateInspectionDto>()), Times.Once);
    }

    [Fact]
    public async Task CreateOrEdit_ExistingInspection_UpdatesAndRedirects()
    {
      // Arrange
      var mapper = GetMapper();
      var mockInspectionService = new Mock<IInspectionApplicationService>();
      var mockRoomService = new Mock<IRoomApplicationService>();

      var updatedInspection = new InspectionDto
      {
          InspectionId = 1,
          RoomId = 1,
          Date = DateTime.Today,
          Result = "Updated",
          Notes = "Updated notes"
      };

      mockInspectionService.Setup(s => s.UpdateInspectionAsync(It.IsAny<int>(), It.IsAny<UpdateInspectionDto>()))
          .ReturnsAsync(ServiceResult<InspectionDto>.Success(updatedInspection));

      var controller = GetController(mockInspectionService, mockRoomService, mapper);
      var inspectionVm = new InspectionViewModel
      {
          InspectionId = 1,
          RoomId = 1,
          Date = DateTime.Today,
          Result = "Updated",
          Notes = "Updated notes"
      };

      // Act
      var result = await controller.CreateOrEdit(inspectionVm);

      // Assert
      var redirectResult = Assert.IsType<RedirectToActionResult>(result);
      Assert.Equal("Index", redirectResult.ActionName);
      Assert.Equal("Inspection updated successfully.", controller.TempData["Success"]);
      mockInspectionService.Verify(s => s.UpdateInspectionAsync(1, It.IsAny<UpdateInspectionDto>()), Times.Once);
    }

    [Fact]
    public async Task Delete_ValidId_DeletesAndRedirects()
    {
      // Arrange
      var mapper = GetMapper();
      var mockInspectionService = new Mock<IInspectionApplicationService>();
      var mockRoomService = new Mock<IRoomApplicationService>();

      mockInspectionService.Setup(s => s.DeleteInspectionAsync(1))
          .ReturnsAsync(ServiceResult<bool>.Success(true));

      var controller = GetController(mockInspectionService, mockRoomService, mapper);

      // Act
      var result = await controller.Delete(1);

      // Assert
      var redirectResult = Assert.IsType<RedirectToActionResult>(result);
      Assert.Equal("Index", redirectResult.ActionName);
      Assert.Equal("Inspection deleted successfully.", controller.TempData["Success"]);
      mockInspectionService.Verify(s => s.DeleteInspectionAsync(1), Times.Once);
    }

    [Fact]
    public async Task CreateOrEdit_InvalidModel_ReturnsPartialView()
    {
      // Arrange
      var mapper = GetMapper();
      var mockInspectionService = new Mock<IInspectionApplicationService>();
      var mockRoomService = new Mock<IRoomApplicationService>();

      var rooms = new List<RoomDto>
      {
          new RoomDto { RoomId = 1, Number = "101", Type = "Single", Status = "Available" }
      };

      mockRoomService.Setup(s => s.GetAllRoomsAsync())
          .ReturnsAsync(ServiceResult<IEnumerable<RoomDto>>.Success(rooms));

      var controller = GetController(mockInspectionService, mockRoomService, mapper);
      controller.ModelState.AddModelError("Date", "Required");

      var inspectionVm = new InspectionViewModel();

      // Act
      var result = await controller.CreateOrEdit(inspectionVm);

      // Assert
      var partial = Assert.IsType<PartialViewResult>(result);
      Assert.Equal("_InspectionModal", partial.ViewName);
      Assert.False(controller.ModelState.IsValid);
      Assert.Equal("Please correct the errors in the form.", controller.TempData["Error"]);
    }
  }
}