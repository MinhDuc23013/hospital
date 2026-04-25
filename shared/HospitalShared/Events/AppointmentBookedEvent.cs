namespace HospitalShared.Events;

/// <summary>
/// Published after booking is fully confirmed. Consumed by SearchService to index the appointment.
/// Topic: hospital.appointment-booked
/// </summary>
public class AppointmentBookedEvent
{
    public Guid AppointmentId { get; set; }
    public Guid PatientId { get; set; }
    public string DoctorId { get; set; } = string.Empty;
    public DateTime ScheduledTime { get; set; }
    public int DurationMinutes { get; set; }
    public string Status { get; set; } = "Confirmed";
    public string? Notes { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
