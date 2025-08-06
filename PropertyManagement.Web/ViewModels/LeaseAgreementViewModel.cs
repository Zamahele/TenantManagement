using System;
using Microsoft.AspNetCore.Http;
using PropertyManagement.Domain.Entities;

namespace PropertyManagement.Web.ViewModels
{
  public class LeaseAgreementViewModel
  {
    public int LeaseAgreementId { get; set; }
    public int TenantId { get; set; }
    public int RoomId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal RentAmount { get; set; }
    public int ExpectedRentDay { get; set; }
    public string? FilePath { get; set; }
    public IFormFile? File { get; set; }
    public DateTime? RentDueDate { get; set; }
    public TenantViewModel? Tenant { get; set; }
    public RoomViewModel? Room { get; set; }

    // Digital Lease Properties
    public int? LeaseTemplateId { get; set; }
    public string? GeneratedHtmlContent { get; set; }
    public string? GeneratedPdfPath { get; set; }
    public DateTime? GeneratedAt { get; set; }
    public DateTime? LastModifiedAt { get; set; }
    
    // Status and Signing Properties
    public LeaseAgreement.LeaseStatus Status { get; set; } = LeaseAgreement.LeaseStatus.Draft;
    public DateTime? SentToTenantAt { get; set; }
    public DateTime? SignedAt { get; set; }
    public bool RequiresDigitalSignature { get; set; } = true;
    public bool IsDigitallySigned { get; set; } = false;

    // Navigation for Digital Signatures
    public List<DigitalSignatureViewModel> DigitalSignatures { get; set; } = new();
  }
}