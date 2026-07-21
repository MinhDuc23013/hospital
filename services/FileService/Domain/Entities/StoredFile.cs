using HospitalShared;

namespace FileService.Domain.Entities;

/// <summary>Stored file metadata — narrow table (no bytea) kept fast for list/select. Binary content lives in <see cref="StoredFileContent"/>.</summary>
public class StoredFile
{
    public Guid Id { get; private set; }
    public string FileName { get; private set; } = string.Empty;
    public string ContentType { get; private set; } = string.Empty;
    public long SizeBytes { get; private set; }
    public string? Sha256 { get; private set; }
    public string? UploadedBy { get; private set; }
    public DateTime UploadedAt { get; private set; }
    public bool IsActive { get; private set; }
    public string? DriveFileId { get; private set; }
    public string? DriveEditUrl { get; private set; }

    private StoredFile() { } // EF Core constructor

    public static StoredFile Create(
        string fileName, string contentType, long sizeBytes, string? uploadedBy, string? sha256)
    {
        return new StoredFile
        {
            Id = GuidV7.NewGuid(),
            FileName = fileName.Trim(),
            ContentType = contentType.Trim(),
            SizeBytes = sizeBytes,
            Sha256 = sha256,
            UploadedBy = uploadedBy,
            UploadedAt = DateTime.UtcNow,
            IsActive = true
        };
    }

    public void Deactivate() => IsActive = false;

    public void SetDriveInfo(string driveFileId, string driveEditUrl)
    {
        DriveFileId = driveFileId;
        DriveEditUrl = driveEditUrl;
    }

    /// <summary>Clears the Drive link after the Drive-side copy is deleted post-sync.</summary>
    public void ClearDriveInfo()
    {
        DriveFileId = null;
        DriveEditUrl = null;
    }

    /// <summary>Updates size/hash metadata after content is replaced (e.g. synced back from Drive).</summary>
    public void UpdateContentMetadata(long sizeBytes, string sha256)
    {
        SizeBytes = sizeBytes;
        Sha256 = sha256;
    }
}
