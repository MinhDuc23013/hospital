namespace SearchServiceDotnet.Application.Models;

/// <summary>Elasticsearch document model for the hospital-doctors index.</summary>
public class DoctorDocument
{
    public string DoctorId  { get; set; } = string.Empty;
    public string FullName  { get; set; } = string.Empty;
    public string Specialty { get; set; } = string.Empty;
    public string? Phone    { get; set; }
    public string? Email    { get; set; }
    public bool   IsActive  { get; set; }
    public DateTime CreatedAt { get; set; }
}
