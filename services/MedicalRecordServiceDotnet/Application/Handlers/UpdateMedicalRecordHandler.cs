using MediatR;
using MedicalRecordServiceDotnet.Application.Commands;
using MedicalRecordServiceDotnet.Domain.Entities;
using MedicalRecordServiceDotnet.Infrastructure.Repositories;

namespace MedicalRecordServiceDotnet.Application.Handlers;

public class UpdateMedicalRecordHandler : IRequestHandler<UpdateMedicalRecordCommand, MedicalRecordResult?>
{
    private readonly IMedicalRecordRepository _repo;

    public UpdateMedicalRecordHandler(IMedicalRecordRepository repo) => _repo = repo;

    public async Task<MedicalRecordResult?> Handle(UpdateMedicalRecordCommand cmd, CancellationToken ct)
    {
        var record = await _repo.GetByIdAsync(cmd.Id, ct);
        if (record is null) return null;

        if (cmd.Findings is not null) record.Findings = cmd.Findings;
        if (cmd.Diagnosis is not null) record.Diagnosis = cmd.Diagnosis;
        if (cmd.LabResults is not null)
        {
            record.LabResults = cmd.LabResults.Select(l => new LabResult
            {
                TestName = l.TestName,
                Result = l.Result,
                NormalRange = l.NormalRange,
                Timestamp = DateTime.UtcNow
            }).ToList();
        }
        record.UpdatedAt = DateTime.UtcNow;

        await _repo.UpdateAsync(record, ct);
        return MedicalRecordMapper.ToResult(record);
    }
}
