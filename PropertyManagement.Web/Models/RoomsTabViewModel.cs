using PropertyManagement.Domain.Entities;

namespace PropertyManagement.Web.Models
{
  public class RoomsTabViewModel
  {
    public List<Room> AllRooms { get; set; }
    public List<Room> OccupiedRooms { get; set; }
    public List<Room> VacantRooms { get; set; }
    public List<Room> MaintenanceRooms { get; set; }
    public List<BookingRequest> PendingBookingRequests { get; internal set; }
    public int PendingRequestCount { get; internal set; }
  }
}
