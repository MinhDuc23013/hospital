using FluentValidation;
using PharmacyServiceDotnet.Application.Commands;

namespace PharmacyServiceDotnet.Application.Validators;

public class CreateDrugValidator : AbstractValidator<CreateDrugCommand>
{
    public CreateDrugValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Code).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Dosage).MaximumLength(100).When(x => x.Dosage is not null);
        RuleFor(x => x.Quantity).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Price).GreaterThanOrEqualTo(0);
        RuleFor(x => x.LowStockThreshold).GreaterThanOrEqualTo(0);
    }
}
