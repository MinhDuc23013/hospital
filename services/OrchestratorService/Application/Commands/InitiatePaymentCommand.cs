using MediatR;

namespace OrchestratorService.Application.Commands;

/// <summary>Initiate the payment saga for an appointment (Steps 1-3: invoice → create → process).</summary>
public record InitiatePaymentCommand(
    Guid AppointmentId,
    Guid PatientId,
    string Method,
    string Currency = "VND") : IRequest<PaymentSagaResult>;

public record PaymentSagaResult(
    Guid SagaId,
    Guid AppointmentId,
    Guid? PaymentId,
    decimal Amount,
    string? CheckoutUrl,
    string Status,
    string? FailureReason);
