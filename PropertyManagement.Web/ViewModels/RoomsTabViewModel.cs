using PropertyManagement.Domain.Entities;

namespace PropertyManagement.Web.ViewModels
{
  public class RoomsTabViewModel
  {
    public List<RoomViewModel> AllRooms { get; set; } = new();
    public List<RoomViewModel> OccupiedRooms { get; set; } = new();
    public List<RoomViewModel> VacantRooms { get; set; } = new();
    public List<RoomViewModel> MaintenanceRooms { get; set; } = new();
    public List<BookingRequestViewModel> PendingBookingRequests { get; set; } = new();
    public int PendingRequestCount { get; set; }
  }
}
