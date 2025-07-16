using FluentValidation;
using Microsoft.EntityFrameworkCore;
using PropertyManagement.Domain.Entities;
using PropertyManagement.Infrastructure.Data;

public class TenantValidator : AbstractValidator<Tenant>
{
    private readonly ApplicationDbContext _dbContext;

    public TenantValidator(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;

        // Full name: required, letters and spaces only, reasonable length
        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Full name is required.")
            .Matches(@"^[A-Za-z\s]+$").WithMessage("Full name must contain letters and spaces only.")
            .MaximumLength(100).WithMessage("Full name must be at most 100 characters.");

        // Contact: required, valid South African cellphone number
        RuleFor(x => x.Contact)
            .NotEmpty().WithMessage("Contact is required.")
            .Matches(@"^0(6|7|8)[0-9]{8}$").WithMessage("Contact must be a valid South African cellphone number (e.g., 0821234567).");

        // RoomId: required, must exist, and must not already have an active tenant
        RuleFor(x => x.RoomId)
            .GreaterThan(0).WithMessage("Room selection is required.")
            .MustAsync(async (roomId, cancellation) =>
                await _dbContext.Set<Room>().AnyAsync(r => r.RoomId == roomId, cancellation))
            .WithMessage("Selected room does not exist.")
            .MustAsync(async (roomId, cancellation) =>
            {
                // Only allow one active tenant per room
                return !await _dbContext.Set<Tenant>().AnyAsync(t => t.RoomId == roomId, cancellation);
            })
            .WithMessage("This room is already assigned to another tenant.");

        // Emergency contact name: required, letters and spaces only, reasonable length
        RuleFor(x => x.EmergencyContactName)
            .NotEmpty().WithMessage("Emergency contact name is required.")
            .Matches(@"^[A-Za-z\s]+$").WithMessage("Emergency contact name must contain letters and spaces only.")
            .MaximumLength(100).WithMessage("Emergency contact name must be at most 100 characters.");

        // Emergency contact number: required, valid South African cellphone number
        RuleFor(x => x.EmergencyContactNumber)
            .NotEmpty().WithMessage("Emergency contact number is required.")
            .Matches(@"^0(6|7|8)[0-9]{8}$").WithMessage("Emergency contact must be a valid South African cellphone number.");

        // Username: required, unique
        RuleFor(x => x.User)
            .NotNull().WithMessage("User account is required.")
            .MustAsync(async (user, cancellation) =>
            {
                if (user == null || string.IsNullOrWhiteSpace(user.Username))
                    return false;
                return !await _dbContext.Set<User>().AnyAsync(u => u.Username == user.Username, cancellation);
            })
            .WithMessage("Username already exists.");

        // UserId: must exist in Users table
        RuleFor(x => x.UserId)
            .MustAsync(async (userId, cancellation) =>
                await _dbContext.Set<User>().AnyAsync(u => u.UserId == userId, cancellation))
            .WithMessage("Associated user account does not exist.");

        // Prevent duplicate tenant for the same user
        RuleFor(x => x.UserId)
            .MustAsync(async (userId, cancellation) =>
                !await _dbContext.Set<Tenant>().AnyAsync(t => t.UserId == userId, cancellation))
            .WithMessage("A tenant profile for this user already exists.");
    }
}