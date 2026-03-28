using AppointmentService.Application.Commands;
using FluentValidation;

namespace AppointmentService.Application.Validators;

public class ScheduleAppointmentValidator : AbstractValidator<ScheduleAppointmentCommand>
{
    public ScheduleAppointmentValidator()
    {
        RuleFor(x => x.PatientId).NotEmpty();
        RuleFor(x => x.ProviderId).NotEmpty().MaximumLength(100);
        RuleFor(x => x.ScheduledTime).NotEmpty().GreaterThan(DateTime.UtcNow);
        RuleFor(x => x.DurationMinutes).InclusiveBetween(5, 480);
        RuleFor(x => x.Notes).MaximumLength(2000).When(x => x.Notes is not null);
    }
}
