using MediatR;
using PharmacyServiceDotnet.Application;

namespace PharmacyServiceDotnet.Application.Commands;

public record CreateDrugCommand(
    string Name,
    string Code,
    string? Dosage,
    int Quantity,
    decimal Price,
    int LowStockThreshold = 10
) : IRequest<DrugResult>;
