namespace HospitalShared.Events;

/// <summary>Published when a time slot is reserved for a patient.</summary>
public class SlotReservedEvent
{
    public Guid SlotId { get; set; }
    public Guid ScheduleId { get; set; }
    public string DoctorId { get; set; } = string.Empty;
    public Guid PatientId { get; set; }
    public DateTime ScheduledTime { get; set; }
    public int DurationMinutes { get; set; }
    public DateTime ReservedUntil { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.Now;
}
