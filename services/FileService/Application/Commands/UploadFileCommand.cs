using FileService.Application.DTOs;
using MediatR;

namespace FileService.Application.Commands;

public record UploadFileCommand(
    byte[] Content,
    string FileName,
    string ContentType,
    string? UploadedBy
) : IRequest<FileMetadataDto>;
