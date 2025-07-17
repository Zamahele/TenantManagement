namespace PropertyManagement.Application.DTOs;

public class MaintenanceRequestDto
{
    public int MaintenanceRequestId { get; set; }
    public int RoomId { get; set; }
    public string TenantId { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime RequestDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? AssignedTo { get; set; }
    public DateTime? CompletedDate { get; set; }
    public RoomDto? Room { get; set; }
}

public class CreateMaintenanceRequestDto
{
    public int RoomId { get; set; }
    public string TenantId { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? AssignedTo { get; set; }
}

public class UpdateMaintenanceRequestDto
{
    public string Description { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? AssignedTo { get; set; }
    public DateTime? CompletedDate { get; set; }
}