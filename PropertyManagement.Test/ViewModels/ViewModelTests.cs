using PropertyManagement.Web.ViewModels;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Xunit;

namespace PropertyManagement.Test.ViewModels
{
    public class ViewModelTests
    {
        private static IList<ValidationResult> ValidateModel(object model)
        {
            var validationResults = new List<ValidationResult>();
            var ctx = new ValidationContext(model, null, null);
            Validator.TryValidateObject(model, ctx, validationResults, true);
            return validationResults;
        }

        [Fact]
        public void TenantViewModel_ValidModel_PassesValidation()
        {
            var viewModel = new TenantViewModel
            {
                TenantId = 1,
                UserId = 1,
                RoomId = 1,
                FullName = "John Doe",
                Contact = "1234567890",
                EmergencyContactName = "Jane Doe",
                EmergencyContactNumber = "0987654321"
            };

            var validationResults = ValidateModel(viewModel);
            Assert.Empty(validationResults);
        }

        [Fact]
        public void RoomViewModel_ValidModel_PassesValidation()
        {
            var viewModel = new RoomViewModel
            {
                RoomId = 1,
                Number = "101",
                Type = "Single",
                Status = "Available"
            };

            var validationResults = ValidateModel(viewModel);
            Assert.Empty(validationResults);
        }

        [Fact]
        public void PaymentViewModel_ValidModel_PassesValidation()
        {
            var viewModel = new PaymentViewModel
            {
                PaymentId = 1,
                TenantId = 1,
                LeaseAgreementId = 1,
                Amount = 1000,
                Type = "Rent",
                PaymentMonth = 1,
                PaymentYear = 2024,
                Date = DateTime.Now
            };

            var validationResults = ValidateModel(viewModel);
            Assert.Empty(validationResults);
        }

        [Fact]
        public void LeaseAgreementViewModel_ValidModel_PassesValidation()
        {
            var viewModel = new LeaseAgreementViewModel
            {
                LeaseAgreementId = 1,
                TenantId = 1,
                RoomId = 1,
                StartDate = DateTime.Now,
                EndDate = DateTime.Now.AddMonths(12),
                RentAmount = 1000,
                ExpectedRentDay = 1
            };

            var validationResults = ValidateModel(viewModel);
            Assert.Empty(validationResults);
        }

        [Fact]
        public void MaintenanceRequestViewModel_ValidModel_PassesValidation()
        {
            var viewModel = new MaintenanceRequestViewModel
            {
                MaintenanceRequestId = 1,
                RoomId = 1,
                TenantId = "1",
                Description = "Test maintenance request",
                RequestDate = DateTime.Now,
                Status = "Pending"
            };

            var validationResults = ValidateModel(viewModel);
            Assert.Empty(validationResults);
        }

        [Fact]
        public void BookingRequestViewModel_ValidModel_PassesValidation()
        {
            var viewModel = new BookingRequestViewModel
            {
                BookingRequestId = 1,
                RoomId = 1,
                FullName = "John Doe",
                Contact = "1234567890",
                Status = "Pending",
                RequestDate = DateTime.Now
            };

            var validationResults = ValidateModel(viewModel);
            Assert.Empty(validationResults);
        }

        [Fact]
        public void DashboardViewModel_ValidModel_HasCorrectProperties()
        {
            var viewModel = new DashboardViewModel
            {
                TotalRooms = 10,
                AvailableRooms = 5,
                OccupiedRooms = 3,
                UnderMaintenanceRooms = 2,
                TotalTenants = 8,
                ActiveLeases = 6,
                ExpiringLeases = 2,
                PendingRequests = 4
            };

            Assert.Equal(10, viewModel.TotalRooms);
            Assert.Equal(5, viewModel.AvailableRooms);
            Assert.Equal(3, viewModel.OccupiedRooms);
            Assert.Equal(2, viewModel.UnderMaintenanceRooms);
            Assert.Equal(8, viewModel.TotalTenants);
            Assert.Equal(6, viewModel.ActiveLeases);
            Assert.Equal(2, viewModel.ExpiringLeases);
            Assert.Equal(4, viewModel.PendingRequests);
        }

