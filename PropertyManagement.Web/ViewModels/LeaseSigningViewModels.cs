using PropertyManagement.Application.DTOs;
using System.ComponentModel.DataAnnotations;

namespace PropertyManagement.Web.ViewModels
{
    public class LeaseSigningViewModel
    {
        public LeaseAgreementSigningDto LeaseAgreement { get; set; } = new();
        public int TenantId { get; set; }
        public string? SigningNotes { get; set; }
    }

    public class LeaseTemplateViewModel
    {
        public int LeaseTemplateId { get; set; }

        [Required]
        [MaxLength(100)]
        [Display(Name = "Template Name")]
        public string Name { get; set; } = string.Empty;

        [Required]
        [Display(Name = "HTML Content")]
        public string HtmlContent { get; set; } = string.Empty;

        [Display(Name = "Description")]
        public string? Description { get; set; }

        [Display(Name = "Is Active")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Is Default Template")]
        public bool IsDefault { get; set; } = false;

        [Display(Name = "Template Variables (JSON)")]
        public string? TemplateVariables { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class DigitalSignatureViewModel
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
        public TenantViewModel? Tenant { get; set; }
    }

    public class LeaseStatusViewModel
    {
        public int LeaseAgreementId { get; set; }
        public string Status { get; set; } = string.Empty;
        public string StatusDisplayName { get; set; } = string.Empty;
        public string StatusColor { get; set; } = string.Empty;
        public bool CanSign { get; set; }
        public bool CanDownload { get; set; }
        public DateTime? GeneratedAt { get; set; }
        public DateTime? SentAt { get; set; }
        public DateTime? SignedAt { get; set; }
    }
}