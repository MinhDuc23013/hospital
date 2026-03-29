using MediatR;
using PharmacyServiceDotnet.Application;

namespace PharmacyServiceDotnet.Application.Commands;

public record UpdateDrugCommand(
    Guid Id,
    string? Name,
    string? Dosage,
    int? Quantity,
    decimal? Price,
    int? LowStockThreshold
) : IRequest<DrugResult>;
