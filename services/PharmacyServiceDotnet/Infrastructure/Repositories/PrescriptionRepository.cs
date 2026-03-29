using Microsoft.EntityFrameworkCore;
using PharmacyServiceDotnet.Domain.Entities;
using PharmacyServiceDotnet.Infrastructure.Persistence;

namespace PharmacyServiceDotnet.Infrastructure.Repositories;

public class PrescriptionRepository : IPrescriptionRepository
{
    private readonly PharmacyDbContext _context;
    public PrescriptionRepository(PharmacyDbContext context) => _context = context;

    public Task<Prescription?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _context.Prescriptions.FirstOrDefaultAsync(p => p.Id == id, ct);

    public Task AddAsync(Prescription prescription, CancellationToken ct = default)
        => _context.Prescriptions.AddAsync(prescription, ct).AsTask();

    public Task SaveChangesAsync(CancellationToken ct = default)
        => _context.SaveChangesAsync(ct);
}
