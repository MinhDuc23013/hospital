namespace HospitalShared.Events;

/// <summary>Published when a doctor creates a new imaging order (X-ray, CT, MRI, etc.).</summary>
public class ImagingOrderCreatedEvent
{
    public Guid OrderId { get; set; }
    public Guid PatientId { get; set; }
    public Guid AppointmentId { get; set; }
    public string DoctorId { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string BodyPart { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
