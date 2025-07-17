namespace PropertyManagement.Application.DTOs;

public class InspectionDto
{
    public int InspectionId { get; set; }
    public int RoomId { get; set; }
    public DateTime Date { get; set; }
    public string? Result { get; set; }
    public string? Notes { get; set; }
    public RoomDto? Room { get; set; }
}

public class CreateInspectionDto
{
    public int RoomId { get; set; }
    public DateTime Date { get; set; }
    public string? Result { get; set; }
    public string? Notes { get; set; }
}

public class UpdateInspectionDto
{
    public DateTime Date { get; set; }
    public string? Result { get; set; }
    public string? Notes { get; set; }
}