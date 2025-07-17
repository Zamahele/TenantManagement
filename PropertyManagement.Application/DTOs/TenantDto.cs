namespace PropertyManagement.Application.DTOs;

public class TenantDto
{
    public int TenantId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Contact { get; set; } = string.Empty;
    public string EmergencyContactName { get; set; } = string.Empty;
    public string EmergencyContactNumber { get; set; } = string.Empty;
    public int RoomId { get; set; }
    public int UserId { get; set; }
    public UserDto User { get; set; } = null!;
    public RoomDto Room { get; set; } = null!;
}

public class CreateTenantDto
{
    public string FullName { get; set; } = string.Empty;
    public string Contact { get; set; } = string.Empty;
    public string EmergencyContactName { get; set; } = string.Empty;
    public string EmergencyContactNumber { get; set; } = string.Empty;
    public int RoomId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class UpdateTenantDto
{
    public string FullName { get; set; } = string.Empty;
    public string Contact { get; set; } = string.Empty;
    public string EmergencyContactName { get; set; } = string.Empty;
    public string EmergencyContactNumber { get; set; } = string.Empty;
    public int RoomId { get; set; }
    public string? Username { get; set; }
    public string? Password { get; set; }
}

public class RegisterTenantDto
{
    public string FullName { get; set; } = string.Empty;
    public string Contact { get; set; } = string.Empty;
    public string EmergencyContactName { get; set; } = string.Empty;
    public string EmergencyContactNumber { get; set; } = string.Empty;
    public int RoomId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class UpdateProfileDto
{
    public string FullName { get; set; } = string.Empty;
    public string Contact { get; set; } = string.Empty;
    public string EmergencyContactName { get; set; } = string.Empty;
    public string EmergencyContactNumber { get; set; } = string.Empty;
}

public class TenantOutstandingDto
{
    public int TenantId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Contact { get; set; } = string.Empty;
    public string RoomNumber { get; set; } = string.Empty;
    public decimal OutstandingBalance { get; set; }
    public DateTime LastPaymentDate { get; set; }
}