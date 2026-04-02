using MediatR;
using MedicalRecordServiceDotnet.Application.Commands;
using MedicalRecordServiceDotnet.Domain.Entities;
using MedicalRecordServiceDotnet.Infrastructure.Repositories;

namespace MedicalRecordServiceDotnet.Application.Handlers;

public class CreateMedicalRecordHandler : IRequestHandler<CreateMedicalRecordCommand, MedicalRecordResult>
{
    private readonly IMedicalRecordRepository _repo;
    private readonly ILogger<CreateMedicalRecordHandler> _logger;

    public CreateMedicalRecordHandler(IMedicalRecordRepository repo, ILogger<CreateMedicalRecordHandler> logger)
    {
        _repo = repo;
        _logger = logger;
    }

    public async Task<MedicalRecordResult> Handle(CreateMedicalRecordCommand cmd, CancellationToken ct)
    {
        var record = new MedicalRecord
        {
            PatientId = cmd.PatientId,
            AppointmentId = cmd.AppointmentId,
            Findings = cmd.Findings ?? string.Empty,
            Diagnosis = cmd.Diagnosis ?? [],
            CreatedBy = cmd.CreatedBy,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _repo.CreateAsync(record, ct);
        _logger.LogInformation("Medical record {RecordId} created for patient {PatientId}", record.Id, record.PatientId);
        return MedicalRecordMapper.ToResult(record);
    }
}
