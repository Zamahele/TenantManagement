namespace PropertyManagement.Application.DTOs;

public class LeaseAgreementDto
{
    public int LeaseAgreementId { get; set; }
    public int TenantId { get; set; }
    public int RoomId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal RentAmount { get; set; }
    public int ExpectedRentDay { get; set; }
    public string? FilePath { get; set; }
    public TenantDto Tenant { get; set; } = null!;
    public RoomDto Room { get; set; } = null!;
}

public class CreateLeaseAgreementDto
{
    public int TenantId { get; set; }
    public int RoomId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal RentAmount { get; set; }
    public int ExpectedRentDay { get; set; }
    public string? FilePath { get; set; }
}

public class UpdateLeaseAgreementDto
{
    public int TenantId { get; set; }
    public int RoomId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal RentAmount { get; set; }
    public int ExpectedRentDay { get; set; }
    public string? FilePath { get; set; }
}