using System.Security.Cryptography;
using FileService.Application.Commands;
using FileService.Application.DTOs;
using FileService.Application.Services;
using FileService.Domain.Entities;
using FileService.Infrastructure.Repositories;
using MediatR;

namespace FileService.Application.Handlers;

public class UploadFileHandler : IRequestHandler<UploadFileCommand, FileMetadataDto>
{
    private readonly IFileRepository _repo;
    private readonly FileValidationService _validator;

    public UploadFileHandler(IFileRepository repo, FileValidationService validator)
    {
        _repo = repo;
        _validator = validator;
    }

    public async Task<FileMetadataDto> Handle(UploadFileCommand cmd, CancellationToken ct)
    {
        var safeFileName = FileValidationService.SanitizeFileName(cmd.FileName);
        _validator.Validate(safeFileName, cmd.ContentType, cmd.Content.LongLength);

        var sha256 = Convert.ToHexString(SHA256.HashData(cmd.Content)).ToLowerInvariant();

        var file = StoredFile.Create(safeFileName, cmd.ContentType, cmd.Content.LongLength, cmd.UploadedBy, sha256);
        await _repo.AddAsync(file, cmd.Content, ct);
        await _repo.SaveChangesAsync(ct);

        return MapToDto(file);
    }

    internal static FileMetadataDto MapToDto(StoredFile f) => new()
    {
        Id = f.Id,
        FileName = f.FileName,
        ContentType = f.ContentType,
        SizeBytes = f.SizeBytes,
        Sha256 = f.Sha256,
        UploadedBy = f.UploadedBy,
        UploadedAt = f.UploadedAt,
        HasDriveLink = !string.IsNullOrWhiteSpace(f.DriveFileId)
    };
}
