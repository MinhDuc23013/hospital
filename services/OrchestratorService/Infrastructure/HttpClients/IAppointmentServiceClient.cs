namespace OrchestratorService.Infrastructure.HttpClients;

public interface IAppointmentServiceClient
{
    Task<Guid?> CreateAppointmentAsync(Guid patientId, string doctorId, DateTime scheduledTime,
        int durationMinutes, string? notes, CancellationToken ct = default);
    Task<bool> ConfirmAppointmentAsync(Guid appointmentId, CancellationToken ct = default);
    Task<bool> CancelAppointmentAsync(Guid appointmentId, CancellationToken ct = default);
}
