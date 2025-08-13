using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace PropertyManagement.Web.ViewModels;

public class QuickAddWaitingListViewModel
{
    [Required(ErrorMessage = "Phone number is required")]
    [DisplayName("Phone Number")]
    [RegularExpression(@"^0[6-8][0-9]{8}$", ErrorMessage = "Phone number must be in South African format (e.g., 0821234567)")]
    public string PhoneNumber { get; set; } = string.Empty;

    [DisplayName("Name (optional)")]
    [StringLength(100, ErrorMessage = "Name cannot exceed 100 characters")]
    public string? FullName { get; set; }

    [DisplayName("Room Type Preference")]
    public string? PreferredRoomType { get; set; } = "Any";

    [DisplayName("Maximum Budget (optional)")]
    [Range(0, 999999.99, ErrorMessage = "Budget must be between R0 and R999,999.99")]
    [DataType(DataType.Currency)]
    public decimal? MaxBudget { get; set; }

    [DisplayName("Notes")]
    [StringLength(500, ErrorMessage = "Notes cannot exceed 500 characters")]
    [DataType(DataType.MultilineText)]
    public string? Notes { get; set; }

    [DisplayName("Source")]
    public string Source { get; set; } = "Phone";

    // Options for dropdowns - populated by controller
    public List<string> RoomTypeOptions { get; set; } = new List<string> 
    { 
        "Any", "Single", "Double", "Family", "Studio" 
    };

    public List<string> SourceOptions { get; set; } = new List<string> 
    { 
        "Phone", "Website", "Walk-in", "Referral", "Social Media" 
    };
}