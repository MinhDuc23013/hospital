using FileService.Application.DTOs;
using FileService.Application.Queries;
using FileService.Infrastructure.Repositories;
using MediatR;

namespace FileService.Application.Handlers;

public class ListFilesHandler : IRequestHandler<ListFilesQuery, (List<FileMetadataDto> Items, int Total)>
{
    private readonly IFileRepository _repo;
    public ListFilesHandler(IFileRepository repo) => _repo = repo;

    public async Task<(List<FileMetadataDto> Items, int Total)> Handle(ListFilesQuery query, CancellationToken ct)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 200);
        var (items, total) = await _repo.ListAsync(page, pageSize, ct);
        return (items.Select(UploadFileHandler.MapToDto).ToList(), total);
    }
}
