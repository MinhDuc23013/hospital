namespace FileService.Application.DTOs;

/// <summary>Metadata-only view of a stored file — never carries the raw content bytes.</summary>
public class FileMetadataDto
{
    public Guid Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public string? Sha256 { get; set; }
    public string? UploadedBy { get; set; }
    public DateTime UploadedAt { get; set; }
    public bool HasDriveLink { get; set; }
}
