using PropertyManagement.Domain.Entities;

namespace PropertyManagement.Application.DTOs
{
    public class LeaseTemplateDto
    {
        public int LeaseTemplateId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string HtmlContent { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsActive { get; set; }
        public bool IsDefault { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string? TemplateVariables { get; set; }
    }

    public class CreateLeaseTemplateDto
    {
        public string Name { get; set; } = string.Empty;
        public string HtmlContent { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsActive { get; set; } = true;
        public bool IsDefault { get; set; } = false;
        public string? TemplateVariables { get; set; }
    }

    public class UpdateLeaseTemplateDto
    {
        public string Name { get; set; } = string.Empty;
        public string HtmlContent { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsActive { get; set; }
        public bool IsDefault { get; set; }
        public string? TemplateVariables { get; set; }
    }

    public class DigitalSignatureDto
    {
        public int DigitalSignatureId { get; set; }
        public int LeaseAgreementId { get; set; }
        public int TenantId { get; set; }
        public DateTime SignedDate { get; set; }
        public string SignatureImagePath { get; set; } = string.Empty;
        public string SignerIPAddress { get; set; } = string.Empty;
        public string SignerUserAgent { get; set; } = string.Empty;
        public string? SigningNotes { get; set; }
        public string SignatureHash { get; set; } = string.Empty;
        public bool IsVerified { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        
        // Navigation properties
        public TenantDto? Tenant { get; set; }
    }

    public class CreateDigitalSignatureDto
    {
        public int LeaseAgreementId { get; set; }
        public int TenantId { get; set; }
        public string SignatureImagePath { get; set; } = string.Empty;
        public string SignerIPAddress { get; set; } = string.Empty;
        public string SignerUserAgent { get; set; } = string.Empty;
        public string? SigningNotes { get; set; }
    }

    public class GenerateLeaseDto
    {
        public int LeaseAgreementId { get; set; }
        public int? LeaseTemplateId { get; set; } // If null, use default template
        public bool GeneratePdf { get; set; } = true;
        public bool SendToTenant { get; set; } = false;
    }

    public class SignLeaseDto
    {
        public int LeaseAgreementId { get; set; }
        public string SignatureDataUrl { get; set; } = string.Empty; // Base64 image data
        public string? SigningNotes { get; set; }
        public string SignerIPAddress { get; set; } = string.Empty;
        public string SignerUserAgent { get; set; } = string.Empty;
    }

    public class LeaseAgreementSigningDto
    {
        public int LeaseAgreementId { get; set; }
        public string TenantName { get; set; } = string.Empty;
        public string RoomNumber { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public decimal RentAmount { get; set; }
        public int ExpectedRentDay { get; set; }
        public string? GeneratedHtmlContent { get; set; }
        public string? GeneratedPdfPath { get; set; }
        public LeaseAgreement.LeaseStatus Status { get; set; }
        public bool RequiresDigitalSignature { get; set; }
        public bool IsDigitallySigned { get; set; }
        public DateTime? SignedAt { get; set; }
        public List<DigitalSignatureDto> DigitalSignatures { get; set; } = new();
    }
}