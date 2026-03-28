using AppointmentService.Application.Commands;
using FluentValidation;

namespace AppointmentService.Application.Validators;

public class BookAppointmentValidator : AbstractValidator<BookAppointmentCommand>
{
    public BookAppointmentValidator()
    {
        RuleFor(x => x.PatientId).NotEmpty();
        RuleFor(x => x.ProviderId).NotEmpty().MaximumLength(100);
        RuleFor(x => x.ScheduleId).NotEmpty();
        RuleFor(x => x.SlotId).NotEmpty();
        RuleFor(x => x.ScheduledTime).NotEmpty().GreaterThan(DateTime.UtcNow);
        RuleFor(x => x.DurationMinutes).InclusiveBetween(5, 480);
        RuleFor(x => x.PaymentAmount).GreaterThan(0);
        RuleFor(x => x.PaymentMethod).NotEmpty();
        RuleFor(x => x.Currency).NotEmpty().MaximumLength(10);
        RuleFor(x => x.Notes).MaximumLength(2000).When(x => x.Notes is not null);
    }
}
