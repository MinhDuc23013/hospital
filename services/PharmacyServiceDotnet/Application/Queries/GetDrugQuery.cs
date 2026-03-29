using MediatR;
using PharmacyServiceDotnet.Application;

namespace PharmacyServiceDotnet.Application.Queries;

public record GetDrugQuery(Guid Id) : IRequest<DrugResult?>;
