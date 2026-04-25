using PharmacyServiceDotnet.Domain.Entities;

namespace PharmacyServiceDotnet.Infrastructure.Repositories;

public interface IPrescriptionRepository
{
    Task<Prescription?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<List<Prescription>> ListByAppointmentAsync(Guid appointmentId, CancellationToken ct = default);
    Task AddAsync(Prescription prescription, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
