using Microsoft.EntityFrameworkCore;
using PatientService.Domain.Entities;
using PatientService.Infrastructure.Persistence;

namespace PatientService.Infrastructure.Repositories;

public class PatientRepository : IPatientRepository
{
    private readonly PatientDbContext _context;
    public PatientRepository(PatientDbContext context) => _context = context;

    public Task<Patient?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _context.Patients.FirstOrDefaultAsync(p => p.Id == id && p.IsActive, ct);

    public Task<Patient?> GetByEmailAsync(string email, CancellationToken ct = default)
        => _context.Patients.FirstOrDefaultAsync(p => p.Email == email.ToLowerInvariant(), ct);

    public async Task<(List<Patient> Items, int Total)> ListAsync(int page, int pageSize, CancellationToken ct = default)
    {
        var query = _context.Patients.Where(p => p.IsActive).OrderByDescending(p => p.CreatedAt);
        var total = await query.CountAsync(ct);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return (items, total);
    }

    public Task AddAsync(Patient patient, CancellationToken ct = default)
        => _context.Patients.AddAsync(patient, ct).AsTask();

    public Task UpdateAsync(Patient patient, CancellationToken ct = default)
    {
        _context.Patients.Update(patient);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken ct = default)
        => _context.SaveChangesAsync(ct);
}
