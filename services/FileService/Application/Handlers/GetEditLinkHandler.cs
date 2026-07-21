using FileService.Application.Commands;
using FileService.Application.Services;
using FileService.Domain.Exceptions;
using FileService.Infrastructure.Repositories;
using MediatR;

namespace FileService.Application.Handlers;

public class GetEditLinkHandler : IRequestHandler<GetEditLinkCommand, string>
{
    private readonly IFileRepository _repo;
    private readonly GoogleDriveEditService _driveService;

    public GetEditLinkHandler(IFileRepository repo, GoogleDriveEditService driveService)
    {
        _repo = repo;
        _driveService = driveService;
    }

    public async Task<string> Handle(GetEditLinkCommand request, CancellationToken ct)
    {
        var result = await _repo.GetWithContentAsync(request.Id, ct)
            ?? throw new NotFoundException("StoredFile", request.Id);

        var wasAlreadyLinked = !string.IsNullOrWhiteSpace(result.Metadata.DriveEditUrl);

        var (driveFileId, editUrl) = await _driveService.GetOrCreateEditLinkAsync(
            result.Metadata, result.Content, ct);

        if (!wasAlreadyLinked)
            await _repo.SetDriveInfoAsync(request.Id, driveFileId, editUrl, ct);

        return editUrl;
    }
}
