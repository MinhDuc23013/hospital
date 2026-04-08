using HospitalShared.DTOs;
using MediatR;

namespace DoctorScheduleService.Application.Commands;

public record CreateDoctorCommand(
    string FullName,
    string Specialty,
    string? Phone,
    string? Email,
    string? Password
) : IRequest<DoctorDto>;
