using FileService.Application.DTOs;
using MediatR;

namespace FileService.Application.Queries;

public record GetFileMetadataQuery(Guid Id) : IRequest<FileMetadataDto?>;
