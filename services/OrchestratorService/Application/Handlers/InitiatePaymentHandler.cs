using MediatR;
using OrchestratorService.Application.Commands;
using OrchestratorService.Application.Saga;

namespace OrchestratorService.Application.Handlers;

public class InitiatePaymentHandler : IRequestHandler<InitiatePaymentCommand, PaymentSagaResult>
{
    private readonly IPaymentSagaOrchestrator _orchestrator;

    public InitiatePaymentHandler(IPaymentSagaOrchestrator orchestrator)
        => _orchestrator = orchestrator;

    public async Task<PaymentSagaResult> Handle(InitiatePaymentCommand cmd, CancellationToken ct)
    {
        var saga = await _orchestrator.InitiateAsync(
            cmd.AppointmentId, cmd.PatientId, cmd.Method, cmd.Currency, ct);

        return new PaymentSagaResult(
            SagaId: saga.Id,
            AppointmentId: saga.AppointmentId,
            PaymentId: saga.PaymentId,
            Amount: saga.Amount,
            CheckoutUrl: saga.CheckoutUrl,
            Status: saga.CurrentStep.ToString(),
            FailureReason: saga.FailureReason);
    }
}
