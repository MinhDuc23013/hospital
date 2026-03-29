using MediatR;
using PharmacyServiceDotnet.Application;

namespace PharmacyServiceDotnet.Application.Queries;

public record ListDrugsQuery(
    string? Name,
    bool? LowStock,
    int Page,
    int PageSize
) : IRequest<(List<DrugResult> Items, int Total)>;
