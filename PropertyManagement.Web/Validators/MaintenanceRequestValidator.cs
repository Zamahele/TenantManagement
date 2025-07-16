using FluentValidation;
using Microsoft.EntityFrameworkCore;
using PropertyManagement.Domain.Entities;
using PropertyManagement.Infrastructure.Data;

public class MaintenanceRequestValidator : AbstractValidator<MaintenanceRequest>
{
    private readonly ApplicationDbContext _dbContext;

    public MaintenanceRequestValidator(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;

        // RoomId: required, must exist
        RuleFor(x => x.RoomId)
            .GreaterThan(0).WithMessage("A valid room must be selected.")
            .MustAsync(async (roomId, cancellation) =>
                await _dbContext.Set<Room>().AnyAsync(r => r.RoomId == roomId, cancellation))
            .WithMessage("Selected room does not exist.");

        // Description: required, length
        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Description is required.")
            .MinimumLength(10).WithMessage("Description must be at least 10 characters.")
            .MaximumLength(1000).WithMessage("Description must be at most 1000 characters.");

        // Status: required, must be valid value
        RuleFor(x => x.Status)
            .NotEmpty().WithMessage("Status is required.")
            .Must(status => status == "Pending" || status == "In Progress" || status == "Completed")
            .WithMessage("Status must be Pending, In Progress, or Completed.");

        // AssignedTo: required
        RuleFor(x => x.AssignedTo)
            .NotEmpty().WithMessage("AssignedTo is required for tracking responsibility.")
            .MaximumLength(100).WithMessage("AssignedTo must be at most 100 characters.");

        // CompletedDate: must be set if status is Completed, must not be set otherwise
        RuleFor(x => x.CompletedDate)
            .NotNull().When(x => x.Status == "Completed")
            .WithMessage("Completed date must be set when status is Completed.");
        RuleFor(x => x.CompletedDate)
            .Null().When(x => x.Status != "Completed")
            .WithMessage("Completed date must be empty unless status is Completed.");

        // RequestDate: required, not in the future
        RuleFor(x => x.RequestDate)
            .NotEmpty().WithMessage("Request date is required.")
            .LessThanOrEqualTo(DateTime.UtcNow).WithMessage("Request date cannot be in the future.");

        // TenantId: required, must be numeric, must exist if not null/empty
        RuleFor(x => x.TenantId)
            .NotEmpty().WithMessage("Tenant is required.")
            .Matches(@"^\d+$").WithMessage("Tenant ID must be numeric.")
            .MustAsync(async (tenantId, cancellation) =>
            {
                if (string.IsNullOrWhiteSpace(tenantId)) return false;
                if (!int.TryParse(tenantId, out var id)) return false;
                return await _dbContext.Set<Tenant>().AnyAsync(t => t.TenantId == id, cancellation);
            })
            .WithMessage("Selected tenant does not exist.");
    }
}