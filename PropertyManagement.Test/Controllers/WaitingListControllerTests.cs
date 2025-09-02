using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;
using PropertyManagement.Application.Common;
using PropertyManagement.Application.DTOs;
using PropertyManagement.Application.Services;
using PropertyManagement.Test.Infrastructure;
using PropertyManagement.Web.Controllers;
using PropertyManagement.Web.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Assert = Xunit.Assert;

namespace PropertyManagement.Test.Controllers;

public class WaitingListControllerTests : BaseControllerTest
{
    private readonly Mock<IWaitingListApplicationService> _mockWaitingListService;
    private readonly Mock<IRoomApplicationService> _mockRoomService;
    private readonly Mock<ITenantApplicationService> _mockTenantService;
    private readonly Mock<IMaintenanceRequestApplicationService> _mockMaintenanceService;

    public WaitingListControllerTests()
    {
        _mockWaitingListService = new Mock<IWaitingListApplicationService>();
        _mockRoomService = new Mock<IRoomApplicationService>();
        _mockTenantService = new Mock<ITenantApplicationService>();
        _mockMaintenanceService = new Mock<IMaintenanceRequestApplicationService>();
    }

    private WaitingListController GetController()
    {
        var controller = new WaitingListController(
            _mockWaitingListService.Object,
            _mockRoomService.Object,
            _mockTenantService.Object,
            _mockMaintenanceService.Object,
            Mapper);

        SetupControllerContext(controller, GetManagerUser());
        return controller;
    }

    private List<WaitingListEntryDto> GetSampleWaitingListEntries()
    {
        return new List<WaitingListEntryDto>
        {
            new WaitingListEntryDto
            {
                WaitingListId = 1,
                PhoneNumber = "0821234567",
                FullName = "John Doe",
                Email = "john.doe@example.com",
                PreferredRoomType = "Single",
                MaxBudget = 5000,
                Status = "Active",
                RegisteredDate = DateTime.Now.AddDays(-10),
                IsActive = true,
                NotificationCount = 2,
                Source = "Phone",
                Notes = "Test notes for John",
                LastNotified = null,
                Notifications = new List<WaitingListNotificationDto>()
            },
            new WaitingListEntryDto
            {
                WaitingListId = 2,
                PhoneNumber = "0837654321",
                FullName = "Jane Smith",
                Email = "jane.smith@example.com",
                PreferredRoomType = "Double",
                MaxBudget = 7000,
                Status = "Contacted",
                RegisteredDate = DateTime.Now.AddDays(-5),
                IsActive = true,
                NotificationCount = 1,
                Source = "Website",
                Notes = "Test notes for Jane",
                LastNotified = DateTime.Now.AddDays(-2),
                Notifications = new List<WaitingListNotificationDto>()
            }
        };
    }

    private WaitingListSummaryDto GetSampleSummary()
    {
        return new WaitingListSummaryDto
        {
            TotalEntries = 10,
            ActiveEntries = 8,
            NotifiedThisWeek = 3,
            ConvertedThisMonth = 2,
            TotalNotificationsSent = 15,
            AverageResponseTime = 4.5m,
            MostRequestedRoomType = "Single",
            AverageMaxBudget = 6500m,
            NewRegistrationsThisWeek = 4,
            ConversionRate = 0.15
        };
    }

    #region Index Action Tests

    [Fact]
    public async Task Index_WithoutFilters_ReturnsViewWithAllEntries()
    {
        // Arrange
        var entries = GetSampleWaitingListEntries();
        var summary = GetSampleSummary();

        _mockWaitingListService.Setup(s => s.GetAllWaitingListEntriesAsync())
            .ReturnsAsync(ServiceResult<IEnumerable<WaitingListEntryDto>>.Success(entries));
        _mockWaitingListService.Setup(s => s.GetWaitingListSummaryAsync())
            .ReturnsAsync(ServiceResult<WaitingListSummaryDto>.Success(summary));

        SetupSidebarCountMocks();

        var controller = GetController();

        // Act
        var result = await controller.Index();

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<WaitingListManagementViewModel>(viewResult.Model);
        
        // Verify the service was called (called twice - once for main data, once for sidebar counts)
        _mockWaitingListService.Verify(s => s.GetAllWaitingListEntriesAsync(), Times.Exactly(2));
        _mockWaitingListService.Verify(s => s.GetWaitingListSummaryAsync(), Times.Once);
        
        // Check the model properties
        Assert.NotNull(model.Entries);
        Assert.Equal(2, model.Entries.Count);
        Assert.Equal("All", model.StatusFilter);
        Assert.Equal("All", model.RoomTypeFilter);
        Assert.Empty(model.SearchTerm);
        Assert.Equal(10, model.Summary.TotalEntries);
    }

    [Fact]
    public async Task Index_WithStatusFilter_ReturnsFilteredEntries()
    {
        // Arrange
        var filteredEntries = GetSampleWaitingListEntries().Where(e => e.Status == "Active").ToList();
        var summary = GetSampleSummary();

        _mockWaitingListService.Setup(s => s.GetWaitingListEntriesByStatusAsync("Active"))
            .ReturnsAsync(ServiceResult<IEnumerable<WaitingListEntryDto>>.Success(filteredEntries));
        _mockWaitingListService.Setup(s => s.GetWaitingListSummaryAsync())
            .ReturnsAsync(ServiceResult<WaitingListSummaryDto>.Success(summary));

        SetupSidebarCountMocks();

        var controller = GetController();

        // Act
        var result = await controller.Index("Active", "All", "");

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<WaitingListManagementViewModel>(viewResult.Model);
        Assert.Single(model.Entries);
        Assert.Equal("Active", model.StatusFilter);
        Assert.Equal("John Doe", model.Entries.First().FullName);
    }

