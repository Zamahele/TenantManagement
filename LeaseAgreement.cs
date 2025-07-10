using System;

namespace PropertyManagement.Domain.Entities
{
    public class LeaseAgreement
    {
        public int Id { get; set; }
        public int TenantId { get; set; }
        public string FilePath { get; set; } // Path to the uploaded digital copy
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        // Navigation property
        public Tenant Tenant { get; set; }
    }
}