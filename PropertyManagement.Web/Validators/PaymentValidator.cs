using FluentValidation;
using Microsoft.EntityFrameworkCore;
using PropertyManagement.Domain.Entities;
using PropertyManagement.Infrastructure.Data;

public class PaymentValidator : AbstractValidator<Payment>
{
    private readonly ApplicationDbContext _dbContext;

    public PaymentValidator(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;

        RuleFor(x => x.TenantId)
            .GreaterThan(0).WithMessage("Tenant is required.");

        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("Amount must be greater than zero.");

        RuleFor(x => x.Type)
            .NotEmpty().WithMessage("Payment type is required.")
            .Must(type => type == "Rent" || type == "Deposit")
            .WithMessage("Payment type must be 'Rent' or 'Deposit'.");

        RuleFor(x => x.PaymentMonth)
            .InclusiveBetween(1, 12).WithMessage("Please select a valid month.");

        RuleFor(x => x.PaymentYear)
            .InclusiveBetween(2000, 2100).WithMessage("Please select a valid year.");

        // Prevent duplicate payments for the same tenant, year, and month
        RuleFor(x => new { x.TenantId, x.PaymentYear, x.PaymentMonth, x.PaymentId })
            .MustAsync(async (payment, cancellation) =>
            {
                return !await _dbContext.Set<Payment>().AnyAsync(p =>
                    p.TenantId == payment.TenantId &&
                    p.PaymentYear == payment.PaymentYear &&
                    p.PaymentMonth == payment.PaymentMonth &&
                    p.PaymentId != payment.PaymentId, cancellation);
            })
            .WithMessage("A payment for this tenant, year, and month already exists.");

        // Ensure the tenant exists
        RuleFor(x => x.TenantId)
            .MustAsync(async (tenantId, cancellation) =>
                await _dbContext.Set<Tenant>().AnyAsync(t => t.TenantId == tenantId, cancellation))
            .WithMessage("Selected tenant does not exist.");

        // Ensure the tenant is assigned to a room
        RuleFor(x => x.TenantId)
            .MustAsync(async (tenantId, cancellation) =>
            {
                var tenant = await _dbContext.Set<Tenant>().FirstOrDefaultAsync(t => t.TenantId == tenantId, cancellation);
                return tenant != null && tenant.RoomId > 0;
            })
            .WithMessage("Tenant must be assigned to a room.");

        // Ensure payment date is not in the future
        RuleFor(x => x.Date)
            .LessThanOrEqualTo(DateTime.Now).WithMessage("Payment date cannot be in the future.");

        // If LeaseAgreementId is set, ensure it exists and belongs to the tenant
        RuleFor(x => x)
            .MustAsync(async (payment, cancellation) =>
            {
                if (payment.LeaseAgreementId == null) return true;
                var agreement = await _dbContext.Set<LeaseAgreement>().FirstOrDefaultAsync(
                    la => la.LeaseAgreementId == payment.LeaseAgreementId && la.TenantId == payment.TenantId, cancellation);
                return agreement != null;
            })
            .WithMessage("Lease agreement does not exist or does not belong to the tenant.");

        // Optionally: Ensure payment is not for a future period
        RuleFor(x => new { x.PaymentYear, x.PaymentMonth })
            .Must(period =>
            {
                var now = DateTime.Now;
                return period.PaymentYear < now.Year ||
                       (period.PaymentYear == now.Year && period.PaymentMonth <= now.Month);
            })
            .WithMessage("Cannot record payment for a future period.");
    }
}