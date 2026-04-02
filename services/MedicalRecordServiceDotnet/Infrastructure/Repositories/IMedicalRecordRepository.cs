using MedicalRecordServiceDotnet.Domain.Entities;

namespace MedicalRecordServiceDotnet.Infrastructure.Repositories;

public interface IMedicalRecordRepository
{
    Task<MedicalRecord?> GetByIdAsync(string id, CancellationToken ct = default);
    Task<(List<MedicalRecord> Items, int Total)> GetByPatientIdAsync(string patientId, int page, int pageSize, CancellationToken ct = default);
    Task CreateAsync(MedicalRecord record, CancellationToken ct = default);
    Task UpdateAsync(MedicalRecord record, CancellationToken ct = default);
}
