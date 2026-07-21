using FileService.Domain.Entities;
using FileService.Domain.Exceptions;
using FileService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FileService.Infrastructure.Repositories;

public class FileRepository : IFileRepository
{
    private readonly FileDbContext _context;
    public FileRepository(FileDbContext context) => _context = context;

    public Task<StoredFile?> GetMetadataAsync(Guid id, CancellationToken ct = default)
        => _context.Files.FirstOrDefaultAsync(f => f.Id == id && f.IsActive, ct);

    public async Task<(StoredFile Metadata, byte[] Content)?> GetWithContentAsync(Guid id, CancellationToken ct = default)
    {
        var result = await _context.Files
            .Where(f => f.Id == id && f.IsActive)
            .Join(_context.Contents, f => f.Id, c => c.Id, (f, c) => new { Metadata = f, c.Content })
            .FirstOrDefaultAsync(ct);

        return result is null ? null : (result.Metadata, result.Content);
    }

    public async Task<(List<StoredFile> Items, int Total)> ListAsync(int page, int pageSize, CancellationToken ct = default)
    {
        var query = _context.Files.Where(f => f.IsActive).OrderByDescending(f => f.UploadedAt);
        var total = await query.CountAsync(ct);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return (items, total);
    }

    public async Task AddAsync(StoredFile file, byte[] content, CancellationToken ct = default)
    {
        await _context.Files.AddAsync(file, ct);
        await _context.Contents.AddAsync(StoredFileContent.Create(file.Id, content), ct);
    }

    public Task SaveChangesAsync(CancellationToken ct = default)
        => _context.SaveChangesAsync(ct);

    public async Task SetDriveInfoAsync(Guid id, string driveFileId, string driveEditUrl, CancellationToken ct = default)
    {
        var file = await _context.Files.FirstOrDefaultAsync(f => f.Id == id, ct)
            ?? throw new NotFoundException("StoredFile", id);

        file.SetDriveInfo(driveFileId, driveEditUrl);
        await _context.SaveChangesAsync(ct);
    }

    public async Task UpdateContentAsync(Guid id, byte[] newContent, long sizeBytes, string sha256, CancellationToken ct = default)
    {
        var file = await _context.Files.FirstOrDefaultAsync(f => f.Id == id, ct)
            ?? throw new NotFoundException("StoredFile", id);
        var content = await _context.Contents.FirstOrDefaultAsync(c => c.Id == id, ct)
            ?? throw new NotFoundException("StoredFile", id);

        file.UpdateContentMetadata(sizeBytes, sha256);
        content.ReplaceContent(newContent);
        await _context.SaveChangesAsync(ct);
    }

    public async Task ClearDriveInfoAsync(Guid id, CancellationToken ct = default)
    {
        var file = await _context.Files.FirstOrDefaultAsync(f => f.Id == id, ct)
            ?? throw new NotFoundException("StoredFile", id);

        file.ClearDriveInfo();
        await _context.SaveChangesAsync(ct);
    }
}
