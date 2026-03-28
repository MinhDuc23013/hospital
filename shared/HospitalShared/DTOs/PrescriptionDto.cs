namespace HospitalShared.DTOs;

/// <summary>Data transfer object for Prescription entity across services.</summary>
public class PrescriptionDto
{
    public Guid Id { get; set; }
    public Guid PatientId { get; set; }
    public string DrugId { get; set; } = string.Empty;
    public string DrugName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public string Instructions { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending";
    public DateTime IssuedAt { get; set; }
}
