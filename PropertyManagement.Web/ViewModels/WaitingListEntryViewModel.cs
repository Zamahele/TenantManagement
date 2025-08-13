using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace PropertyManagement.Web.ViewModels;

public class WaitingListEntryViewModel
{
    public int WaitingListId { get; set; }

    [Required(ErrorMessage = "Phone number is required")]
    [DisplayName("Phone Number")]
    [RegularExpression(@"^0[6-8][0-9]{8}$", ErrorMessage = "Phone number must be in South African format (e.g., 0821234567)")]
    public string PhoneNumber { get; set; } = string.Empty;

    [DisplayName("Full Name")]
    [StringLength(100, ErrorMessage = "Full name cannot exceed 100 characters")]
    public string? FullName { get; set; }

    [DisplayName("Email Address")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    [StringLength(100, ErrorMessage = "Email cannot exceed 100 characters")]
    public string? Email { get; set; }

    [DisplayName("Preferred Room Type")]
    public string? PreferredRoomType { get; set; }

    [DisplayName("Maximum Budget")]
    [Range(0, 999999.99, ErrorMessage = "Budget must be between R0 and R999,999.99")]
    [DataType(DataType.Currency)]
    public decimal? MaxBudget { get; set; }

    [DisplayName("Registration Date")]
    [DataType(DataType.DateTime)]
    public DateTime RegisteredDate { get; set; }

    [DisplayName("Last Notified")]
    [DataType(DataType.DateTime)]
    public DateTime? LastNotified { get; set; }

    [Required(ErrorMessage = "Status is required")]
    [DisplayName("Status")]
    public string Status { get; set; } = "Active";

    [DisplayName("Notification Count")]
    public int NotificationCount { get; set; }

    [DisplayName("Notes")]
    [StringLength(500, ErrorMessage = "Notes cannot exceed 500 characters")]
    [DataType(DataType.MultilineText)]
    public string? Notes { get; set; }

    [DisplayName("Source")]
    public string? Source { get; set; }

    [DisplayName("Active")]
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public List<WaitingListNotificationViewModel> Notifications { get; set; } = new List<WaitingListNotificationViewModel>();

    // Display properties for views
    [DisplayName("Registration Date")]
    public string RegisteredDateFormatted => RegisteredDate.ToString("dd/MM/yyyy");

    [DisplayName("Last Notified")]
    public string LastNotifiedFormatted => LastNotified?.ToString("dd/MM/yyyy HH:mm") ?? "Never";

    [DisplayName("Budget")]
    public string BudgetFormatted => MaxBudget?.ToString("C") ?? "Any";

    [DisplayName("Room Type")]
    public string RoomTypeDisplay => PreferredRoomType ?? "Any";

    [DisplayName("Contact")]
    public string ContactDisplay => !string.IsNullOrEmpty(FullName) ? $"{FullName} ({PhoneNumber})" : PhoneNumber;

    // Status badge class for UI
    public string StatusBadgeClass => Status.ToLower() switch
    {
        "active" => "bg-success",
        "notified" => "bg-info",
        "interested" => "bg-primary",
        "converted" => "bg-warning",
        "inactive" => "bg-secondary",
        "optedout" => "bg-danger",
        _ => "bg-secondary"
    };
}