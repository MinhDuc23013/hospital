using FluentValidation;
using PaymentService.Application.Commands;
using PaymentService.Domain.Enums;

namespace PaymentService.Application.Validators;

public class CreatePaymentValidator : AbstractValidator<CreatePaymentCommand>
{
    public CreatePaymentValidator()
    {
        RuleFor(x => x.AppointmentId).NotEmpty();
        RuleFor(x => x.PatientId).NotEmpty();
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.Currency).NotEmpty().MaximumLength(10);
        RuleFor(x => x.Method).IsInEnum();
        RuleFor(x => x.Description).MaximumLength(500).When(x => x.Description is not null);
    }
}
