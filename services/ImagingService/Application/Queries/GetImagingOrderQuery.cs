using ImagingService.Application.DTOs;
using MediatR;

namespace ImagingService.Application.Queries;

public record GetImagingOrderQuery(Guid Id) : IRequest<ImagingOrderDto?>;
