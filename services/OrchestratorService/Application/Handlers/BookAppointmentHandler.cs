using System.Diagnostics;
using MediatR;
using OrchestratorService.Application.Commands;
using OrchestratorService.Application.Saga;

namespace OrchestratorService.Application.Handlers;

/// <summary>MediatR handler that delegates to BookingSagaOrchestrator.</summary>
public class BookAppointmentHandler : IRequestHandler<BookAppointmentCommand, BookAppointmentResult>
{
    private readonly IBookingSagaOrchestrator _orchestrator;

    public BookAppointmentHandler(IBookingSagaOrchestrator orchestrator) => _orchestrator = orchestrator;

    public async Task<BookAppointmentResult> Handle(BookAppointmentCommand cmd, CancellationToken ct)
    {
        var saga = await _orchestrator.ExecuteAsync(
            cmd.PatientId, cmd.DoctorId,
            cmd.ScheduleId, cmd.SlotId,
            cmd.ScheduledTime, cmd.DurationMinutes,
            cmd.Notes, ct);

        return new BookAppointmentResult(
            saga.Id,
            saga.AppointmentId,
            saga.CurrentStep.ToString(),
            saga.FailureReason,
            Activity.Current?.TraceId.ToString());
    }
}
