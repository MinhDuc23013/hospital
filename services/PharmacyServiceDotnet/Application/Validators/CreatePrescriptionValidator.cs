using FluentValidation;
using PharmacyServiceDotnet.Application.Commands;

namespace PharmacyServiceDotnet.Application.Validators;

public class CreatePrescriptionValidator : AbstractValidator<CreatePrescriptionCommand>
{
    public CreatePrescriptionValidator()
    {
        RuleFor(x => x.PatientId).NotEmpty();
        RuleFor(x => x.DoctorId).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Items).NotEmpty();
        RuleFor(x => x.Notes).MaximumLength(2000).When(x => x.Notes is not null);
    }
}
