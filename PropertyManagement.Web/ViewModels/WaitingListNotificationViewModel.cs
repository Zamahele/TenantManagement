using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace PropertyManagement.Web.ViewModels;

public class WaitingListNotificationViewModel
{
    public int NotificationId { get; set; }
    public int WaitingListId { get; set; }
    public int? RoomId { get; set; }

    [DisplayName("Sent Date")]
    [DataType(DataType.DateTime)]
    public DateTime SentDate { get; set; }

    [Required(ErrorMessage = "Message content is required")]
    [DisplayName("Message")]
    [StringLength(1000, ErrorMessage = "Message cannot exceed 1000 characters")]
    [DataType(DataType.MultilineText)]
    public string MessageContent { get; set; } = string.Empty;

    [Required(ErrorMessage = "Status is required")]
    [DisplayName("Status")]
    public string Status { get; set; } = "Sent";

    [DisplayName("Response")]
    [StringLength(100, ErrorMessage = "Response cannot exceed 100 characters")]
    public string? Response { get; set; }

    [DisplayName("Response Date")]
    [DataType(DataType.DateTime)]
    public DateTime? ResponseDate { get; set; }

    // Navigation properties
    public WaitingListEntryViewModel? WaitingListEntry { get; set; }
    public RoomViewModel? Room { get; set; }

    // Display properties for views
    [DisplayName("Sent")]
    public string SentDateFormatted => SentDate.ToString("dd/MM/yyyy HH:mm");

    [DisplayName("Response Date")]
    public string ResponseDateFormatted => ResponseDate?.ToString("dd/MM/yyyy HH:mm") ?? "-";

    [DisplayName("Room")]
    public string RoomDisplay => Room?.Number ?? (RoomId.HasValue ? $"Room {RoomId}" : "General");

    [DisplayName("Recipient")]
    public string RecipientDisplay => WaitingListEntry?.ContactDisplay ?? "Unknown";

    // Status badge class for UI
    public string StatusBadgeClass => Status.ToLower() switch
    {
        "sent" => "bg-primary",
        "delivered" => "bg-success",
        "failed" => "bg-danger",
        "responded" => "bg-info",
        _ => "bg-secondary"
    };

    // Response badge class for UI
    public string ResponseBadgeClass => Response?.ToLower() switch
    {
        "interested" or "yes" => "bg-success",
        "not interested" or "no" => "bg-danger",
        "stop" or "unsubscribe" => "bg-warning",
        _ => "bg-secondary"
    };
}