using MediatR;

namespace PharmacyServiceDotnet.Application.Commands;

/// <summary>Cancels a dispensing saga — releases all stock reservations.</summary>
public record CancelDispensingCommand(Guid SagaId) : IRequest<DispensingSagaResult>;
