namespace HospitalShared.Events;

/// <summary>Published when drugs are dispensed to a patient from a specific batch.</summary>
public class DrugDispensedEvent
{
    public string DrugId { get; set; } = string.Empty;
    public string DrugName { get; set; } = string.Empty;
    public string BatchNumber { get; set; } = string.Empty;
    public string PatientId { get; set; } = string.Empty;
    public string PrescriptionId { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public string DispensedBy { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.Now;
}
