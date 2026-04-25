namespace HospitalShared.Events;

/// <summary>
/// Published at end of sync booking phase (slot locked, appointment created).
/// Consumed by BookingAsyncPhaseConsumer to trigger confirm → notify → index steps.
/// </summary>
public class BookingSlotLockedEvent
{
    public Guid SagaId { get; set; }
    public Guid AppointmentId { get; set; }
    public Guid PatientId { get; set; }
    public string DoctorId { get; set; } = string.Empty;
    public Guid ScheduleId { get; set; }
    public Guid SlotId { get; set; }
    public DateTime ScheduledTime { get; set; }
    public int DurationMinutes { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
