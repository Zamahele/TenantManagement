using System.ComponentModel;

namespace PropertyManagement.Web.ViewModels;

public class WaitingListSummaryViewModel
{
    [DisplayName("Total Entries")]
    public int TotalEntries { get; set; }

    [DisplayName("Active Entries")]
    public int ActiveEntries { get; set; }

    [DisplayName("Notified This Week")]
    public int NotifiedThisWeek { get; set; }

    [DisplayName("Converted This Month")]
    public int ConvertedThisMonth { get; set; }

    [DisplayName("Total Notifications Sent")]
    public int TotalNotificationsSent { get; set; }

    [DisplayName("Average Response Time (hours)")]
    public decimal AverageResponseTime { get; set; }

    [DisplayName("Most Requested Room Type")]
    public string MostRequestedRoomType { get; set; } = string.Empty;

    [DisplayName("Average Maximum Budget")]
    public decimal? AverageMaxBudget { get; set; }

    [DisplayName("New Registrations This Week")]
    public int NewRegistrationsThisWeek { get; set; }

    [DisplayName("Conversion Rate")]
    public double ConversionRate { get; set; }

    // Display properties for views
    [DisplayName("Avg Budget")]
    public string AverageMaxBudgetFormatted => AverageMaxBudget?.ToString("C") ?? "N/A";

    [DisplayName("Conversion Rate")]
    public string ConversionRateFormatted => $"{ConversionRate:F1}%";

    [DisplayName("Response Time")]
    public string AverageResponseTimeFormatted => $"{AverageResponseTime:F1} hours";

    // Performance metrics for dashboard cards
    public string ActiveEntriesChange => "+12%"; // TODO: Calculate actual change
    public string ConversionRateChange => "+5.2%"; // TODO: Calculate actual change
    public string NotificationRateChange => "+8.1%"; // TODO: Calculate actual change
}