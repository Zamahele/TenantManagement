using System;
using System.Collections.Generic;

namespace PropertyManagement.Application.DTOs
{
    public class WaitingListEntryDto
    {
        public int WaitingListId { get; set; }
        public string PhoneNumber { get; set; } = string.Empty;
        public string? FullName { get; set; }
        public string? Email { get; set; }
        public string? PreferredRoomType { get; set; }
        public decimal? MaxBudget { get; set; }
        public DateTime RegisteredDate { get; set; }
        public DateTime? LastNotified { get; set; }
        public string Status { get; set; } = "Active";
        public int NotificationCount { get; set; }
        public string? Notes { get; set; }
        public string? Source { get; set; }
        public bool IsActive { get; set; } = true;
        
        // Navigation properties
        public List<WaitingListNotificationDto> Notifications { get; set; } = new List<WaitingListNotificationDto>();
    }
}