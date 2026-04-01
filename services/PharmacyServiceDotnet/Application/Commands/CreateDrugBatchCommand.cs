using MediatR;
using PharmacyServiceDotnet.Application;

namespace PharmacyServiceDotnet.Application.Commands;

public record CreateDrugBatchCommand(
    Guid DrugId,
    string BatchNumber,
    DateTime ExpiryDate,
    int Quantity,
    DateTime? ReceivedDate = null
) : IRequest<DrugBatchResult>;
