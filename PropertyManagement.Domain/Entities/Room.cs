using System.Collections.Generic;

namespace PropertyManagement.Domain.Entities
{
    public class Room
    {
        public int RoomId { get; set; }
        public string Number { get; set; }           // e.g., "101", "A2"
        public string Type { get; set; }             // e.g., "Single", "Double", "Suite"
        public string Status { get; set; }           // e.g., "Available", "Occupied", "Under Maintenance"
        public int? CottageId { get; set; }          // Optional: link to a Cottage entity if you have one

        // Navigation properties
        public ICollection<MaintenanceRequest> MaintenanceRequests { get; set; }
        public ICollection<Tenant> Tenants { get; set; }
    }
}