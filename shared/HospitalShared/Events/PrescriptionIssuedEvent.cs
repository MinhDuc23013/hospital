namespace HospitalShared.Events;

/// <summary>Published when a prescription is issued by a provider.</summary>
public class PrescriptionIssuedEvent
{
    public Guid PrescriptionId { get; set; }
    public Guid PatientId { get; set; }
    public string DrugId { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.Now;
}
