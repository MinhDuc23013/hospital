namespace HospitalShared.Events;

/// <summary>Published when a payment is completed successfully.</summary>
public class PaymentCompletedEvent
{
    public Guid PaymentId { get; set; }
    public Guid AppointmentId { get; set; }
    public Guid PatientId { get; set; }
    public decimal Amount { get; set; }
    public string Method { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
