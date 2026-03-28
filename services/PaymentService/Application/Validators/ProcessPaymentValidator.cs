using FluentValidation;
using PaymentService.Application.Commands;

namespace PaymentService.Application.Validators;

public class ProcessPaymentValidator : AbstractValidator<ProcessPaymentCommand>
{
    public ProcessPaymentValidator()
    {
        RuleFor(x => x.PaymentId).NotEmpty();
    }
}
