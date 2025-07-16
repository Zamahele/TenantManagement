using System;
using System.ComponentModel.DataAnnotations;

namespace PropertyManagement.Web.ViewModels;
public class MaintenanceRequestViewModel
{
    public int MaintenanceRequestId { get; set; }

    [Required]
    public int RoomId { get; set; }

    public string TenantId { get; set; }

    [Required]
    [Display(Name = "Description")]
    [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters.")]
    public string Description { get; set; }

    [Display(Name = "Request Date")]
    public DateTime RequestDate { get; set; }

    [Required]
    [Display(Name = "Status")]
    public string Status { get; set; }

    [Display(Name = "Assigned To")]
    public string? AssignedTo { get; set; }

    [Display(Name = "Completed Date")]
    public DateTime? CompletedDate { get; set; }

    // For display purposes
    public RoomViewModel? Room { get; set; }
}