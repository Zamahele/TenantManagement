using FluentValidation;
using PropertyManagement.Web.ViewModels;

public class TenantLoginViewModelValidator : AbstractValidator<TenantLoginViewModel>
{
    public TenantLoginViewModelValidator()
    {
        RuleFor(x => x.Username)
            .NotEmpty().WithMessage("Username is required.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.");
    }
}