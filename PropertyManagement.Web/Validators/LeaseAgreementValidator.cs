using FluentValidation;
using Microsoft.EntityFrameworkCore;
using PropertyManagement.Domain.Entities;
using PropertyManagement.Infrastructure.Data;

public class LeaseAgreementValidator : AbstractValidator<LeaseAgreement>
{
    private readonly ApplicationDbContext _dbContext;

    public LeaseAgreementValidator(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;

        // TenantId: required, must exist
        RuleFor(x => x.TenantId)
            .GreaterThan(0).WithMessage("A valid tenant must be selected.")
            .MustAsync(async (tenantId, cancellation) =>
                await _dbContext.Set<Tenant>().AnyAsync(t => t.TenantId == tenantId, cancellation))
            .WithMessage("Selected tenant does not exist.");

        // RoomId: required, must exist
        RuleFor(x => x.RoomId)
            .GreaterThan(0).WithMessage("A valid room must be selected.")
            .MustAsync(async (roomId, cancellation) =>
                await _dbContext.Set<Room>().AnyAsync(r => r.RoomId == roomId, cancellation))
            .WithMessage("Selected room does not exist.");

        // StartDate: required, not in the past for new agreements
        RuleFor(x => x.StartDate)
            .NotEmpty().WithMessage("Start date is required.")
            .Must((agreement, startDate) =>
                agreement.LeaseAgreementId != 0 || startDate >= DateTime.Today)
            .WithMessage("Start date cannot be in the past for new agreements.");

        // EndDate: required, after StartDate
        RuleFor(x => x.EndDate)
            .NotEmpty().WithMessage("End date is required.")
            .GreaterThan(x => x.StartDate).WithMessage("End date must be after start date.");

        // RentAmount: required, positive
        RuleFor(x => x.RentAmount)
            .GreaterThan(0).WithMessage("Rent amount must be greater than zero.");

        // ExpectedRentDay: 1-28 (to avoid issues with short months)
        RuleFor(x => x.ExpectedRentDay)
            .InclusiveBetween(1, 28).WithMessage("Expected rent day must be between 1 and 28.");

        // FilePath: if present, must be PDF
        RuleFor(x => x.FilePath)
            .Must(path => string.IsNullOrEmpty(path) || path.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
            .WithMessage("Lease agreement file must be a PDF.");

        // Uniqueness: Only one active lease per tenant per room (no overlapping leases)
        RuleFor(x => new { x.TenantId, x.RoomId, x.StartDate, x.EndDate, x.LeaseAgreementId })
            .MustAsync(async (lease, cancellation) =>
            {
                return !await _dbContext.Set<LeaseAgreement>().AnyAsync(a =>
                    a.TenantId == lease.TenantId &&
                    a.RoomId == lease.RoomId &&
                    a.LeaseAgreementId != lease.LeaseAgreementId &&
                    // Overlapping date ranges
                    a.StartDate < lease.EndDate && lease.StartDate < a.EndDate,
                    cancellation);
            })
            .WithMessage("There is already an active lease for this tenant and room during the selected period.");
    }
}