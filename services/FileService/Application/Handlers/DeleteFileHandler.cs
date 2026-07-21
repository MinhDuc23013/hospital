using FileService.Application.Commands;
using FileService.Domain.Exceptions;
using FileService.Infrastructure.Repositories;
using MediatR;

namespace FileService.Application.Handlers;

public class DeleteFileHandler : IRequestHandler<DeleteFileCommand>
{
    private readonly IFileRepository _repo;
    public DeleteFileHandler(IFileRepository repo) => _repo = repo;

    public async Task Handle(DeleteFileCommand request, CancellationToken ct)
    {
        var file = await _repo.GetMetadataAsync(request.Id, ct)
            ?? throw new NotFoundException("StoredFile", request.Id);

        file.Deactivate();
        await _repo.SaveChangesAsync(ct);
    }
}
