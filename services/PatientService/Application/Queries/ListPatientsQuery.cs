using HospitalShared.DTOs;
using MediatR;

namespace PatientService.Application.Queries;

public record ListPatientsQuery(int Page = 1, int PageSize = 50) : IRequest<(List<PatientDto> Items, int Total)>;
