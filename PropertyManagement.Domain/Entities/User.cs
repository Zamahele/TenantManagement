using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PropertyManagement.Domain.Entities
{
  public class User
  {
      public int UserId { get; set; }
      [Required]
      public string Username { get; set; } = string.Empty;
      [Required]
      public string PasswordHash { get; set; } = string.Empty;
      [Required]
      public string Role { get; set; } = "Tenant"; // "Tenant" or "Manager"
      // Optionally: public int? TenantId { get; set; }
  }
}
