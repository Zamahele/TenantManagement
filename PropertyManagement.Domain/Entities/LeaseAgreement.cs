using Microsoft.AspNetCore.Http;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PropertyManagement.Domain.Entities
{
  public class LeaseAgreement
  {
    public int LeaseAgreementId { get; set; }

    [Required]
    public int TenantId { get; set; }

    [Required]
    public int RoomId { get; set; } // <-- Add this

    [Required]
    [DataType(DataType.Date)]
    public DateTime StartDate { get; set; }

    [Required]
    [DataType(DataType.Date)]
    public DateTime EndDate { get; set; }

    // Not mapped, for file upload only
    [NotMapped]
    public IFormFile? File { get; set; }

    // Add this property for the file path
    public string? FilePath { get; set; }

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal RentAmount { get; set; }

    [Required]
    [Range(1, 31, ErrorMessage = "Expected rent day must be between 1 and 28.")]
    public int ExpectedRentDay { get; set; } // e.g., 1 = 1st of each month

    // Computed property for next rent due date
    [NotMapped]
    public DateTime? RentDueDate
    {
        get
        {
            var today = DateTime.Today;
            // If lease has ended, no due date
            if (EndDate < today)
                return null;

            // Compute the next due date
            var year = today.Year;
            var month = today.Month;

            // If today is past the expected day, move to next month
            if (today.Day > ExpectedRentDay)
            {
                month++;
                if (month > 12)
                {
                    month = 1;
                    year++;
                }
            }

            // Clamp the day to the last day of the month
            var day = Math.Min(ExpectedRentDay, DateTime.DaysInMonth(year, month));
            var dueDate = new DateTime(year, month, day);

            // If due date is before lease start, use start date
            if (dueDate < StartDate)
                dueDate = StartDate;

            // If due date is after lease end, return null
            if (dueDate > EndDate)
                return null;

            return dueDate;
        }
    }

    public Tenant? Tenant { get; set; }
    public Room? Room { get; set; } // <-- Add this
  }
}