using System.Diagnostics;
using MediatR;
using OrchestratorService.Application.Commands;
using OrchestratorService.Application.Saga;

namespace OrchestratorService.Application.Handlers;

/// <summary>MediatR handler that delegates to BookingSagaOrchestrator.</summary>
public class BookAppointmentHandler : IRequestHandler<BookAppointmentCommand, BookAppointmentResult>
{
    private readonly BookingSagaOrchestrator _orchestrator;

    public BookAppointmentHandler(BookingSagaOrchestrator orchestrator) => _orchestrator = orchestrator;

    public async Task<BookAppointmentResult> Handle(BookAppointmentCommand cmd, CancellationToken ct)
    {
        var saga = await _orchestrator.ExecuteAsync(
            cmd.PatientId, cmd.DoctorId,
            cmd.ScheduleId, cmd.SlotId,
            cmd.ScheduledTime, cmd.DurationMinutes,
            cmd.PaymentAmount, cmd.PaymentMethod, cmd.Currency,
            cmd.Notes, ct);

        return new BookAppointmentResult(
            saga.Id,
            saga.AppointmentId,
            saga.PaymentId,
            saga.CurrentStep.ToString(),
            saga.FailureReason,
            Activity.Current?.TraceId.ToString());
    }
}
