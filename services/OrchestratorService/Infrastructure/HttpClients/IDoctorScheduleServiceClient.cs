using HospitalShared.DTOs;

namespace OrchestratorService.Infrastructure.HttpClients;

public interface IDoctorScheduleServiceClient
{
    Task<TimeSlotDto?> ReserveSlotAsync(Guid scheduleId, Guid slotId, Guid patientId, CancellationToken ct = default);
    Task<TimeSlotDto?> ConfirmSlotAsync(Guid scheduleId, Guid slotId, Guid appointmentId, CancellationToken ct = default);
    Task<bool> ReleaseSlotAsync(Guid scheduleId, Guid slotId, CancellationToken ct = default);
}
