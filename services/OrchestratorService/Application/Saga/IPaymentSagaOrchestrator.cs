using OrchestratorService.Domain.Entities;

namespace OrchestratorService.Application.Saga;

public interface IPaymentSagaOrchestrator
{
    Task<PaymentSaga> InitiateAsync(
        Guid appointmentId, Guid patientId,
        string method, string currency,
        CancellationToken ct);

    Task HandleCompletedAsync(Guid paymentId, CancellationToken ct);
    Task HandleFailedAsync(Guid paymentId, string? reason, CancellationToken ct);
}
