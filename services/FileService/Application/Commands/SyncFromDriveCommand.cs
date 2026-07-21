using FileService.Application.DTOs;
using MediatR;

namespace FileService.Application.Commands;

/// <summary>Pulls the latest Drive-edited content for a file back into Postgres (manual, user-triggered).</summary>
public record SyncFromDriveCommand(Guid Id) : IRequest<FileMetadataDto>;
