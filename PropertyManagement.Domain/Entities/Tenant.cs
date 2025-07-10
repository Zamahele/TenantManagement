using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
using System.ComponentModel;

namespace PropertyManagement.Domain.Entities
{
  public class Tenant
  {
    public int TenantId { get; set; }

    [Required]
    [DisplayName("Full Name")]
    [RegularExpression(@"^[A-Za-z\s]+$", ErrorMessage = "Full name must contain letters and spaces only.")]
    public string? FullName { get; set; }

    [Required]
   //[RegularExpression(@"^0(6|7|8)[0-9]{8}$", ErrorMessage = "Contact must be a valid South African cellphone number (e.g., 0821234567).")]
    public string? Contact { get; set; }

    [Required]
    public int RoomId { get; set; }

    public int UserId { get; set; }
    public User User { get; set; }

    // Emergency contact fields
    [Required]
    [DisplayName("Emergency Name")]
    public string? EmergencyContactName { get; set; }

    [Required]
    [DisplayName("Emergency No#")]
    //[RegularExpression(@"^0(6|7|8)[0-9]{8}$", ErrorMessage = "Emergency contact must be a valid South African cellphone number.")]
    public string? EmergencyContactNumber { get; set; }

    // Navigation property
    public Room? Room { get; set; }

    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();
    public virtual ICollection<LeaseAgreement> LeaseAgreements { get; set; } = new List<LeaseAgreement>();
  }
}