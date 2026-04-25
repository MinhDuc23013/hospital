using ImagingService.Application.DTOs;
using MediatR;

namespace ImagingService.Application.Queries;

public record ListImagingOrdersQuery(
    Guid? PatientId,
    Guid? AppointmentId,
    int Page,
    int PageSize
) : IRequest<(List<ImagingOrderDto> Items, int Total)>;
