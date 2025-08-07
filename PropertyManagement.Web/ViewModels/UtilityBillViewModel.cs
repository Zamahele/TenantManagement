using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace PropertyManagement.Web.ViewModels;

public class UtilityBillViewModel
{
    public int UtilityBillId { get; set; }
    
    [Required(ErrorMessage = "Room is required")]
    [Display(Name = "Room")]
    public int RoomId { get; set; }
    
    [Required(ErrorMessage = "Billing date is required")]
    [Display(Name = "Billing Date")]
    [DataType(DataType.Date)]
    public DateTime BillingDate { get; set; }
    
    [Required(ErrorMessage = "Water usage is required")]
    [Display(Name = "Water Usage (Liters)")]
    [Range(0, double.MaxValue, ErrorMessage = "Water usage must be a positive number")]
    public decimal WaterUsage { get; set; }
    
    [Required(ErrorMessage = "Electricity usage is required")]
    [Display(Name = "Electricity Usage (kWh)")]
    [Range(0, double.MaxValue, ErrorMessage = "Electricity usage must be a positive number")]
    public decimal ElectricityUsage { get; set; }
    
    [Display(Name = "Total Amount")]
    [DataType(DataType.Currency)]
    public decimal TotalAmount { get; set; }
    
    [Display(Name = "Notes")]
    [StringLength(500, ErrorMessage = "Notes cannot exceed 500 characters")]
    public string? Notes { get; set; }
    
    // Navigation properties
    public RoomViewModel? Room { get; set; }
    
    // For dropdown lists
    public List<SelectListItem>? RoomOptions { get; set; }
}

public class UtilityBillFormViewModel
{
    public int UtilityBillId { get; set; }
    
    [Required(ErrorMessage = "Room is required")]
    [Display(Name = "Room")]
    public int RoomId { get; set; }
    
    [Required(ErrorMessage = "Billing date is required")]
    [Display(Name = "Billing Date")]
    [DataType(DataType.Date)]
    public DateTime BillingDate { get; set; } = DateTime.Today;
    
    [Required(ErrorMessage = "Water usage is required")]
    [Display(Name = "Water Usage (Liters)")]
    [Range(0, double.MaxValue, ErrorMessage = "Water usage must be a positive number")]
    public decimal WaterUsage { get; set; }
    
    [Required(ErrorMessage = "Electricity usage is required")]
    [Display(Name = "Electricity Usage (kWh)")]
    [Range(0, double.MaxValue, ErrorMessage = "Electricity usage must be a positive number")]
    public decimal ElectricityUsage { get; set; }
    
    [Display(Name = "Total Amount")]
    [DataType(DataType.Currency)]
    public decimal TotalAmount { get; set; }
    
    [Display(Name = "Notes")]
    [StringLength(500, ErrorMessage = "Notes cannot exceed 500 characters")]
    public string? Notes { get; set; }
    
    // For dropdown lists
    public List<SelectListItem>? RoomOptions { get; set; }
    
    // Utility rates for calculation
    public decimal WaterRate { get; set; } = 0.02m;
    public decimal ElectricityRate { get; set; } = 1.50m;
}