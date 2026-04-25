namespace HospitalShared.Events;

/// <summary>Published when a radiologist submits the result for a completed imaging order.</summary>
public class ImagingResultReadyEvent
{
    public Guid OrderId { get; set; }
    public Guid PatientId { get; set; }
    public Guid AppointmentId { get; set; }
    public string DoctorId { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
