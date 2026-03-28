using HospitalShared.DTOs;
using MediatR;

namespace PatientService.Application.Commands;

public record UpdatePatientCommand(
    Guid Id,
    string FirstName,
    string LastName,
    string? PhoneNumber
) : IRequest<PatientDto>;
