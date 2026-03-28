using HospitalShared.DTOs;
using MediatR;

namespace PatientService.Application.Queries;

public record GetPatientQuery(Guid Id) : IRequest<PatientDto?>;
