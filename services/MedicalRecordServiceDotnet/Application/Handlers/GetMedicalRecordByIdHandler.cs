using MediatR;
using MedicalRecordServiceDotnet.Application.Queries;
using MedicalRecordServiceDotnet.Infrastructure.Repositories;

namespace MedicalRecordServiceDotnet.Application.Handlers;

public class GetMedicalRecordByIdHandler : IRequestHandler<GetMedicalRecordByIdQuery, MedicalRecordResult?>
{
    private readonly IMedicalRecordRepository _repo;

    public GetMedicalRecordByIdHandler(IMedicalRecordRepository repo) => _repo = repo;

    public async Task<MedicalRecordResult?> Handle(GetMedicalRecordByIdQuery query, CancellationToken ct)
    {
        var record = await _repo.GetByIdAsync(query.Id, ct);
        return record is null ? null : MedicalRecordMapper.ToResult(record);
    }
}
