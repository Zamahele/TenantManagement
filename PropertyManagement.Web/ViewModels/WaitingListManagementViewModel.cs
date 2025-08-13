using System.ComponentModel;

namespace PropertyManagement.Web.ViewModels;

public class WaitingListManagementViewModel
{
    public List<WaitingListEntryViewModel> Entries { get; set; } = new List<WaitingListEntryViewModel>();
    public WaitingListSummaryViewModel Summary { get; set; } = new WaitingListSummaryViewModel();
    
    // Filter options
    [DisplayName("Status Filter")]
    public string? StatusFilter { get; set; }
    
    [DisplayName("Room Type Filter")]
    public string? RoomTypeFilter { get; set; }
    
    [DisplayName("Date Range Filter")]
    public string? DateRangeFilter { get; set; }

    // Bulk operation properties
    public List<int> SelectedEntryIds { get; set; } = new List<int>();
    public string BulkAction { get; set; } = string.Empty;
    public string BulkMessage { get; set; } = string.Empty;
    
    // Dropdown options - populated by controller
    public List<string> StatusOptions { get; set; } = new List<string> 
    { 
        "All", "Active", "Notified", "Interested", "Converted", "Inactive", "OptedOut" 
    };
    
    public List<string> RoomTypeOptions { get; set; } = new List<string> 
    { 
        "All", "Any", "Single", "Double", "Family", "Studio" 
    };
    
    public List<string> DateRangeOptions { get; set; } = new List<string> 
    { 
        "All Time", "Last 7 Days", "Last 30 Days", "Last 90 Days" 
    };
    
    public List<string> BulkActionOptions { get; set; } = new List<string> 
    { 
        "Send Notification", "Update Status", "Export Selected", "Delete Selected" 
    };

    // Pagination properties
    public int CurrentPage { get; set; } = 1;
    public int PageSize { get; set; } = 15;
    public int TotalCount { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasPreviousPage => CurrentPage > 1;
    public bool HasNextPage => CurrentPage < TotalPages;

    // Search properties
    public string SearchTerm { get; set; } = string.Empty;
    public string SearchIn { get; set; } = "All"; // All, Name, Phone, Email

    public List<string> SearchInOptions { get; set; } = new List<string> 
    { 
        "All", "Name", "Phone", "Email", "Notes" 
    };
}