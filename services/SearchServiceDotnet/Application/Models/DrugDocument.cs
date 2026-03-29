namespace SearchServiceDotnet.Application.Models;

/// <summary>Elasticsearch document model for the hospital-drugs index.</summary>
public class DrugDocument
{
    public int    Id     { get; set; }
    public string Name   { get; set; } = string.Empty;
    public string Code   { get; set; } = string.Empty;
    public string Dosage { get; set; } = string.Empty;
}