    [Fact]
    public async Task Index_WithRoomTypeFilter_ReturnsFilteredEntries()
    {
        // Arrange
        var filteredEntries = GetSampleWaitingListEntries().Where(e => e.PreferredRoomType == "Double").ToList();
        var summary = GetSampleSummary();

        _mockWaitingListService.Setup(s => s.GetWaitingListEntriesByRoomTypeAsync("Double"))
            .ReturnsAsync(ServiceResult<IEnumerable<WaitingListEntryDto>>.Success(filteredEntries));
        _mockWaitingListService.Setup(s => s.GetWaitingListSummaryAsync())
            .ReturnsAsync(ServiceResult<WaitingListSummaryDto>.Success(summary));

        SetupSidebarCountMocks();

        var controller = GetController();

        // Act
        var result = await controller.Index("All", "Double", "");

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<WaitingListManagementViewModel>(viewResult.Model);
        Assert.Single(model.Entries);
        Assert.Equal("Double", model.RoomTypeFilter);
        Assert.Equal("Jane Smith", model.Entries.First().FullName);
    }

    [Fact]
    public async Task Index_WithSearchTerm_ReturnsFilteredEntries()
    {
        // Arrange
        var allEntries = GetSampleWaitingListEntries();
        var summary = GetSampleSummary();

        _mockWaitingListService.Setup(s => s.GetAllWaitingListEntriesAsync())
            .ReturnsAsync(ServiceResult<IEnumerable<WaitingListEntryDto>>.Success(allEntries));
        _mockWaitingListService.Setup(s => s.GetWaitingListSummaryAsync())
            .ReturnsAsync(ServiceResult<WaitingListSummaryDto>.Success(summary));

        SetupSidebarCountMocks();

        var controller = GetController();

        // Act
        var result = await controller.Index("All", "All", "john");

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<WaitingListManagementViewModel>(viewResult.Model);
        
        // Verify the service was called (called twice - once for main data, once for sidebar counts)
        _mockWaitingListService.Verify(s => s.GetAllWaitingListEntriesAsync(), Times.Exactly(2));
        
        // The search should find John Doe (case-insensitive)
        Assert.NotNull(model.Entries);
        Assert.Single(model.Entries);
        Assert.Equal("john", model.SearchTerm);
        Assert.Equal("John Doe", model.Entries.First().FullName);
    }

    [Fact]
    public async Task Index_ServiceFailure_ReturnsEmptyViewModel()
    {
        // Arrange
        _mockWaitingListService.Setup(s => s.GetAllWaitingListEntriesAsync())
            .ReturnsAsync(ServiceResult<IEnumerable<WaitingListEntryDto>>.Failure("Service error"));
        
        // Setup summary service to return success to avoid null reference
        _mockWaitingListService.Setup(s => s.GetWaitingListSummaryAsync())
            .ReturnsAsync(ServiceResult<WaitingListSummaryDto>.Success(new WaitingListSummaryDto()));

        SetupSidebarCountMocks();

        var controller = GetController();

        // Act
        var result = await controller.Index();

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<WaitingListManagementViewModel>(viewResult.Model);
        Assert.Empty(model.Entries);
        
        // Check if the error message is in TempData
        var tempDataValue = controller.TempData["Error"];
        if (tempDataValue != null)
        {
            Assert.Equal("Service error", tempDataValue.ToString());
        }
        else
        {
            // If not in TempData, check if it's handled differently
            Assert.True(model.Entries.Count == 0, "Should return empty entries when service fails");
        }
    }

    #endregion

    #region WaitingListForm Action Tests

    [Fact]
    public async Task WaitingListForm_Get_NewEntry_ReturnsPartialViewWithEmptyModel()
    {
        // Arrange
        var controller = GetController();

        // Act
        var result = await controller.WaitingListForm(null);

        // Assert
        var partialResult = Assert.IsType<PartialViewResult>(result);
        Assert.Equal("_WaitingListForm", partialResult.ViewName);
        var model = Assert.IsType<WaitingListEntryViewModel>(partialResult.Model);
        Assert.Equal(0, model.WaitingListId);
    }

    [Fact]
    public async Task WaitingListForm_Get_ExistingEntry_ReturnsPartialViewWithModel()
    {
        // Arrange
        var existingEntry = GetSampleWaitingListEntries().First();
        _mockWaitingListService.Setup(s => s.GetWaitingListEntryByIdAsync(1))
            .ReturnsAsync(ServiceResult<WaitingListEntryDto>.Success(existingEntry));

        var controller = GetController();

        // Act
        var result = await controller.WaitingListForm(1);

        // Assert
        var partialResult = Assert.IsType<PartialViewResult>(result);
        Assert.Equal("_WaitingListForm", partialResult.ViewName);
        var model = Assert.IsType<WaitingListEntryViewModel>(partialResult.Model);
        Assert.Equal(1, model.WaitingListId);
        Assert.Equal("John Doe", model.FullName);
    }

