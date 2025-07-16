namespace PropertyManagement.Web.ViewModels
{
    public class RoomViewModel
    {
        public int RoomId { get; set; }
        public string Number { get; set; } = string.Empty;
        public string? Type { get; set; }
        public string? Status { get; set; }
        // Optionally, add more properties as needed for your UI
    }
}