namespace PropertyManagement.Web.ViewModels
{
    public class DashboardViewModel
    {
        // Property Overview
        public int TotalRooms { get; set; }
        public int AvailableRooms { get; set; }
        public int OccupiedRooms { get; set; }
        public int UnderMaintenanceRooms { get; set; }
        
        // Tenant & Leasing
        public int TotalTenants { get; set; }
        public int ActiveLeases { get; set; }
        public int ExpiringLeases { get; set; }
        public int PendingRequests { get; set; }
        
        // Financial Metrics
        public decimal TotalMonthlyRent { get; set; }
        public decimal CollectedRent { get; set; }
        public decimal OutstandingRent { get; set; }
        public int TenantsWithOutstandingBalance { get; set; }
        
        // Operational Metrics
        public int MaintenanceRequestsToday { get; set; }
        public int InspectionsDueThisWeek { get; set; }
        public int NewBookingsThisWeek { get; set; }
        public int RoomsNeedingAttention { get; set; }
        
        // Performance Indicators
        public double OccupancyRate { get; set; }
        public double CollectionRate { get; set; }
        public double MaintenanceResponseRate { get; set; }
        
        // Recent Activity (for activity feed)
        public List<RecentActivityItem> RecentActivities { get; set; } = new();
        
        // Quick Actions
        public List<QuickActionItem> QuickActions { get; set; } = new();
        
        // Alerts & Notifications
        public List<AlertItem> Alerts { get; set; } = new();
    }
    
    public class RecentActivityItem
    {
        public string Type { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public string Icon { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
    }
    
    public class QuickActionItem
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public string Color { get; set; } = "primary";
    }
    
    public class AlertItem
    {
        public string Type { get; set; } = string.Empty; // info, warning, danger, success
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}
