using MediatR;

namespace PharmacyServiceDotnet.Application.Commands;

/// <summary>
/// Called when payment is confirmed — processes payment, commits reservations, and dispenses prescription.
/// PaymentId is already stored in the saga from the CreatePayment step.
/// </summary>
public record CompleteDispensingPaymentCommand(Guid SagaId) : IRequest<DispensingSagaResult>;
