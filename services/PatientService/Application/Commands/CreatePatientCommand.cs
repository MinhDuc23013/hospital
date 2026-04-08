using HospitalShared.DTOs;
using MediatR;

namespace PatientService.Application.Commands;

public record CreatePatientCommand(
    string Email,
    string FirstName,
    string LastName,
    DateTime DateOfBirth,
    string? PhoneNumber,
    string? Password
) : IRequest<PatientDto>;
