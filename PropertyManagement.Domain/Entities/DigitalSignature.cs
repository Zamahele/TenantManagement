using System.ComponentModel.DataAnnotations;

namespace PropertyManagement.Domain.Entities
{
    public class DigitalSignature
    {
        public int DigitalSignatureId { get; set; }
        
        [Required]
        public int LeaseAgreementId { get; set; }
        public LeaseAgreement LeaseAgreement { get; set; } = null!;
        
        [Required]
        public int TenantId { get; set; }
        public Tenant Tenant { get; set; } = null!;
        
        [Required]
        public DateTime SignedDate { get; set; }
        
        [Required]
        public string SignatureImagePath { get; set; } = string.Empty;
        
        [Required]
        public string SignerIPAddress { get; set; } = string.Empty;
        
        [Required]
        public string SignerUserAgent { get; set; } = string.Empty;
        
        public string? SigningNotes { get; set; }
        
        // Digital signature verification
        [Required]
        public string SignatureHash { get; set; } = string.Empty;
        
        public bool IsVerified { get; set; } = true;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}