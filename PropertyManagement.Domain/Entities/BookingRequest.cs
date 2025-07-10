using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PropertyManagement.Domain.Entities
{
  public class BookingRequest
  {
      public int BookingRequestId { get; set; }
      public int RoomId { get; set; }
      public string FullName { get; set; }
      public string Contact { get; set; }
      public DateTime RequestDate { get; set; } = DateTime.Now;
      public bool DepositPaid { get; set; } = false;
      public string Status { get; set; } = "Pending"; // "Pending", "Confirmed", "Rejected"
      public string? ProofOfPaymentPath { get; set; }
      public string? Note { get; set; } // <-- Add this line
      public Room Room { get; set; }
  }
}
