namespace HospitalShared.Events;

/// <summary>Published when a payment fails (provider declined or user cancelled).</summary>
public class PaymentFailedEvent
{
    public Guid PaymentId { get; set; }
    public Guid AppointmentId { get; set; }
    public Guid PatientId { get; set; }
    public decimal Amount { get; set; }
    public string? Reason { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.Now;
}
