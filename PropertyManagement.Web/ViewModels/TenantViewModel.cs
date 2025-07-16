using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace PropertyManagement.Web.ViewModels;

public class TenantViewModel
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
  public UserViewModel User { get; set; } // Made non-nullable to match the entity

  // Emergency contact fields
  [Required]
  [DisplayName("Emergency Name")]
  public string? EmergencyContactName { get; set; }

  [Required]
  [DisplayName("Emergency No#")]
  //[RegularExpression(@"^0(6|7|8)[0-9]{8}$", ErrorMessage = "Emergency contact must be a valid South African cellphone number.")]
  public string? EmergencyContactNumber { get; set; }

  // Navigation property
  public RoomViewModel? Room { get; set; }

  public List<PaymentViewModel> Payments { get; set; } = new List<PaymentViewModel>();
  public List<LeaseAgreementViewModel> LeaseAgreements { get; set; } = new List<LeaseAgreementViewModel>();
}