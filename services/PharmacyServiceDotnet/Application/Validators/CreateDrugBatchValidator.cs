using FluentValidation;
using PharmacyServiceDotnet.Application.Commands;

namespace PharmacyServiceDotnet.Application.Validators;

public class CreateDrugBatchValidator : AbstractValidator<CreateDrugBatchCommand>
{
    public CreateDrugBatchValidator()
    {
        RuleFor(x => x.DrugId).NotEmpty();
        RuleFor(x => x.BatchNumber).NotEmpty().MaximumLength(100);
        RuleFor(x => x.ExpiryDate).GreaterThan(DateTime.Now).WithMessage("Expiry date must be in the future");
        RuleFor(x => x.Quantity).GreaterThan(0);
    }
}
