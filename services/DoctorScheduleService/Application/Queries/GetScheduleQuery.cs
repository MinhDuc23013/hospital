using HospitalShared.DTOs;
using MediatR;

namespace DoctorScheduleService.Application.Queries;

public record GetScheduleQuery(Guid Id) : IRequest<DoctorScheduleDto?>;
