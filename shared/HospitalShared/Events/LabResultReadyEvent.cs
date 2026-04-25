namespace HospitalShared.Events;

public class LabResultReadyEvent
{
    public Guid OrderId { get; set; }
    public Guid PatientId { get; set; }
    public Guid AppointmentId { get; set; }
    public string DoctorId { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
