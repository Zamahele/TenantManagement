using PropertyManagement.Domain.Entities;

namespace PropertyManagement.Application.DTOs;

public class LeaseAgreementDto
{
    public int LeaseAgreementId { get; set; }
    public int TenantId { get; set; }
    public int RoomId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal RentAmount { get; set; }
    public int ExpectedRentDay { get; set; }
    public string? FilePath { get; set; }
    
    // Digital Lease Properties (MISSING - this was the issue!)
    public LeaseAgreement.LeaseStatus Status { get; set; } = LeaseAgreement.LeaseStatus.Draft;
    public string? GeneratedHtmlContent { get; set; }
    public string? GeneratedPdfPath { get; set; }
    public int? LeaseTemplateId { get; set; }
    public DateTime? GeneratedAt { get; set; }
    public DateTime? SentToTenantAt { get; set; }
    public DateTime? SignedAt { get; set; }
    public DateTime? LastModifiedAt { get; set; }
    
    // Digital Signature Properties
    public bool RequiresDigitalSignature { get; set; } = true;
    public bool IsDigitallySigned { get; set; } = false;
    
    public TenantDto Tenant { get; set; } = null!;
    public RoomDto Room { get; set; } = null!;
}

public class CreateLeaseAgreementDto
{
    public int TenantId { get; set; }
    public int RoomId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal RentAmount { get; set; }
    public int ExpectedRentDay { get; set; }
    public string? FilePath { get; set; }
}

public class UpdateLeaseAgreementDto
{
    public int TenantId { get; set; }
    public int RoomId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal RentAmount { get; set; }
    public int ExpectedRentDay { get; set; }
    public string? FilePath { get; set; }
}