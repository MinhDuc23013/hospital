using HospitalShared.DTOs;
using MediatR;

namespace DoctorScheduleService.Application.Queries;

public record GetDoctorQuery(Guid Id) : IRequest<DoctorDto?>;
