using System;
using System.ComponentModel.DataAnnotations;
using PropertyManagement.Web.ViewModels;

namespace PropertyManagement.Web.ViewModels
{
    public class PaymentViewModel
    {
        public int? PaymentId { get; set; }

        [Required(ErrorMessage = "Tenant is required.")]
        public int TenantId { get; set; }

        [Required(ErrorMessage = "Amount is required.")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than zero.")]
        public decimal Amount { get; set; }

        [Required(ErrorMessage = "Payment type is required.")]
        [RegularExpression("^(Rent|Deposit|Other)$", ErrorMessage = "Payment type must be 'Rent', 'Deposit', or 'Other'.")]
        public string Type { get; set; } = string.Empty;

        [Required(ErrorMessage = "Payment month is required.")]
        [Range(1, 12, ErrorMessage = "Please select a valid month.")]
        public int PaymentMonth { get; set; }

        [Required(ErrorMessage = "Payment year is required.")]
        [Range(2000, 2100, ErrorMessage = "Please select a valid year.")]
        public int PaymentYear { get; set; }

        // Optionally for editing
        public int? LeaseAgreementId { get; set; }

        // For displaying receipt or audit
        public DateTime Date { get; set; }

        // Navigation properties for UI display
        public TenantViewModel? Tenant { get; set; }
        public RoomViewModel? Room { get; set; }
        public LeaseAgreementViewModel? LeaseAgreement { get; set; }
    }
}