using MediatR;

namespace FileService.Application.Commands;

public record GetEditLinkCommand(Guid Id) : IRequest<string>;
