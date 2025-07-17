namespace PropertyManagement.Application.DTOs;

public class UtilityBillDto
{
    public int UtilityBillId { get; set; }
    public int RoomId { get; set; }
    public DateTime BillingDate { get; set; }
    public decimal WaterUsage { get; set; }
    public decimal ElectricityUsage { get; set; }
    public decimal TotalAmount { get; set; }
    public string? Notes { get; set; }
    public RoomDto? Room { get; set; }
}

public class CreateUtilityBillDto
{
    public int RoomId { get; set; }
    public DateTime BillingDate { get; set; }
    public decimal WaterUsage { get; set; }
    public decimal ElectricityUsage { get; set; }
    public decimal TotalAmount { get; set; }
    public string? Notes { get; set; }
}

public class UpdateUtilityBillDto
{
    public DateTime BillingDate { get; set; }
    public decimal WaterUsage { get; set; }
    public decimal ElectricityUsage { get; set; }
    public decimal TotalAmount { get; set; }
    public string? Notes { get; set; }
}