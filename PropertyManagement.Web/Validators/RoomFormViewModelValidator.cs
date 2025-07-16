using FluentValidation;
using Microsoft.EntityFrameworkCore;
using PropertyManagement.Infrastructure.Data;
using PropertyManagement.Web.ViewModels;

public class RoomFormViewModelValidator : AbstractValidator<RoomFormViewModel>
{
    private readonly ApplicationDbContext _dbContext;

    public RoomFormViewModelValidator(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;

        RuleFor(x => x.Number)
            .NotEmpty().WithMessage("Room number is required.")
            .MaximumLength(10).WithMessage("Room number must be at most 10 characters.")
            .MustAsync(BeUniqueRoomNumber).WithMessage("Room number already exists.");

        RuleFor(x => x.Type)
            .NotEmpty().WithMessage("Room type is required.")
            .MaximumLength(50).WithMessage("Room type must be at most 50 characters.");

        RuleFor(x => x.Status)
            .NotEmpty().WithMessage("Room status is required.")
            .Must(status => status == "Available" || status == "Occupied" || status == "Under Maintenance")
            .WithMessage("Status must be Available, Occupied, or Under Maintenance.");

        RuleFor(x => x.RoomId)
            .MustAsync(BeUniqueRoomId).WithMessage("This room is already assigned to another tenant.");
    }

    private async Task<bool> BeUniqueRoomNumber(string number, CancellationToken cancellationToken)
    {
        return !await _dbContext.Rooms.AnyAsync(r => r.Number == number, cancellationToken);
    }

    private async Task<bool> BeUniqueRoomId(int roomId, CancellationToken cancellationToken)
    {
        var hasActiveTenant = await _dbContext.Tenants.AnyAsync(t => t.RoomId == roomId, cancellationToken);
        return !hasActiveTenant;
    }
}