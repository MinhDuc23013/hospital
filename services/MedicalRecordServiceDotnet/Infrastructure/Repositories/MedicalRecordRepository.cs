using MedicalRecordServiceDotnet.Domain.Entities;
using MongoDB.Driver;

namespace MedicalRecordServiceDotnet.Infrastructure.Repositories;

public class MedicalRecordRepository : IMedicalRecordRepository
{
    private readonly IMongoCollection<MedicalRecord> _collection;

    public MedicalRecordRepository(IMongoDatabase database)
    {
        _collection = database.GetCollection<MedicalRecord>("medicalRecords");

        // Ensure indexes
        var patientIndex = Builders<MedicalRecord>.IndexKeys.Ascending(r => r.PatientId);
        _collection.Indexes.CreateOne(new CreateIndexModel<MedicalRecord>(patientIndex));

        var appointmentIndex = Builders<MedicalRecord>.IndexKeys.Ascending(r => r.AppointmentId);
        _collection.Indexes.CreateOne(new CreateIndexModel<MedicalRecord>(
            appointmentIndex, new CreateIndexOptions { Unique = true, Sparse = true }));
    }

    public async Task<MedicalRecord?> GetByIdAsync(string id, CancellationToken ct = default)
        => await _collection.Find(r => r.Id == id).FirstOrDefaultAsync(ct);

    public async Task<(List<MedicalRecord> Items, int Total)> GetByPatientIdAsync(
        string patientId, int page, int pageSize, CancellationToken ct = default)
    {
        var filter = Builders<MedicalRecord>.Filter.Eq(r => r.PatientId, patientId);
        var total = await _collection.CountDocumentsAsync(filter, cancellationToken: ct);
        var items = await _collection.Find(filter)
            .SortByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Limit(pageSize)
            .ToListAsync(ct);
        return (items, (int)total);
    }

    public async Task<bool> ExistsByAppointmentIdAsync(string appointmentId, CancellationToken ct = default)
        => await _collection.Find(r => r.AppointmentId == appointmentId).AnyAsync(ct);

    public async Task CreateAsync(MedicalRecord record, CancellationToken ct = default)
        => await _collection.InsertOneAsync(record, cancellationToken: ct);

    public async Task UpdateAsync(MedicalRecord record, CancellationToken ct = default)
        => await _collection.ReplaceOneAsync(r => r.Id == record.Id, record, cancellationToken: ct);
}
