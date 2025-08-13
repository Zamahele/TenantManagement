using System;
using System.Collections.Generic;

namespace PropertyManagement.Domain.Entities
{
    public class WaitingListEntry
    {
        public int WaitingListId { get; set; }
        public string PhoneNumber { get; set; } = string.Empty;
        public string? FullName { get; set; }
        public string? Email { get; set; }
        public string? PreferredRoomType { get; set; } // Single, Double, Family, Any
        public decimal? MaxBudget { get; set; }
        public DateTime RegisteredDate { get; set; } = DateTime.UtcNow;
        public DateTime? LastNotified { get; set; }
        public string Status { get; set; } = "Active"; // Active, Notified, Converted, Inactive, OptedOut
        public int NotificationCount { get; set; } = 0;
        public string? Notes { get; set; }
        public string? Source { get; set; } // Phone, Website, Walk-in, Referral
        public bool IsActive { get; set; } = true;
        
        // Navigation properties
        public virtual ICollection<WaitingListNotification> Notifications { get; set; } = new List<WaitingListNotification>();
    }
}