using HospitalShared.DTOs;
using MediatR;
using PatientService.Application.Queries;
using PatientService.Infrastructure.Repositories;

namespace PatientService.Application.Handlers;

public class ListPatientsHandler : IRequestHandler<ListPatientsQuery, (List<PatientDto> Items, int Total)>
{
    private readonly IPatientRepository _repo;
    public ListPatientsHandler(IPatientRepository repo) => _repo = repo;

    public async Task<(List<PatientDto> Items, int Total)> Handle(ListPatientsQuery query, CancellationToken ct)
    {
        var (patients, total) = await _repo.ListAsync(query.Page, query.PageSize, ct);
        return (patients.Select(CreatePatientHandler.MapToDto).ToList(), total);
    }
}