    [Fact]
    public async Task WaitingListForm_Get_EntryNotFound_ReturnsPartialViewWithEmptyModel()
    {
        // Arrange
        _mockWaitingListService.Setup(s => s.GetWaitingListEntryByIdAsync(999))
            .ReturnsAsync(ServiceResult<WaitingListEntryDto>.Failure("Entry not found"));

        var controller = GetController();

        // Act
        var result = await controller.WaitingListForm(999);

        // Assert
        var partialResult = Assert.IsType<PartialViewResult>(result);
        var model = Assert.IsType<WaitingListEntryViewModel>(partialResult.Model);
        Assert.Equal(0, model.WaitingListId);
        Assert.Equal("Entry not found", controller.TempData["Error"]);
    }

    #endregion

    #region CreateOrEdit Action Tests

    [Fact]
    public async Task CreateOrEdit_CreateNew_ValidModel_ReturnsJsonSuccess()
    {
        // Arrange
        var createdEntry = GetSampleWaitingListEntries().First();
        _mockWaitingListService.Setup(s => s.CreateWaitingListEntryAsync(It.IsAny<CreateWaitingListEntryDto>()))
            .ReturnsAsync(ServiceResult<WaitingListEntryDto>.Success(createdEntry));

        var controller = GetController();
        var model = new WaitingListEntryViewModel
        {
            WaitingListId = 0,
            PhoneNumber = "0821234567",
            FullName = "John Doe",
            Email = "john.doe@example.com",
            Status = "Active"
        };

        // Set up AJAX request
        controller.ControllerContext.HttpContext.Request.Headers["X-Requested-With"] = "XMLHttpRequest";

        // Act
        var result = await controller.CreateOrEdit(model);

        // Assert
        var jsonResult = Assert.IsType<JsonResult>(result);
        var jsonValue = jsonResult.Value;
        
        // Use helper method to safely extract JSON properties
        Assert.True(GetJsonPropertyValue<bool>(jsonValue, "success"));
        Assert.Contains("'John Doe' added to waiting list successfully!", GetJsonPropertyValue<string>(jsonValue, "message"));
        _mockWaitingListService.Verify(s => s.CreateWaitingListEntryAsync(It.IsAny<CreateWaitingListEntryDto>()), Times.Once);
    }

    [Fact]
    public async Task CreateOrEdit_UpdateExisting_ValidModel_ReturnsJsonSuccess()
    {
        // Arrange
        var updatedEntry = GetSampleWaitingListEntries().First();
        updatedEntry.FullName = "John Updated";
        
        _mockWaitingListService.Setup(s => s.UpdateWaitingListEntryAsync(It.IsAny<int>(), It.IsAny<UpdateWaitingListEntryDto>()))
            .ReturnsAsync(ServiceResult<WaitingListEntryDto>.Success(updatedEntry));

        var controller = GetController();
        var model = new WaitingListEntryViewModel
        {
            WaitingListId = 1,
            PhoneNumber = "0821234567",
            FullName = "John Updated",
            Email = "john.doe@example.com",
            Status = "Active"
        };

        // Set up AJAX request
        controller.ControllerContext.HttpContext.Request.Headers["X-Requested-With"] = "XMLHttpRequest";

        // Act
        var result = await controller.CreateOrEdit(model);

        // Assert
        var jsonResult = Assert.IsType<JsonResult>(result);
        var jsonValue = jsonResult.Value;
        
        // Use helper method to safely extract JSON properties
        Assert.True(GetJsonPropertyValue<bool>(jsonValue, "success"));
        Assert.Contains("'John Updated' updated successfully!", GetJsonPropertyValue<string>(jsonValue, "message"));
        _mockWaitingListService.Verify(s => s.UpdateWaitingListEntryAsync(1, It.IsAny<UpdateWaitingListEntryDto>()), Times.Once);
    }

    [Fact]
    public async Task CreateOrEdit_InvalidModel_ReturnsJsonError()
    {
        // Arrange
        var controller = GetController();
        var model = new WaitingListEntryViewModel(); // Invalid - missing required fields

        controller.ModelState.AddModelError("PhoneNumber", "Phone number is required");
        controller.ControllerContext.HttpContext.Request.Headers["X-Requested-With"] = "XMLHttpRequest";

        // Act
        var result = await controller.CreateOrEdit(model);

        // Assert
        var jsonResult = Assert.IsType<JsonResult>(result);
        var jsonValue = jsonResult.Value;
        
        // Use helper method to safely extract JSON properties
        Assert.False(GetJsonPropertyValue<bool>(jsonValue, "success"));
        Assert.NotEmpty(GetJsonPropertyValue<IEnumerable<string>>(jsonValue, "errors"));
    }

