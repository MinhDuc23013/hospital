using MediatR;
using PharmacyServiceDotnet.Application;

namespace PharmacyServiceDotnet.Application.Commands;

public record DispensePrescriptionCommand(Guid Id) : IRequest<PrescriptionResult>;
