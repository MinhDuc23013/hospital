using FileService.Domain.Entities;

namespace FileService.Infrastructure.Repositories;

public interface IFileRepository
{
    /// <summary>Fetch metadata only (no Content bytes) — use for get-metadata/delete.</summary>
    Task<StoredFile?> GetMetadataAsync(Guid id, CancellationToken ct = default);

    /// <summary>Fetch metadata joined with binary content — use for download only.</summary>
    Task<(StoredFile Metadata, byte[] Content)?> GetWithContentAsync(Guid id, CancellationToken ct = default);

    Task<(List<StoredFile> Items, int Total)> ListAsync(int page, int pageSize, CancellationToken ct = default);

    /// <summary>Adds the metadata row and its associated content row in one call.</summary>
    Task AddAsync(StoredFile file, byte[] content, CancellationToken ct = default);

    Task SaveChangesAsync(CancellationToken ct = default);

    /// <summary>Persists the Google Drive file id + edit URL for a file, after first successful conversion.</summary>
    Task SetDriveInfoAsync(Guid id, string driveFileId, string driveEditUrl, CancellationToken ct = default);

    /// <summary>Replaces stored content bytes + size/hash metadata — used by the sync-from-Drive flow.</summary>
    Task UpdateContentAsync(Guid id, byte[] newContent, long sizeBytes, string sha256, CancellationToken ct = default);

    /// <summary>Clears the Drive link after the Drive-side copy has been deleted post-sync.</summary>
    Task ClearDriveInfoAsync(Guid id, CancellationToken ct = default);
}
