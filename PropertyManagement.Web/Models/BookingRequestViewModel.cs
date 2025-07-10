using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

public class BookingRequestViewModel
{
    public int? BookingRequestId { get; set; }
    [Required]
    public int RoomId { get; set; }
    [Required]
    public string FullName { get; set; }
    [Required]
    public string Contact { get; set; }
    public string? Note { get; set; }
    public string? ProofOfPaymentPath { get; set; }
    public IEnumerable<SelectListItem>? RoomOptions { get; set; }
}