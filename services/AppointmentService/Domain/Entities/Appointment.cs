using AppointmentService.Domain.Enums;

namespace AppointmentService.Domain.Entities;

/// <summary>Appointment aggregate root — links patient with provider for a time slot.</summary>
public class Appointment
{
    public Guid Id { get; private set; }
    public Guid PatientId { get; private set; }
    public string ProviderId { get; private set; } = string.Empty;
    public DateTime ScheduledTime { get; private set; }
    public int DurationMinutes { get; private set; }
    public AppointmentStatus Status { get; private set; }
    public string? Notes { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private Appointment() { } // EF Core

    public static Appointment Create(Guid patientId, string providerId, DateTime scheduledTime, int durationMinutes, string? notes = null)
    {
        return new Appointment
        {
            Id = Guid.NewGuid(),
            PatientId = patientId,
            ProviderId = providerId,
            ScheduledTime = scheduledTime,
            DurationMinutes = durationMinutes,
            Status = AppointmentStatus.Scheduled,
            Notes = notes,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public void Cancel() { Status = AppointmentStatus.Cancelled; UpdatedAt = DateTime.UtcNow; }
    public void Complete() { Status = AppointmentStatus.Completed; UpdatedAt = DateTime.UtcNow; }
}
