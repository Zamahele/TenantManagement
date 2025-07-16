using FluentValidation;
using Microsoft.EntityFrameworkCore;
using PropertyManagement.Domain.Entities;
using PropertyManagement.Infrastructure.Data;

public class InspectionValidator : AbstractValidator<Inspection>
{
    private readonly ApplicationDbContext _dbContext;

    public InspectionValidator(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;

        // RoomId: required, must exist
        RuleFor(x => x.RoomId)
            .GreaterThan(0).WithMessage("A valid room must be selected.")
            .MustAsync(async (roomId, cancellation) =>
                await _dbContext.Set<Room>().AnyAsync(r => r.RoomId == roomId, cancellation))
            .WithMessage("Selected room does not exist.");

        // Date: required, not in the future
        RuleFor(x => x.Date)
            .NotEmpty().WithMessage("Inspection date is required.")
            .LessThanOrEqualTo(DateTime.Today).WithMessage("Inspection date cannot be in the future.");

        // Result: required, max length
        RuleFor(x => x.Result)
            .NotEmpty().WithMessage("Result is required.")
            .MaximumLength(200).WithMessage("Result must be at most 200 characters.");

        // Notes: optional, max length
        RuleFor(x => x.Notes)
            .MaximumLength(1000).WithMessage("Notes must be at most 1000 characters.");

        // Optional: Prevent duplicate inspection for same room and date
        RuleFor(x => new { x.RoomId, x.Date, x.InspectionId })
            .MustAsync(async (info, cancellation) =>
            {
                return !await _dbContext.Set<Inspection>().AnyAsync(i =>
                    i.RoomId == info.RoomId &&
                    i.Date == info.Date &&
                    i.InspectionId != info.InspectionId, cancellation);
            })
            .WithMessage("An inspection for this room on this date already exists.");
    }
}