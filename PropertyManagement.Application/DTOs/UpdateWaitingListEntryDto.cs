using System.ComponentModel.DataAnnotations;

namespace PropertyManagement.Application.DTOs
{
    public class UpdateWaitingListEntryDto
    {
        [Required]
        public int WaitingListId { get; set; }
        
        [Required(ErrorMessage = "Phone number is required")]
        [RegularExpression(@"^0[6-8][0-9]{8}$", ErrorMessage = "Phone number must be in South African format (e.g., 0821234567)")]
        public string PhoneNumber { get; set; } = string.Empty;
        
        [StringLength(100, ErrorMessage = "Full name cannot exceed 100 characters")]
        public string? FullName { get; set; }
        
        [EmailAddress(ErrorMessage = "Invalid email format")]
        [StringLength(100, ErrorMessage = "Email cannot exceed 100 characters")]
        public string? Email { get; set; }
        
        [StringLength(20, ErrorMessage = "Room type cannot exceed 20 characters")]
        public string? PreferredRoomType { get; set; }
        
        [Range(0, 999999.99, ErrorMessage = "Budget must be between 0 and 999,999.99")]
        public decimal? MaxBudget { get; set; }
        
        [Required(ErrorMessage = "Status is required")]
        [StringLength(20, ErrorMessage = "Status cannot exceed 20 characters")]
        public string Status { get; set; } = "Active";
        
        [StringLength(500, ErrorMessage = "Notes cannot exceed 500 characters")]
        public string? Notes { get; set; }
        
        [StringLength(50, ErrorMessage = "Source cannot exceed 50 characters")]
        public string? Source { get; set; }
        
        public bool IsActive { get; set; } = true;
    }
}