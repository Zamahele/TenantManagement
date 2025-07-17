namespace PropertyManagement.Application.DTOs;

public class RoomDto
{
    public int RoomId { get; set; }
    public string Number { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int? CottageId { get; set; }
}

public class CreateRoomDto
{
    public string Number { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int? CottageId { get; set; }
}

public class UpdateRoomDto
{
    public string Number { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int? CottageId { get; set; }
}

public class RoomWithTenantsDto
{
    public int RoomId { get; set; }
    public string Number { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int? CottageId { get; set; }
    public ICollection<TenantDto> Tenants { get; set; } = new List<TenantDto>();
}