namespace PropertyManagement.Application.DTOs;

public class BookingRequestDto
{
    public int BookingRequestId { get; set; }
    public int RoomId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Contact { get; set; } = string.Empty;
    public DateTime RequestDate { get; set; }
    public bool DepositPaid { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? ProofOfPaymentPath { get; set; }
    public string? Note { get; set; }
    public RoomDto Room { get; set; } = null!;
}

public class CreateBookingRequestDto
{
    public int RoomId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Contact { get; set; } = string.Empty;
    public bool DepositPaid { get; set; }
    public string? ProofOfPaymentPath { get; set; }
    public string? Note { get; set; }
}

public class UpdateBookingRequestDto
{
    public int RoomId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Contact { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public bool DepositPaid { get; set; }
    public string? ProofOfPaymentPath { get; set; }
    public string? Note { get; set; }
}

public class BookingRequestStatusDto
{
    public int BookingRequestId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Note { get; set; }
}