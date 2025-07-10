using System.ComponentModel.DataAnnotations;

namespace PropertyManagement.Web.ViewModels;
public class TenantLoginViewModel
{
    [Required]
    public string Username { get; set; } = string.Empty;
    [Required]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;
}