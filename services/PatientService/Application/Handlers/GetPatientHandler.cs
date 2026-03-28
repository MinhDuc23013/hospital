using HospitalShared.DTOs;
using MediatR;
using PatientService.Application.Queries;
using PatientService.Infrastructure.Repositories;

namespace PatientService.Application.Handlers;

public class GetPatientHandler : IRequestHandler<GetPatientQuery, PatientDto?>
{
    private readonly IPatientRepository _repo;
    public GetPatientHandler(IPatientRepository repo) => _repo = repo;

    public async Task<PatientDto?> Handle(GetPatientQuery query, CancellationToken ct)
    {
        var patient = await _repo.GetByIdAsync(query.Id, ct);
        return patient is null ? null : CreatePatientHandler.MapToDto(patient);
    }
}