        [Fact]
        public void ErrorViewModel_ValidModel_HasCorrectProperties()
        {
            var viewModel = new ErrorViewModel
            {
                RequestId = "test-request-id"
            };

            Assert.Equal("test-request-id", viewModel.RequestId);
            Assert.True(viewModel.ShowRequestId);

            viewModel.RequestId = null;
            Assert.False(viewModel.ShowRequestId);
        }

        [Fact]
        public void UserViewModel_ValidModel_PassesValidation()
        {
            var viewModel = new UserViewModel
            {
                UserId = 1,
                Username = "testuser",
                PasswordHash = "hashedpassword123",
                Role = "Tenant"
            };

            var validationResults = ValidateModel(viewModel);
            Assert.Empty(validationResults);
        }

        [Fact]
        public void TenantLoginViewModel_ValidModel_PassesValidation()
        {
            var viewModel = new TenantLoginViewModel
            {
                Username = "testuser",
                Password = "password123"
            };

            var validationResults = ValidateModel(viewModel);
            Assert.Empty(validationResults);
        }

        [Fact]
        public void RoomFormViewModel_ValidModel_PassesValidation()
        {
            var viewModel = new RoomFormViewModel
            {
                RoomId = 1,
                Number = "101",
                Type = "Single",
                Status = "Available"
            };

            var validationResults = ValidateModel(viewModel);
            Assert.Empty(validationResults);
        }

        [Fact]
        public void TenantOutstandingViewModel_ValidModel_HasCorrectProperties()
        {
            var viewModel = new TenantOutstandingViewModel
            {
                TenantId = 1,
                FullName = "John Doe",
                Room = new PropertyManagement.Domain.Entities.Room { RoomId = 1, Number = "101", Type = "Single", Status = "Available" },
                LeaseAgreements = new List<PropertyManagement.Domain.Entities.LeaseAgreement>(),
                Payments = new List<PropertyManagement.Domain.Entities.Payment>()
            };

            Assert.Equal(1, viewModel.TenantId);
            Assert.Equal("John Doe", viewModel.FullName);
            Assert.Equal("101", viewModel.Room.Number);
            Assert.NotNull(viewModel.LeaseAgreements);
            Assert.NotNull(viewModel.Payments);
        }

        [Fact]
        public void RoomsTabViewModel_ValidModel_HasCorrectProperties()
        {
            var rooms = new List<RoomViewModel>
            {
                new RoomViewModel { RoomId = 1, Number = "101", Type = "Single", Status = "Available" },
                new RoomViewModel { RoomId = 2, Number = "102", Type = "Double", Status = "Occupied" }
            };

            var bookingRequests = new List<BookingRequestViewModel>
            {
                new BookingRequestViewModel { BookingRequestId = 1, RoomId = 1, FullName = "John Doe", Status = "Pending", Contact = "123", RequestDate = DateTime.Now }
            };

            var viewModel = new RoomsTabViewModel
            {
                AllRooms = rooms,
                PendingBookingRequests = bookingRequests
            };

            Assert.Equal(2, viewModel.AllRooms.Count);
            Assert.Single(viewModel.PendingBookingRequests);
        }

        [Fact]
        public void DeleteModalViewModel_ValidModel_HasCorrectProperties()
        {
            var viewModel = new DeleteModalViewModel
            {
                EntityId = 1,
                Title = "Delete Item",
                Body = "Are you sure you want to delete this item?",
                Controller = "Test",
                Action = "Delete",
                ModalId = "deleteModal",
                ModalLabelId = "deleteModalLabel"
            };

            Assert.Equal(1, viewModel.EntityId);
            Assert.Equal("Delete Item", viewModel.Title);
            Assert.Equal("Are you sure you want to delete this item?", viewModel.Body);
            Assert.Equal("Test", viewModel.Controller);
            Assert.Equal("Delete", viewModel.Action);
            Assert.Equal("deleteModal", viewModel.ModalId);
            Assert.Equal("deleteModalLabel", viewModel.ModalLabelId);
        }
    }
}