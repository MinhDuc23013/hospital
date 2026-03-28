using HospitalShared.DTOs;
using MediatR;

namespace AppointmentService.Application.Queries;

public record GetAppointmentQuery(Guid Id) : IRequest<AppointmentDto?>;
