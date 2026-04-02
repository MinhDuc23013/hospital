using MediatR;
using MedicalRecordServiceDotnet.Application.Queries;
using MedicalRecordServiceDotnet.Infrastructure.Repositories;

namespace MedicalRecordServiceDotnet.Application.Handlers;

public class GetMedicalRecordsByPatientIdHandler
    : IRequestHandler<GetMedicalRecordsByPatientIdQuery, PaginatedResult<MedicalRecordResult>>
{
    private readonly IMedicalRecordRepository _repo;

    public GetMedicalRecordsByPatientIdHandler(IMedicalRecordRepository repo) => _repo = repo;

    public async Task<PaginatedResult<MedicalRecordResult>> Handle(
        GetMedicalRecordsByPatientIdQuery query, CancellationToken ct)
    {
        var (records, total) = await _repo.GetByPatientIdAsync(query.PatientId, query.Page, query.PageSize, ct);
        var items = records.Select(MedicalRecordMapper.ToResult).ToList();
        return new PaginatedResult<MedicalRecordResult>(items, total, query.Page, query.PageSize);
    }
}
