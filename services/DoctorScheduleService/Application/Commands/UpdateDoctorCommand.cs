using HospitalShared.DTOs;
using MediatR;

namespace DoctorScheduleService.Application.Commands;

public record UpdateDoctorCommand(
    Guid Id,
    string FullName,
    string Specialty,
    string? Phone,
    string? Email
) : IRequest<DoctorDto>;
