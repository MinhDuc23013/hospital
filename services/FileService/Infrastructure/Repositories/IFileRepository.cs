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
}
