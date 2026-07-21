using FileService.Application.DTOs;
using MediatR;

namespace FileService.Application.Queries;

public record GetFileQuery(Guid Id) : IRequest<FileDownloadDto?>;
