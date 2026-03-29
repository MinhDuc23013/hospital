using MediatR;
using PharmacyServiceDotnet.Application;

namespace PharmacyServiceDotnet.Application.Queries;

public record GetPrescriptionQuery(Guid Id) : IRequest<PrescriptionResult?>;
