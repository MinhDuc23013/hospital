using LabTestService.Application.DTOs;
using MediatR;

namespace LabTestService.Application.Queries;

public record ListLabOrdersQuery(
    Guid? PatientId,
    Guid? AppointmentId,
    int Page,
    int PageSize
) : IRequest<(List<LabOrderDto> Items, int Total)>;
