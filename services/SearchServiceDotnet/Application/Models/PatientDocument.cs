namespace SearchServiceDotnet.Application.Models;

/// <summary>Elasticsearch document model for the hospital-patients index.</summary>
public class PatientDocument
{
    public string   PatientId { get; set; } = string.Empty;
    public string   FirstName { get; set; } = string.Empty;
    public string   LastName  { get; set; } = string.Empty;
    public string   Email     { get; set; } = string.Empty;
    /// <summary>UTC timestamp when the patient record was created. Used for dashboard week-new-patients metric.</summary>
    public DateTime CreatedAt { get; set; }
}
