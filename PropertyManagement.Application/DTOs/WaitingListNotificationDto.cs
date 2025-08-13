using System;

namespace PropertyManagement.Application.DTOs
{
    public class WaitingListNotificationDto
    {
        public int NotificationId { get; set; }
        public int WaitingListId { get; set; }
        public int? RoomId { get; set; }
        public DateTime SentDate { get; set; }
        public string MessageContent { get; set; } = string.Empty;
        public string Status { get; set; } = "Sent";
        public string? Response { get; set; }
        public DateTime? ResponseDate { get; set; }
        
        // Navigation properties
        public WaitingListEntryDto? WaitingListEntry { get; set; }
        public RoomDto? Room { get; set; }
    }
}