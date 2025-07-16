using FluentValidation;
using Microsoft.EntityFrameworkCore;
using PropertyManagement.Infrastructure.Data;


public class BookingRequestViewModelValidator : AbstractValidator<PropertyManagement.Web.ViewModels.BookingRequestViewModel>
{
    private readonly ApplicationDbContext _dbContext;

    public BookingRequestViewModelValidator(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;

        // RoomId: required, must exist, must be available
        RuleFor(x => x.RoomId)
            .GreaterThan(0).WithMessage("Please select a room.")
            .MustAsync(async (roomId, cancellation) =>
                await _dbContext.Set<PropertyManagement.Domain.Entities.Room>().AnyAsync(r => r.RoomId == roomId, cancellation))
            .WithMessage("Selected room does not exist.")
            .MustAsync(async (roomId, cancellation) =>
                await _dbContext.Set<PropertyManagement.Domain.Entities.Room>().AnyAsync(r => r.RoomId == roomId && r.Status == "Available", cancellation))
            .WithMessage("Selected room is not available for booking.");

        // FullName: required, reasonable length, letters and spaces only
        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Full name is required.")
            .Matches(@"^[A-Za-z\s]+$").WithMessage("Full name must contain letters and spaces only.")
            .MaximumLength(100).WithMessage("Full name must be at most 100 characters.");

        // Contact: required, valid South African cellphone
        RuleFor(x => x.Contact)
            .NotEmpty().WithMessage("Contact is required.")
            .Matches(@"^0(6|7|8)[0-9]{8}$")
            .WithMessage("Contact must be a valid South African cellphone number (e.g., 0821234567).");

        // Note: optional, max length
        RuleFor(x => x.Note)
            .MaximumLength(500).WithMessage("Note must be at most 500 characters.")
            .When(x => !string.IsNullOrEmpty(x.Note));

        // Prevent duplicate pending booking for same room and contact
        RuleFor(x => new { x.RoomId, x.Contact, x.BookingRequestId })
            .MustAsync(async (info, cancellation) =>
            {
                return !await _dbContext.Set<PropertyManagement.Domain.Entities.BookingRequest>().AnyAsync(b =>
                    b.RoomId == info.RoomId &&
                    b.Contact == info.Contact &&
                    b.Status == "Pending" &&
                    (info.BookingRequestId == null || b.BookingRequestId != info.BookingRequestId), cancellation);
            })
            .WithMessage("You already have a pending booking request for this room.");

        // ProofOfPaymentPath: if present, must be a PDF, JPG, or PNG
        RuleFor(x => x.ProofOfPaymentPath)
            .Must(path =>
                string.IsNullOrEmpty(path) ||
                path.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase) ||
                path.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
                path.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase) ||
                path.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
            .WithMessage("Proof of payment must be a PDF, JPG, or PNG file.");
    }
}