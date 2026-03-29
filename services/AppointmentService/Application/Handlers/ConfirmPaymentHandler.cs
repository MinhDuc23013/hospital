using AppointmentService.Application.Commands;
using AppointmentService.Application.Saga;
using MediatR;

namespace AppointmentService.Application.Handlers;

/// <summary>Handles payment confirmation — completes the booking saga after external payment success.</summary>
public class ConfirmPaymentHandler : IRequestHandler<ConfirmPaymentCommand>
{
    private readonly BookingSagaOrchestrator _orchestrator;

    public ConfirmPaymentHandler(BookingSagaOrchestrator orchestrator) => _orchestrator = orchestrator;

    public async Task Handle(ConfirmPaymentCommand cmd, CancellationToken ct)
    {
        await _orchestrator.CompleteAfterPaymentAsync(cmd.SagaId, ct);
    }
}
