using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PropertyManagement.Domain.Entities
{
  public class Payment
  {
    public int PaymentId { get; set; }

    // Make LeaseAgreementId nullable to avoid FK issues if not always set
    public int? LeaseAgreementId { get; set; }
    [ForeignKey("LeaseAgreementId")]
    public LeaseAgreement? LeaseAgreement { get; set; }

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }

    [Required]
    public DateTime Date { get; set; }

    [Required]
    public string? Type { get; set; } // "Rent", "Deposit", etc.

    public string? ReceiptPath { get; set; }

    [Required]
    [Range(1, 12, ErrorMessage = "Please select a valid month.")]
    public int PaymentMonth { get; set; }

    [Required]
    [Range(2000, 2100, ErrorMessage = "Please select a valid year.")]
    public int PaymentYear { get; set; }

    [Required]
    public int TenantId { get; set; }
    public Tenant? Tenant { get; set; }
  }
}