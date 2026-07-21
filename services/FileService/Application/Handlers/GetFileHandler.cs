using FileService.Application.DTOs;
using FileService.Application.Queries;
using FileService.Infrastructure.Repositories;
using MediatR;

namespace FileService.Application.Handlers;

public class GetFileHandler : IRequestHandler<GetFileQuery, FileDownloadDto?>
{
    private readonly IFileRepository _repo;
    public GetFileHandler(IFileRepository repo) => _repo = repo;

    public async Task<FileDownloadDto?> Handle(GetFileQuery query, CancellationToken ct)
    {
        var result = await _repo.GetWithContentAsync(query.Id, ct);
        if (result is null) return null;

        return new FileDownloadDto
        {
            FileName = result.Value.Metadata.FileName,
            ContentType = result.Value.Metadata.ContentType,
            Content = result.Value.Content
        };
    }
}
