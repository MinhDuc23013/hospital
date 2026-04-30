using HospitalShared;
namespace ImagingService.Domain.Entities;

/// <summary>Radiology report attached to an imaging order after the scan is read.</summary>
public class ImagingResult
{
    public Guid Id { get; private set; }
    public Guid ImagingOrderId { get; private set; }
    public string Findings { get; private set; } = string.Empty;
    public string Impression { get; private set; } = string.Empty;
    public string? ImageUrl { get; private set; }
    public string ReportedBy { get; private set; } = string.Empty;
    public DateTime ReportedAt { get; private set; }

    private ImagingResult() { }

    public static ImagingResult Create(
        Guid imagingOrderId, string findings, string impression,
        string? imageUrl, string reportedBy) =>
        new()
        {
            Id = GuidV7.NewGuid(),
            ImagingOrderId = imagingOrderId,
            Findings = findings,
            Impression = impression,
            ImageUrl = imageUrl,
            ReportedBy = reportedBy,
            ReportedAt = DateTime.UtcNow
        };
}
