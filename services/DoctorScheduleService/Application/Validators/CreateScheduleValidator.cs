using DoctorScheduleService.Application.Commands;
using FluentValidation;

namespace DoctorScheduleService.Application.Validators;

public class CreateScheduleValidator : AbstractValidator<CreateScheduleCommand>
{
    public CreateScheduleValidator()
    {
        RuleFor(x => x.DoctorId).NotEmpty().MaximumLength(100);
        RuleFor(x => x.DoctorName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Date).NotEmpty().GreaterThan(DateTime.UtcNow.Date);
        RuleFor(x => x.StartTime).NotEmpty();
        RuleFor(x => x.EndTime).NotEmpty()
            .GreaterThan(x => x.StartTime).WithMessage("EndTime must be after StartTime.");
        RuleFor(x => x.SlotDurationMinutes).InclusiveBetween(10, 120);
    }
}
