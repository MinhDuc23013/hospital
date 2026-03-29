using AppointmentService.Application.Commands;
using AppointmentService.Application.Saga;
using AppointmentService.Domain.Exceptions;
using AppointmentService.Infrastructure.Repositories;
using MediatR;

namespace AppointmentService.Application.Handlers;

/// <summary>Handles payment confirmation — finds saga by paymentId then completes it.</summary>
public class ConfirmPaymentHandler : IRequestHandler<ConfirmPaymentCommand>
{
    private readonly BookingSagaOrchestrator _orchestrator;
    private readonly IBookingSagaRepository _sagaRepo;

    public ConfirmPaymentHandler(BookingSagaOrchestrator orchestrator, IBookingSagaRepository sagaRepo)
    {
        _orchestrator = orchestrator;
        _sagaRepo = sagaRepo;
    }

    public async Task Handle(ConfirmPaymentCommand cmd, CancellationToken ct)
    {
        var saga = await _sagaRepo.GetByPaymentIdAsync(cmd.PaymentId, ct)
            ?? throw new NotFoundException("Saga with payment", cmd.PaymentId);

        await _orchestrator.CompleteAfterPaymentAsync(saga.Id, ct);
    }
}
