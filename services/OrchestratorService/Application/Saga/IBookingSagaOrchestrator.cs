using OrchestratorService.Domain.Entities;

namespace OrchestratorService.Application.Saga;

public interface IBookingSagaOrchestrator
{
    Task<BookingSaga> ExecuteAsync(
        Guid patientId, string providerId,
        Guid scheduleId, Guid slotId,
        DateTime scheduledTime, int durationMinutes,
        string? notes, CancellationToken ct);

    Task ExecuteAsyncPhaseAsync(Guid sagaId, CancellationToken ct);
}
