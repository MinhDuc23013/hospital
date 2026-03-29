namespace HospitalShared.Events;

/// <summary>Published when an appointment is scheduled or rescheduled.</summary>
public class AppointmentScheduledEvent
{
    public Guid AppointmentId { get; set; }
    public Guid PatientId { get; set; }
    public string DoctorId { get; set; } = string.Empty;
    public DateTime ScheduledTime { get; set; }
    public int DurationMinutes { get; set; }
    public string Status { get; set; } = "Scheduled";
    public DateTime Timestamp { get; set; } = DateTime.Now;
}
