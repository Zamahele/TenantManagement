namespace PropertyManagement.Application.DTOs;

public class PaymentDto
{
    public int PaymentId { get; set; }
    public int TenantId { get; set; }
    public int? LeaseAgreementId { get; set; }
    public decimal Amount { get; set; }
    public DateTime PaymentDate { get; set; }
    public string Type { get; set; } = string.Empty;
    public string? ReceiptPath { get; set; }
    public int PaymentMonth { get; set; }
    public int PaymentYear { get; set; }
    public TenantDto Tenant { get; set; } = null!;
    public LeaseAgreementDto? LeaseAgreement { get; set; }
}

public class CreatePaymentDto
{
    public int TenantId { get; set; }
    public decimal Amount { get; set; }
    public DateTime PaymentDate { get; set; }
    public string Type { get; set; } = string.Empty;
    public string? ReceiptPath { get; set; }
    public int PaymentMonth { get; set; }
    public int PaymentYear { get; set; }
}

public class UpdatePaymentDto
{
    public decimal Amount { get; set; }
    public DateTime PaymentDate { get; set; }
    public string Type { get; set; } = string.Empty;
    public string? ReceiptPath { get; set; }
    public int PaymentMonth { get; set; }
    public int PaymentYear { get; set; }
}

public class PaymentReceiptDto
{
    public int PaymentId { get; set; }
    public string TenantName { get; set; } = string.Empty;
    public string RoomNumber { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime PaymentDate { get; set; }
    public string Type { get; set; } = string.Empty;
    public string PaymentPeriod { get; set; } = string.Empty;
    public string ReceiptNumber { get; set; } = string.Empty;
}