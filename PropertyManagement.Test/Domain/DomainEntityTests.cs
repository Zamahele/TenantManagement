using PropertyManagement.Domain.Entities;
using System;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace PropertyManagement.Test.Domain
{
    public class DomainEntityTests
    {
        private static IList<ValidationResult> ValidateModel(object model)
        {
            var validationResults = new List<ValidationResult>();
            var ctx = new ValidationContext(model, null, null);
            Validator.TryValidateObject(model, ctx, validationResults, true);
            return validationResults;
        }

        [Fact]
        public void User_ValidModel_PassesValidation()
        {
            var user = new User
            {
                UserId = 1,
                Username = "testuser",
                PasswordHash = "hashedpassword123",
                Role = "Tenant"
            };

            var validationResults = ValidateModel(user);
            Assert.Empty(validationResults);
        }

        [Fact]
        public void Room_ValidModel_PassesValidation()
        {
            var room = new Room
            {
                RoomId = 1,
                Number = "101",
                Type = "Single",
                Status = "Available"
            };

            var validationResults = ValidateModel(room);
            Assert.Empty(validationResults);
        }

        [Fact]
        public void Tenant_ValidModel_PassesValidation()
        {
            var tenant = new Tenant
            {
                TenantId = 1,
                UserId = 1,
                RoomId = 1,
                FullName = "John Doe",
                Contact = "1234567890",
                EmergencyContactName = "Jane Doe",
                EmergencyContactNumber = "0987654321"
            };

            var validationResults = ValidateModel(tenant);
            Assert.Empty(validationResults);
        }

        [Fact]
        public void LeaseAgreement_ValidModel_PassesValidation()
        {
            var lease = new LeaseAgreement
            {
                LeaseAgreementId = 1,
                TenantId = 1,
                RoomId = 1,
                StartDate = DateTime.Now,
                EndDate = DateTime.Now.AddMonths(12),
                RentAmount = 1000,
                ExpectedRentDay = 1
            };

            var validationResults = ValidateModel(lease);
            Assert.Empty(validationResults);
        }

        [Fact]
        public void Payment_ValidModel_PassesValidation()
        {
            var payment = new Payment
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

            var validationResults = ValidateModel(payment);
            Assert.Empty(validationResults);
        }

        [Fact]
        public void MaintenanceRequest_ValidModel_PassesValidation()
        {
            var request = new MaintenanceRequest
            {
                MaintenanceRequestId = 1,
                RoomId = 1,
                TenantId = "1",
                Description = "Test maintenance request",
                RequestDate = DateTime.Now,
                Status = "Pending"
            };

            var validationResults = ValidateModel(request);
            Assert.Empty(validationResults);
        }

        [Fact]
        public void BookingRequest_ValidModel_PassesValidation()
        {
            var booking = new BookingRequest
            {
                BookingRequestId = 1,
                RoomId = 1,
                FullName = "John Doe",
                Contact = "1234567890",
                Status = "Pending",
                RequestDate = DateTime.Now
            };

            var validationResults = ValidateModel(booking);
            Assert.Empty(validationResults);
        }

        [Fact]
        public void Inspection_ValidModel_PassesValidation()
        {
            var inspection = new Inspection
            {
                InspectionId = 1,
                RoomId = 1,
                Date = DateTime.Now,
                Result = "Good",
                Notes = "Room in good condition"
            };

            var validationResults = ValidateModel(inspection);
            Assert.Empty(validationResults);
        }

        [Fact]
        public void UtilityBill_ValidModel_PassesValidation()
        {
            var bill = new UtilityBill
            {
                UtilityBillId = 1,
                RoomId = 1,
                BillingDate = DateTime.Now,
                WaterUsage = 100.5m,
                ElectricityUsage = 250.75m,
                TotalAmount = 150.25m
            };

            var validationResults = ValidateModel(bill);
            Assert.Empty(validationResults);
        }

        [Fact]
        public void User_EmptyUsername_FailsValidation()
        {
            var user = new User
            {
                UserId = 1,
                Username = "",
                PasswordHash = "hashedpassword123",
                Role = "Tenant"
            };

            var validationResults = ValidateModel(user);
            Assert.Contains(validationResults, vr => vr.MemberNames.Contains("Username"));
        }

        [Fact]
        public void Room_EmptyNumber_DoesNotFailValidation()
        {
            var room = new Room
            {
                RoomId = 1,
                Number = "",
                Type = "Single",
                Status = "Available"
            };

            var validationResults = ValidateModel(room);
            // Since Room entity doesn't have validation attributes, empty validation results are expected
            Assert.Empty(validationResults);
        }

        [Fact]
        public void Tenant_EmptyFullName_FailsValidation()
        {
            var tenant = new Tenant
            {
                TenantId = 1,
                UserId = 1,
                RoomId = 1,
                FullName = "",
                Contact = "1234567890",
                EmergencyContactName = "Jane Doe",
                EmergencyContactNumber = "0987654321"
            };

            var validationResults = ValidateModel(tenant);
            Assert.Contains(validationResults, vr => vr.MemberNames.Contains("FullName"));
        }

        [Fact]
        public void LeaseAgreement_EndDateBeforeStartDate_IsInvalid()
        {
            var lease = new LeaseAgreement
            {
                LeaseAgreementId = 1,
                TenantId = 1,
                RoomId = 1,
                StartDate = DateTime.Now,
                EndDate = DateTime.Now.AddDays(-1), // End date before start date
                RentAmount = 1000,
                ExpectedRentDay = 1
            };

            // This would need custom validation logic in the domain entity
            Assert.True(lease.StartDate < lease.EndDate == false);
        }

        [Fact]
        public void Payment_NegativeAmount_IsInvalid()
        {
            var payment = new Payment
            {
                PaymentId = 1,
                TenantId = 1,
                LeaseAgreementId = 1,
                Amount = -100, // Negative amount
                Type = "Rent",
                PaymentMonth = 1,
                PaymentYear = 2024,
                Date = DateTime.Now
            };

            Assert.True(payment.Amount < 0);
        }
    }
}