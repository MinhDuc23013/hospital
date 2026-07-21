using FileService.Application.DTOs;
using FileService.Application.Queries;
using FileService.Infrastructure.Repositories;
using MediatR;

namespace FileService.Application.Handlers;

public class GetFileMetadataHandler : IRequestHandler<GetFileMetadataQuery, FileMetadataDto?>
{
    private readonly IFileRepository _repo;
    public GetFileMetadataHandler(IFileRepository repo) => _repo = repo;

    public async Task<FileMetadataDto?> Handle(GetFileMetadataQuery query, CancellationToken ct)
    {
        var file = await _repo.GetMetadataAsync(query.Id, ct);
        return file is null ? null : UploadFileHandler.MapToDto(file);
    }
}
