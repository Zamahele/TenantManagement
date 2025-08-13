using System;

namespace PropertyManagement.Domain.Entities
{
    public class WaitingListNotification
    {
        public int NotificationId { get; set; }
        public int WaitingListId { get; set; }
        public int? RoomId { get; set; }
        public DateTime SentDate { get; set; } = DateTime.UtcNow;
        public string MessageContent { get; set; } = string.Empty;
        public string Status { get; set; } = "Sent"; // Sent, Delivered, Failed, Responded
        public string? Response { get; set; } // Interested, NotInterested, Converted
        public DateTime? ResponseDate { get; set; }
        
        // Navigation properties
        public virtual WaitingListEntry WaitingListEntry { get; set; } = null!;
        public virtual Room? Room { get; set; }
    }
}