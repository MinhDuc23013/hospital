namespace SearchServiceDotnet.Application.Models;

public class SlotDocument
{
    public string SlotId { get; set; } = string.Empty;
    public string ScheduleId { get; set; } = string.Empty;
    public string DoctorId { get; set; } = string.Empty;
    public string PatientId { get; set; } = string.Empty;
    public DateTime ScheduledTime { get; set; }
    public int DurationMinutes { get; set; }
    public string Status { get; set; } = string.Empty; // Reserved, Booked, Available
    public DateTime? ReservedUntil { get; set; }
}
