namespace HospitalShared.DTOs;

/// <summary>Data transfer object for Appointment entity across services.</summary>
public class AppointmentDto
{
    public Guid Id { get; set; }
    public Guid PatientId { get; set; }
    public string ProviderId { get; set; } = string.Empty;
    public DateTime ScheduledTime { get; set; }
    public int DurationMinutes { get; set; }
    public string Status { get; set; } = "Scheduled";
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
}
