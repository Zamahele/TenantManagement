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

        // Room Number validation with specific error messages for each rule
        RuleFor(x => x.Number)
            .NotEmpty()
            .WithMessage("? Room number is required - please enter a room number")
            .WithName("Room Number");

        RuleFor(x => x.Number)
            .MaximumLength(10)
            .WithMessage("? Room number is too long - maximum 10 characters allowed (current: {TotalLength} characters)")
            .WithName("Room Number")
            .When(x => !string.IsNullOrEmpty(x.Number));

        RuleFor(x => x.Number)
            .Must((model, number) => BeUniqueRoomNumberSync(model.RoomId, number))
            .WithMessage("? Room number '{PropertyValue}' already exists - please choose a different room number")
            .WithName("Room Number")
            .When(x => !string.IsNullOrEmpty(x.Number) && x.Number.Length <= 10);

        // Room Type validation with specific error messages
        RuleFor(x => x.Type)
            .NotEmpty()
            .WithMessage("? Room type is required - please select a room type")
            .WithName("Room Type");

        RuleFor(x => x.Type)
            .MaximumLength(50)
            .WithMessage("? Room type is too long - maximum 50 characters allowed")
            .WithName("Room Type")
            .When(x => !string.IsNullOrEmpty(x.Type));

        // Room Status validation with specific error messages
        RuleFor(x => x.Status)
            .NotEmpty()
            .WithMessage("? Room status is required - please select a status")
            .WithName("Room Status");

        RuleFor(x => x.Status)
            .Must(status => status == "Available" || status == "Occupied" || status == "Under Maintenance")
            .WithMessage("? Invalid status selected - must be 'Available', 'Occupied', or 'Under Maintenance' (current: '{PropertyValue}')")
            .WithName("Room Status")
            .When(x => !string.IsNullOrEmpty(x.Status));
    }

    private bool BeUniqueRoomNumberSync(int roomId, string number)
    {
        if (string.IsNullOrEmpty(number))
            return true; // Let the NotEmpty rule handle this

        try
        {
            // When creating a new room (RoomId == 0), check if number exists
            // When updating an existing room (RoomId > 0), exclude current room from check
            var existingRoom = _dbContext.Rooms
                .Where(r => r.Number == number && r.RoomId != roomId)
                .FirstOrDefault();
                
            return existingRoom == null;
        }
        catch (Exception)
        {
            // If database query fails, allow validation to pass
            // The actual operation will handle the database error
            return true;
        }
    }
}