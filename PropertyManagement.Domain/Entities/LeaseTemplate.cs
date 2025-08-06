using System.ComponentModel.DataAnnotations;

namespace PropertyManagement.Domain.Entities
{
    public class LeaseTemplate
    {
        public int LeaseTemplateId { get; set; }
        
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;
        
        [Required]
        public string HtmlContent { get; set; } = string.Empty;
        
        public string? Description { get; set; }
        
        public bool IsActive { get; set; } = true;
        
        public bool IsDefault { get; set; } = false;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        
        // Template variables that can be used in the HTML content
        public string? TemplateVariables { get; set; } // JSON string of available variables
    }
}