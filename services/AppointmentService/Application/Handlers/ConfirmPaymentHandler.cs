using AppointmentService.Application.Commands;
using MediatR;

namespace AppointmentService.Application.Handlers;

/// <summary>
/// Handles ConfirmPaymentCommand — payment confirmation is now managed by OrchestratorService.
/// This handler is kept as a no-op stub so the endpoint contract remains intact for legacy callers.
/// Full saga completion (state update, notification) is handled by OrchestratorService PaymentCompletedConsumer.
/// </summary>
public class ConfirmPaymentHandler : IRequestHandler<ConfirmPaymentCommand>
{
    private readonly ILogger<ConfirmPaymentHandler> _logger;

    public ConfirmPaymentHandler(ILogger<ConfirmPaymentHandler> logger) => _logger = logger;

    public Task Handle(ConfirmPaymentCommand cmd, CancellationToken ct)
    {
        // Payment saga completion is owned by OrchestratorService.
        // AppointmentService receives the appointment confirm call separately via POST /confirm.
        _logger.LogInformation(
            "ConfirmPayment {PaymentId} received — saga completion delegated to OrchestratorService",
            cmd.PaymentId);
        return Task.CompletedTask;
    }
}
