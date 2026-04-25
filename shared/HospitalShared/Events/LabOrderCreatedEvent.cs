namespace HospitalShared.Events;

public class LabOrderCreatedEvent
{
    public Guid OrderId { get; set; }
    public Guid PatientId { get; set; }
    public Guid AppointmentId { get; set; }
    public string DoctorId { get; set; } = string.Empty;
    public int TestCount { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
