using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

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

public class MaintenanceRequestFormViewModel
{
    public int MaintenanceRequestId { get; set; }

    [Required(ErrorMessage = "Please select a room.")]
    [Display(Name = "Room")]
    public int RoomId { get; set; }

    public string? TenantId { get; set; } = "0";

    [Required(ErrorMessage = "Description is required.")]
    [Display(Name = "Description")]
    [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters.")]
    public string Description { get; set; } = string.Empty;

    [Display(Name = "Request Date")]
    public DateTime RequestDate { get; set; } = DateTime.Today;

    [Required(ErrorMessage = "Status is required.")]
    [Display(Name = "Status")]
    public string Status { get; set; } = "Pending";

    [Display(Name = "Assigned To")]
    [StringLength(100, ErrorMessage = "Assigned To cannot exceed 100 characters.")]
    public string? AssignedTo { get; set; }

    [Display(Name = "Completed Date")]
    public DateTime? CompletedDate { get; set; }

    // Dropdown options
    public List<SelectListItem> RoomOptions { get; set; } = new List<SelectListItem>();
    public List<SelectListItem> StatusOptions { get; set; } = new List<SelectListItem>
    {
        new SelectListItem { Value = "Pending", Text = "Pending" },
        new SelectListItem { Value = "In Progress", Text = "In Progress" },
        new SelectListItem { Value = "Completed", Text = "Completed" },
        new SelectListItem { Value = "Cancelled", Text = "Cancelled" }
    };
}