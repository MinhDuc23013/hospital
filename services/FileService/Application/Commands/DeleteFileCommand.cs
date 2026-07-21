using MediatR;

namespace FileService.Application.Commands;

public record DeleteFileCommand(Guid Id) : IRequest;
