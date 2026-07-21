using FileService.Application.DTOs;
using MediatR;

namespace FileService.Application.Queries;

public record ListFilesQuery(int Page, int PageSize) : IRequest<(List<FileMetadataDto> Items, int Total)>;
