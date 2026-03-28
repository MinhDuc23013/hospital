using DoctorScheduleService.Application.Commands;
using FluentValidation;

namespace DoctorScheduleService.Application.Validators;

public class ReserveSlotValidator : AbstractValidator<ReserveSlotCommand>
{
    public ReserveSlotValidator()
    {
        RuleFor(x => x.ScheduleId).NotEmpty();
        RuleFor(x => x.SlotId).NotEmpty();
        RuleFor(x => x.PatientId).NotEmpty();
    }
}
