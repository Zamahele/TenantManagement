using FluentValidation;
using PropertyManagement.Domain.Entities;

public class UtilityBillValidator : AbstractValidator<UtilityBill>
{
    public UtilityBillValidator()
    {
        RuleFor(x => x.RoomId)
            .GreaterThan(0).WithMessage("A valid room must be selected.");

        RuleFor(x => x.BillingDate)
            .NotEmpty().WithMessage("Billing date is required.")
            .LessThanOrEqualTo(DateTime.Today).WithMessage("Billing date cannot be in the future.");

        RuleFor(x => x.WaterUsage)
            .GreaterThanOrEqualTo(0).WithMessage("Water usage must be zero or positive.");

        RuleFor(x => x.ElectricityUsage)
            .GreaterThanOrEqualTo(0).WithMessage("Electricity usage must be zero or positive.");

        RuleFor(x => x.TotalAmount)
            .GreaterThanOrEqualTo(0).WithMessage("Total amount must be zero or positive.");

        RuleFor(x => x.Notes)
            .MaximumLength(1000).WithMessage("Notes must be at most 1000 characters.");
    }
}