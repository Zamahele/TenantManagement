using FluentValidation;
using PropertyManagement.Web.ViewModels;

namespace PropertyManagement.Web.Validators
{
    public class RoomFormViewModelValidatorSync : AbstractValidator<RoomFormViewModel>
    {
        public RoomFormViewModelValidatorSync()
        {
            RuleFor(x => x.Number)
                .NotEmpty().WithMessage("Room number is required.")
                .MaximumLength(10).WithMessage("Room number must be at most 10 characters.");

            RuleFor(x => x.Type)
                .NotEmpty().WithMessage("Room type is required.")
                .MaximumLength(50).WithMessage("Room type must be at most 50 characters.");

            RuleFor(x => x.Status)
                .NotEmpty().WithMessage("Room status is required.")
                .Must(status => status == "Available" || status == "Occupied" || status == "Under Maintenance")
                .WithMessage("Status must be Available, Occupied, or Under Maintenance.");

            // Note: Uniqueness validation will be handled in the controller/service layer
        }
    }
}