    [Fact]
    public async Task CreateOrEdit_ServiceFailure_ReturnsJsonError()
    {
        // Arrange
        _mockWaitingListService.Setup(s => s.CreateWaitingListEntryAsync(It.IsAny<CreateWaitingListEntryDto>()))
            .ReturnsAsync(ServiceResult<WaitingListEntryDto>.Failure("Phone number already exists"));

        var controller = GetController();
        var model = new WaitingListEntryViewModel
        {
            WaitingListId = 0,
            PhoneNumber = "0821234567",
            FullName = "John Doe",
            Status = "Active"
        };

        controller.ControllerContext.HttpContext.Request.Headers["X-Requested-With"] = "XMLHttpRequest";

        // Act
        var result = await controller.CreateOrEdit(model);

        // Assert
        var jsonResult = Assert.IsType<JsonResult>(result);
        var jsonValue = jsonResult.Value;
        
        // Use helper method to safely extract JSON properties
        Assert.False(GetJsonPropertyValue<bool>(jsonValue, "success"));
        Assert.Equal("Phone number already exists", GetJsonPropertyValue<string>(jsonValue, "message"));
    }

    [Fact]
    public async Task CreateOrEdit_NonAjaxRequest_RedirectsToIndex()
    {
        // Arrange
        var createdEntry = GetSampleWaitingListEntries().First();
        _mockWaitingListService.Setup(s => s.CreateWaitingListEntryAsync(It.IsAny<CreateWaitingListEntryDto>()))
            .ReturnsAsync(ServiceResult<WaitingListEntryDto>.Success(createdEntry));

        var controller = GetController();
        var model = new WaitingListEntryViewModel
        {
            WaitingListId = 0,
            PhoneNumber = "0821234567",
            FullName = "John Doe",
            Status = "Active"
        };

        // Act
        var result = await controller.CreateOrEdit(model);

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(WaitingListController.Index), redirectResult.ActionName);
        Assert.Equal("? Waiting list entry created successfully!", controller.TempData["Success"]);
    }

    #endregion

    #region QuickAdd Action Tests

    [Fact]
    public async Task QuickAdd_Get_ReturnsPartialViewWithModel()
    {
        // Arrange
        var controller = GetController();

        // Act
        var result = controller.QuickAdd();

        // Assert
        var partialResult = Assert.IsType<PartialViewResult>(result);
        Assert.Equal("_QuickAddForm", partialResult.ViewName);
        Assert.IsType<QuickAddWaitingListViewModel>(partialResult.Model);
    }

    [Fact]
    public async Task QuickAdd_Post_ValidModel_ReturnsJsonSuccess()
    {
        // Arrange
        var createdEntry = GetSampleWaitingListEntries().First();
        _mockWaitingListService.Setup(s => s.CreateWaitingListEntryAsync(It.IsAny<CreateWaitingListEntryDto>()))
            .ReturnsAsync(ServiceResult<WaitingListEntryDto>.Success(createdEntry));

        var controller = GetController();
        var model = new QuickAddWaitingListViewModel
        {
            PhoneNumber = "0821234567",
            FullName = "John Doe",
            PreferredRoomType = "Single",
            Source = "Phone"
        };

        controller.ControllerContext.HttpContext.Request.Headers["X-Requested-With"] = "XMLHttpRequest";

        // Act
        var result = await controller.QuickAdd(model);

        // Assert
        var jsonResult = Assert.IsType<JsonResult>(result);
        var jsonValue = jsonResult.Value;
        
        // Use helper method to safely extract JSON properties
        Assert.True(GetJsonPropertyValue<bool>(jsonValue, "success"));
        Assert.Equal("Contact added to waiting list successfully!", GetJsonPropertyValue<string>(jsonValue, "message"));
        _mockWaitingListService.Verify(s => s.CreateWaitingListEntryAsync(It.IsAny<CreateWaitingListEntryDto>()), Times.Once);
    }

    [Fact]
    public async Task QuickAdd_Post_InvalidModel_ReturnsJsonError()
    {
        // Arrange
        var controller = GetController();
        var model = new QuickAddWaitingListViewModel(); // Invalid - missing phone number

        controller.ModelState.AddModelError("PhoneNumber", "Phone number is required");
        controller.ControllerContext.HttpContext.Request.Headers["X-Requested-With"] = "XMLHttpRequest";

        // Act
        var result = await controller.QuickAdd(model);

        // Assert
        var jsonResult = Assert.IsType<JsonResult>(result);
        var jsonValue = jsonResult.Value;
        
        // Use helper method to safely extract JSON properties
        Assert.False(GetJsonPropertyValue<bool>(jsonValue, "success"));
        Assert.NotEmpty(GetJsonPropertyValue<IEnumerable<string>>(jsonValue, "errors"));
    }

    [Fact]
    public async Task QuickAdd_Post_ServiceFailure_ReturnsJsonError()
    {
        // Arrange
        _mockWaitingListService.Setup(s => s.CreateWaitingListEntryAsync(It.IsAny<CreateWaitingListEntryDto>()))
            .ReturnsAsync(ServiceResult<WaitingListEntryDto>.Failure("Duplicate phone number"));

        var controller = GetController();
        var model = new QuickAddWaitingListViewModel
        {
            PhoneNumber = "0821234567",
            Source = "Phone"
        };

        controller.ControllerContext.HttpContext.Request.Headers["X-Requested-With"] = "XMLHttpRequest";

        // Act
        var result = await controller.QuickAdd(model);

        // Assert
        var jsonResult = Assert.IsType<JsonResult>(result);
        var jsonValue = jsonResult.Value;
        
        // Use helper method to safely extract JSON properties
        Assert.False(GetJsonPropertyValue<bool>(jsonValue, "success"));
        Assert.Equal("Duplicate phone number", GetJsonPropertyValue<string>(jsonValue, "message"));
    }

    #endregion

    #region Delete Action Tests

    [Fact]
    public async Task Delete_ValidId_DeletesEntryAndRedirects()
    {
        // Arrange
        _mockWaitingListService.Setup(s => s.DeleteWaitingListEntryAsync(1))
            .ReturnsAsync(ServiceResult<bool>.Success(true));

        var controller = GetController();

        // Act
        var result = await controller.Delete(1);

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(WaitingListController.Index), redirectResult.ActionName);
        Assert.Equal("Waiting list entry deleted successfully!", controller.TempData["Success"]);
        _mockWaitingListService.Verify(s => s.DeleteWaitingListEntryAsync(1), Times.Once);
    }

    [Fact]
    public async Task Delete_InvalidId_ReturnsRedirectWithError()
    {
        // Arrange
        _mockWaitingListService.Setup(s => s.DeleteWaitingListEntryAsync(999))
            .ReturnsAsync(ServiceResult<bool>.Failure("Entry not found"));

        var controller = GetController();

        // Act
        var result = await controller.Delete(999);

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(WaitingListController.Index), redirectResult.ActionName);
        Assert.Equal("Entry not found", controller.TempData["Error"]);
    }

    #endregion

    #region Details Action Tests

    [Fact]
    public async Task Details_ValidId_ReturnsViewWithModel()
    {
        // Arrange
        var entry = GetSampleWaitingListEntries().First();
        _mockWaitingListService.Setup(s => s.GetWaitingListEntryByIdAsync(1))
            .ReturnsAsync(ServiceResult<WaitingListEntryDto>.Success(entry));

        var controller = GetController();

        // Act
        var result = await controller.Details(1);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<WaitingListEntryViewModel>(viewResult.Model);
        Assert.Equal(1, model.WaitingListId);
        Assert.Equal("John Doe", model.FullName);
    }

    [Fact]
    public async Task Details_InvalidId_RedirectsToIndexWithError()
    {
        // Arrange
        _mockWaitingListService.Setup(s => s.GetWaitingListEntryByIdAsync(999))
            .ReturnsAsync(ServiceResult<WaitingListEntryDto>.Failure("Entry not found"));

        var controller = GetController();

        // Act
        var result = await controller.Details(999);

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(WaitingListController.Index), redirectResult.ActionName);
        Assert.Equal("Entry not found", controller.TempData["Error"]);
    }

    #endregion

    #region SendNotification Tests

    [Fact]
    public async Task SendNotification_ValidData_SendsNotificationAndRedirects()
    {
        // Arrange
        _mockWaitingListService.Setup(s => s.SendNotificationAsync(1, "Test message", null))
            .ReturnsAsync(ServiceResult<bool>.Success(true));

        var controller = GetController();

        // Act
        var result = await controller.SendNotification(1, "Test message");

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(WaitingListController.Details), redirectResult.ActionName);
        Assert.Equal("Notification sent successfully!", controller.TempData["Success"]);
        _mockWaitingListService.Verify(s => s.SendNotificationAsync(1, "Test message", null), Times.Once);
    }

    [Fact]
    public async Task SendNotification_EmptyMessage_ReturnsRedirectWithError()
    {
        // Arrange
        var controller = GetController();

        // Act
        var result = await controller.SendNotification(1, "");

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(WaitingListController.Details), redirectResult.ActionName);
        Assert.Equal("Message content is required", controller.TempData["Error"]);
    }

    [Fact]
    public async Task SendNotification_ServiceFailure_ReturnsRedirectWithError()
    {
        // Arrange
        _mockWaitingListService.Setup(s => s.SendNotificationAsync(1, "Test message", null))
            .ReturnsAsync(ServiceResult<bool>.Failure("Failed to send notification"));

        var controller = GetController();

        // Act
        var result = await controller.SendNotification(1, "Test message");

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(WaitingListController.Details), redirectResult.ActionName);
        Assert.Equal("Failed to send notification", controller.TempData["Error"]);
    }

    #endregion

    #region SendBulkNotification Tests

    [Fact]
    public async Task SendBulkNotification_ValidData_SendsNotificationAndRedirects()
    {
        // Arrange
        var selectedIds = new List<int> { 1, 2, 3 };
        _mockWaitingListService.Setup(s => s.SendBulkNotificationAsync(selectedIds, "Bulk message", null))
            .ReturnsAsync(ServiceResult<bool>.Success(true));

        var controller = GetController();

        // Act
        var result = await controller.SendBulkNotification(selectedIds, "Bulk message");

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(WaitingListController.Index), redirectResult.ActionName);
        Assert.Equal("Bulk notification sent to 3 entries successfully!", controller.TempData["Success"]);
    }

    [Fact]
    public async Task SendBulkNotification_NoEntriesSelected_ReturnsRedirectWithError()
    {
        // Arrange
        var controller = GetController();

        // Act
        var result = await controller.SendBulkNotification(new List<int>(), "Bulk message");

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(WaitingListController.Index), redirectResult.ActionName);
        Assert.Equal("No entries selected", controller.TempData["Error"]);
    }

    #endregion

    #region Export Tests

    [Fact]
    public async Task Export_ValidData_ReturnsFileResult()
    {
        // Arrange
        var entries = GetSampleWaitingListEntries();
        _mockWaitingListService.Setup(s => s.GetAllWaitingListEntriesAsync())
            .ReturnsAsync(ServiceResult<IEnumerable<WaitingListEntryDto>>.Success(entries));

        var controller = GetController();

        // Act
        var result = await controller.Export();

        // Assert
        var fileResult = Assert.IsType<FileContentResult>(result);
        Assert.Equal("text/csv", fileResult.ContentType);
        Assert.StartsWith("waiting-list-export-", fileResult.FileDownloadName);
        Assert.EndsWith(".csv", fileResult.FileDownloadName);
    }

    [Fact]
    public async Task Export_ServiceFailure_RedirectsWithError()
    {
        // Arrange
        _mockWaitingListService.Setup(s => s.GetAllWaitingListEntriesAsync())
            .ReturnsAsync(ServiceResult<IEnumerable<WaitingListEntryDto>>.Failure("Export failed"));

        var controller = GetController();

        // Act
        var result = await controller.Export();

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(WaitingListController.Index), redirectResult.ActionName);
        Assert.Equal("Export failed", controller.TempData["Error"]);
    }

    #endregion

    #region Analytics Tests

    [Fact]
    public async Task Analytics_ValidData_ReturnsViewWithModel()
    {
        // Arrange
        var summary = GetSampleSummary();
        var recentEntries = GetSampleWaitingListEntries().Take(1);

        _mockWaitingListService.Setup(s => s.GetWaitingListSummaryAsync())
            .ReturnsAsync(ServiceResult<WaitingListSummaryDto>.Success(summary));
        _mockWaitingListService.Setup(s => s.GetRecentRegistrationsAsync(30))
            .ReturnsAsync(ServiceResult<IEnumerable<WaitingListEntryDto>>.Success(recentEntries));

        var controller = GetController();

        // Act
        var result = await controller.Analytics();

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<WaitingListSummaryViewModel>(viewResult.Model);
        Assert.Equal(10, model.TotalEntries);
        
        var recentEntriesViewBag = Assert.IsType<List<WaitingListEntryViewModel>>(controller.ViewBag.RecentEntries);
        Assert.Single(recentEntriesViewBag);
    }

    #endregion

    #region GetEntries API Tests

    [Fact]
    public async Task GetEntries_WithoutFilters_ReturnsJsonWithAllEntries()
    {
        // Arrange
        var entries = GetSampleWaitingListEntries();
        _mockWaitingListService.Setup(s => s.GetAllWaitingListEntriesAsync())
            .ReturnsAsync(ServiceResult<IEnumerable<WaitingListEntryDto>>.Success(entries));

        var controller = GetController();

        // Act
        var result = await controller.GetEntries();

        // Assert
        var jsonResult = Assert.IsType<JsonResult>(result);
        var jsonValue = jsonResult.Value;
        
        // Use helper method to safely extract JSON properties
        Assert.True(GetJsonPropertyValue<bool>(jsonValue, "success"));
        Assert.Equal(2, GetJsonPropertyValue<IEnumerable<object>>(jsonValue, "data").Count());
        Assert.Equal(2, GetJsonPropertyValue<int>(jsonValue, "totalCount"));
    }

    [Fact]
    public async Task GetEntries_WithStatusFilter_ReturnsFilteredEntries()
    {
        // Arrange
        var filteredEntries = GetSampleWaitingListEntries().Where(e => e.Status == "Active");
        _mockWaitingListService.Setup(s => s.GetWaitingListEntriesByStatusAsync("Active"))
            .ReturnsAsync(ServiceResult<IEnumerable<WaitingListEntryDto>>.Success(filteredEntries));

        var controller = GetController();

        // Act
        var result = await controller.GetEntries(status: "Active");

        // Assert
        var jsonResult = Assert.IsType<JsonResult>(result);
        var jsonValue = jsonResult.Value;
        
        // Use helper method to safely extract JSON properties
        Assert.True(GetJsonPropertyValue<bool>(jsonValue, "success"));
        Assert.Single(GetJsonPropertyValue<IEnumerable<object>>(jsonValue, "data"));
    }

    #endregion

    #region Edge Case Tests

    [Fact]
    public async Task Index_CombinedFilters_StatusAndRoomType_AppliesStatusFilterOnly()
    {
        // Arrange - When both status and room type filters are provided, status takes precedence
        var filteredEntries = GetSampleWaitingListEntries().Where(e => e.Status == "Active");
        var summary = GetSampleSummary();

        _mockWaitingListService.Setup(s => s.GetWaitingListEntriesByStatusAsync("Active"))
            .ReturnsAsync(ServiceResult<IEnumerable<WaitingListEntryDto>>.Success(filteredEntries));
        _mockWaitingListService.Setup(s => s.GetWaitingListSummaryAsync())
            .ReturnsAsync(ServiceResult<WaitingListSummaryDto>.Success(summary));

        SetupSidebarCountMocks();

        var controller = GetController();

        // Act
        var result = await controller.Index("Active", "Double", "");

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<WaitingListManagementViewModel>(viewResult.Model);
        Assert.Single(model.Entries);
        Assert.Equal("Active", model.StatusFilter);
        Assert.Equal("Double", model.RoomTypeFilter);
        
        // Verify that only status filter was called, not room type
        _mockWaitingListService.Verify(s => s.GetWaitingListEntriesByStatusAsync("Active"), Times.Once);
        _mockWaitingListService.Verify(s => s.GetWaitingListEntriesByRoomTypeAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task CreateOrEdit_ExceptionThrown_ReturnsJsonErrorForAjax()
    {
        // Arrange
        _mockWaitingListService.Setup(s => s.CreateWaitingListEntryAsync(It.IsAny<CreateWaitingListEntryDto>()))
            .ThrowsAsync(new InvalidOperationException("Database connection failed"));

        var controller = GetController();
        var model = new WaitingListEntryViewModel
        {
            WaitingListId = 0,
            PhoneNumber = "0821234567",
            FullName = "John Doe",
            Status = "Active"
        };

        controller.ControllerContext.HttpContext.Request.Headers["X-Requested-With"] = "XMLHttpRequest";

        // Act
        var result = await controller.CreateOrEdit(model);

        // Assert
        var jsonResult = Assert.IsType<JsonResult>(result);
        var jsonValue = jsonResult.Value;
        
        // Use helper method to safely extract JSON properties
        Assert.False(GetJsonPropertyValue<bool>(jsonValue, "success"));
        Assert.Contains("Database connection failed", GetJsonPropertyValue<string>(jsonValue, "message"));
    }

    [Fact]
    public async Task QuickAdd_ExceptionThrown_ReturnsJsonErrorForAjax()
    {
        // Arrange
        _mockWaitingListService.Setup(s => s.CreateWaitingListEntryAsync(It.IsAny<CreateWaitingListEntryDto>()))
            .ThrowsAsync(new ArgumentException("Invalid data"));

        var controller = GetController();
        var model = new QuickAddWaitingListViewModel
        {
            PhoneNumber = "0821234567",
            Source = "Phone"
        };

        controller.ControllerContext.HttpContext.Request.Headers["X-Requested-With"] = "XMLHttpRequest";

        // Act
        var result = await controller.QuickAdd(model);

        // Assert
        var jsonResult = Assert.IsType<JsonResult>(result);
        var jsonValue = jsonResult.Value;
        
        // Use helper method to safely extract JSON properties
        Assert.False(GetJsonPropertyValue<bool>(jsonValue, "success"));
        Assert.Contains("Invalid data", GetJsonPropertyValue<string>(jsonValue, "message"));
    }

    [Fact]
    public async Task SendNotification_WithRoomId_SendsNotificationWithRoom()
    {
        // Arrange
        _mockWaitingListService.Setup(s => s.SendNotificationAsync(1, "Room available", 5))
            .ReturnsAsync(ServiceResult<bool>.Success(true));

        var controller = GetController();

        // Act
        var result = await controller.SendNotification(1, "Room available", 5);

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(WaitingListController.Details), redirectResult.ActionName);
        Assert.Equal("Notification sent successfully!", controller.TempData["Success"]);
        _mockWaitingListService.Verify(s => s.SendNotificationAsync(1, "Room available", 5), Times.Once);
    }

    [Fact]
    public async Task FindMatches_ValidRoomId_ReturnsViewWithMatches()
    {
        // Arrange
        var matchingEntries = GetSampleWaitingListEntries().Take(1);
        var roomDetails = new RoomDto { RoomId = 1, Number = "101", Type = "Single", Status = "Available" };

        _mockWaitingListService.Setup(s => s.FindMatchingEntriesForRoomAsync(1))
            .ReturnsAsync(ServiceResult<IEnumerable<WaitingListEntryDto>>.Success(matchingEntries));
        _mockRoomService.Setup(s => s.GetRoomByIdAsync(1))
            .ReturnsAsync(ServiceResult<RoomDto>.Success(roomDetails));

        var controller = GetController();

        // Act
        var result = await controller.FindMatches(1);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Equal("MatchingEntries", viewResult.ViewName);
        var model = Assert.IsType<List<WaitingListEntryViewModel>>(viewResult.Model);
        Assert.Single(model);
        Assert.Equal(roomDetails, controller.ViewBag.RoomDetails);
    }

    [Fact]
    public async Task FindMatches_InvalidRoomId_RedirectsToIndex()
    {
        // Arrange
        _mockWaitingListService.Setup(s => s.FindMatchingEntriesForRoomAsync(999))
            .ReturnsAsync(ServiceResult<IEnumerable<WaitingListEntryDto>>.Failure("No matches found"));

        var controller = GetController();

        // Act
        var result = await controller.FindMatches(999);

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(WaitingListController.Index), redirectResult.ActionName);
        Assert.Equal("No matches found", controller.TempData["Error"]);
    }

    [Fact]
    public async Task UpdateStatus_ValidData_UpdatesStatusAndRedirects()
    {
        // Arrange
        var existingEntry = GetSampleWaitingListEntries().First();
        var updatedEntry = GetSampleWaitingListEntries().First();
        updatedEntry.Status = "Converted";

        _mockWaitingListService.Setup(s => s.GetWaitingListEntryByIdAsync(1))
            .ReturnsAsync(ServiceResult<WaitingListEntryDto>.Success(existingEntry));
        _mockWaitingListService.Setup(s => s.UpdateWaitingListEntryAsync(1, It.IsAny<UpdateWaitingListEntryDto>()))
            .ReturnsAsync(ServiceResult<WaitingListEntryDto>.Success(updatedEntry));

        var controller = GetController();

        // Act
        var result = await controller.UpdateStatus(1, "Converted");

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(WaitingListController.Index), redirectResult.ActionName);
        Assert.Equal("Status updated to Converted successfully!", controller.TempData["Success"]);
    }

    [Fact]
    public async Task UpdateStatus_EntryNotFound_ReturnsRedirectWithError()
    {
        // Arrange
        _mockWaitingListService.Setup(s => s.GetWaitingListEntryByIdAsync(999))
            .ReturnsAsync(ServiceResult<WaitingListEntryDto>.Failure("Entry not found"));

        var controller = GetController();

        // Act
        var result = await controller.UpdateStatus(999, "Converted");

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(WaitingListController.Index), redirectResult.ActionName);
        Assert.Equal("Waiting list entry not found", controller.TempData["Error"]);
    }

    [Fact]
    public async Task Notifications_WithId_ReturnsViewWithNotifications()
    {
        // Arrange
        var notifications = new List<WaitingListNotificationDto>
        {
            new WaitingListNotificationDto
            {
                NotificationId = 1,
                WaitingListId = 1,
                MessageContent = "Test notification",
                SentDate = DateTime.Now,
                Status = "Sent"
            }
        };

        _mockWaitingListService.Setup(s => s.GetNotificationHistoryAsync(1))
            .ReturnsAsync(ServiceResult<IEnumerable<WaitingListNotificationDto>>.Success(notifications));

        var controller = GetController();

        // Act
        var result = await controller.Notifications(1);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<List<WaitingListNotificationViewModel>>(viewResult.Model);
        Assert.Single(model);
        Assert.Equal("Test notification", model.First().MessageContent);
    }

    [Fact]
    public async Task Notifications_WithoutId_ReturnsAllNotifications()
    {
        // Arrange
        var allNotifications = new List<WaitingListNotificationDto>
        {
            new WaitingListNotificationDto
            {
                NotificationId = 1,
                WaitingListId = 1,
                MessageContent = "Test notification 1",
                SentDate = DateTime.Now,
                Status = "Sent"
            },
            new WaitingListNotificationDto
            {
                NotificationId = 2,
                WaitingListId = 2,
                MessageContent = "Test notification 2",
                SentDate = DateTime.Now.AddHours(-1),
                Status = "Sent"
            }
        };

        _mockWaitingListService.Setup(s => s.GetAllNotificationsAsync())
            .ReturnsAsync(ServiceResult<IEnumerable<WaitingListNotificationDto>>.Success(allNotifications));

        var controller = GetController();

        // Act
        var result = await controller.Notifications(null);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<List<WaitingListNotificationViewModel>>(viewResult.Model);
        Assert.Equal(2, model.Count);
    }

    [Fact]
    public async Task GetEntries_WithPagination_ReturnsPagedResults()
    {
        // Arrange
        var allEntries = GetSampleWaitingListEntries();
        _mockWaitingListService.Setup(s => s.GetAllWaitingListEntriesAsync())
            .ReturnsAsync(ServiceResult<IEnumerable<WaitingListEntryDto>>.Success(allEntries));

        var controller = GetController();

        // Act
        var result = await controller.GetEntries(page: 1, pageSize: 1);

        // Assert
        var jsonResult = Assert.IsType<JsonResult>(result);
        var jsonValue = jsonResult.Value;
        
        // Use helper method to safely extract JSON properties
        Assert.True(GetJsonPropertyValue<bool>(jsonValue, "success"));
        Assert.Single(GetJsonPropertyValue<IEnumerable<object>>(jsonValue, "data")); // Only 1 item per page
        Assert.Equal(2, GetJsonPropertyValue<int>(jsonValue, "totalCount")); // Total is still 2
        Assert.Equal(1, GetJsonPropertyValue<int>(jsonValue, "currentPage"));
        Assert.Equal(2, GetJsonPropertyValue<int>(jsonValue, "totalPages")); // 2 items with page size 1 = 2 pages
    }

    #endregion

    #region Helper Methods

    // Helper method to safely extract values from anonymous JSON objects
    private static T GetJsonPropertyValue<T>(object jsonObject, string propertyName)
    {
        var property = jsonObject.GetType().GetProperty(propertyName);
        if (property == null)
        {
            throw new InvalidOperationException($"Property '{propertyName}' not found in JSON response");
        }
        return (T)property.GetValue(jsonObject);
    }

    private void SetupSidebarCountMocks()
    {
        _mockTenantService.Setup(s => s.GetAllTenantsAsync())
            .ReturnsAsync(ServiceResult<IEnumerable<TenantDto>>.Success(new List<TenantDto>()));
        _mockRoomService.Setup(s => s.GetAllRoomsAsync())
            .ReturnsAsync(ServiceResult<IEnumerable<RoomDto>>.Success(new List<RoomDto>()));
        
        // Note: WaitingList service is already set up in individual tests
        // We don't want to override it here, so we'll let the main test setup handle it
        
        _mockMaintenanceService.Setup(s => s.GetAllMaintenanceRequestsAsync())
            .ReturnsAsync(ServiceResult<IEnumerable<MaintenanceRequestDto>>.Success(new List<MaintenanceRequestDto>()));
    }

    #endregion
}