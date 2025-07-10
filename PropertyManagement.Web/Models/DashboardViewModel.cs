namespace PropertyManagement.Web.Models
{
    public class DashboardViewModel
    {
        public int TotalRooms { get; set; }
        public int AvailableRooms { get; set; }
        public int OccupiedRooms { get; set; }
        public int UnderMaintenanceRooms { get; set; }
        public int TotalTenants { get; set; }
        public int ActiveLeases { get; set; }
        public int ExpiringLeases { get; set; }
        public int PendingRequests { get; set; }
    }
}
