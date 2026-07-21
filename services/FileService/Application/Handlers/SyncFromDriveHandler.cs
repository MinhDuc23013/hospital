using System.Security.Cryptography;
using FileService.Application.Commands;
using FileService.Application.DTOs;
using FileService.Application.Services;
using FileService.Domain.Exceptions;
using FileService.Infrastructure.Repositories;
using MediatR;

namespace FileService.Application.Handlers;

public class SyncFromDriveHandler : IRequestHandler<SyncFromDriveCommand, FileMetadataDto>
{
    private readonly IFileRepository _repo;
    private readonly GoogleDriveEditService _driveService;
    private readonly ILogger<SyncFromDriveHandler> _logger;

    public SyncFromDriveHandler(IFileRepository repo, GoogleDriveEditService driveService, ILogger<SyncFromDriveHandler> logger)
    {
        _repo = repo;
        _driveService = driveService;
        _logger = logger;
    }

    public async Task<FileMetadataDto> Handle(SyncFromDriveCommand cmd, CancellationToken ct)
    {
        var metadata = await _repo.GetMetadataAsync(cmd.Id, ct)
            ?? throw new NotFoundException("StoredFile", cmd.Id);

        if (string.IsNullOrWhiteSpace(metadata.DriveFileId))
            throw new NotSupportedException("File is not linked to Google Drive yet — use Edit first to create the link.");

        var content = await _driveService.ExportFileAsync(metadata.DriveFileId!, metadata.ContentType, ct);
        var sha256 = Convert.ToHexString(SHA256.HashData(content)).ToLowerInvariant();

        await _repo.UpdateContentAsync(cmd.Id, content, content.LongLength, sha256, ct);

        // Drive was only a scratch pad for editing — the DB is now source of truth again,
        // so delete the Drive copy to avoid burning the connected account's storage quota.
        // If delete fails, leave the Drive link in place so a future sync retries cleanup
        // (data safety isn't at risk either way — the sync above already landed in Postgres).
        try
        {
            await _driveService.DeleteFileAsync(metadata.DriveFileId!, ct);
            await _repo.ClearDriveInfoAsync(cmd.Id, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to delete Drive file {DriveFileId} after sync — will retry on next sync", metadata.DriveFileId);
        }

        var updated = await _repo.GetMetadataAsync(cmd.Id, ct)
            ?? throw new NotFoundException("StoredFile", cmd.Id);
        return UploadFileHandler.MapToDto(updated);
    }
}
