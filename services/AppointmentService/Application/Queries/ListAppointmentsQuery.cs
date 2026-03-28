using HospitalShared.DTOs;
using MediatR;

namespace AppointmentService.Application.Queries;

public record ListAppointmentsQuery(
    Guid? PatientId,
    string? ProviderId,
    int Page = 1,
    int PageSize = 50
) : IRequest<(List<AppointmentDto> Items, int Total)>;
