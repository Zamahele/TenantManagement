using System.ComponentModel.DataAnnotations;

namespace PropertyManagement.Web.ViewModels;

public class UserViewModel
{
    public int UserId { get; set; }

    [Required]
    [StringLength(100)]
    public string Username { get; set; } = string.Empty;

    [Required]
    public string PasswordHash { get; set; } = string.Empty;

    [Required]
    [StringLength(20)]
    public string Role { get; set; } = string.Empty;
}