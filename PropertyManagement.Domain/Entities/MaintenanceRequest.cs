using System;

namespace PropertyManagement.Domain.Entities
{
    public class MaintenanceRequest
    {
        public int MaintenanceRequestId { get; set; }
        public int RoomId { get; set; }
        public string TenantId { get; set; } // Or int, depending on your user model
        public string Description { get; set; }
        public DateTime RequestDate { get; set; }
        public string Status { get; set; } // e.g., "Pending", "In Progress", "Completed"
        public string? AssignedTo { get; set; } // Property manager or staff
        public DateTime? CompletedDate { get; set; }

        // Navigation properties
        public Room? Room { get; set; }
        // Add Tenant navigation if you have a Tenant entity
    }
}