using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace PropertyManagement.Web.ViewModels;

public class BookingRequestViewModel
{
  public int BookingRequestId { get; set; }

  [Required]
  public int RoomId { get; set; }

  [Required]
  public string FullName { get; set; }

  [Required]
  public string Contact { get; set; }

  public DateTime RequestDate { get; set; } = DateTime.Now;

  public bool DepositPaid { get; set; } = false;

  public string Status { get; set; } = "Pending"; // "Pending", "Confirmed", "Rejected"

  public string? ProofOfPaymentPath { get; set; }

  public string? Note { get; set; }

  public RoomViewModel? Room { get; set; }

  public IEnumerable<SelectListItem>? RoomOptions { get; set; }
}