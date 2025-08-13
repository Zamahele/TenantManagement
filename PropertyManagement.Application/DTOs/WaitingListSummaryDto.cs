namespace PropertyManagement.Application.DTOs
{
    public class WaitingListSummaryDto
    {
        public int TotalEntries { get; set; }
        public int ActiveEntries { get; set; }
        public int NotifiedThisWeek { get; set; }
        public int ConvertedThisMonth { get; set; }
        public int TotalNotificationsSent { get; set; }
        public decimal AverageResponseTime { get; set; } // In hours
        public string MostRequestedRoomType { get; set; } = string.Empty;
        public decimal? AverageMaxBudget { get; set; }
        public int NewRegistrationsThisWeek { get; set; }
        public double ConversionRate { get; set; } // Percentage
    }
}