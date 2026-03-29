using DoctorScheduleService.Domain.Entities;
using DoctorScheduleService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DoctorScheduleService.Infrastructure.Repositories;

public class DoctorRepository : IDoctorRepository
{
    private readonly DoctorScheduleDbContext _context;
    public DoctorRepository(DoctorScheduleDbContext context) => _context = context;

    public Task<Doctor?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _context.Doctors.FirstOrDefaultAsync(d => d.Id == id, ct);

    public async Task<(List<Doctor> Items, int Total)> ListAsync(
        string? specialty, bool? isActive, int page, int pageSize, CancellationToken ct = default)
    {
        var query = _context.Doctors.AsQueryable();
        if (!string.IsNullOrEmpty(specialty)) query = query.Where(d => d.Specialty == specialty);
        if (isActive.HasValue) query = query.Where(d => d.IsActive == isActive);
        query = query.OrderBy(d => d.FullName);
        var total = await query.CountAsync(ct);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return (items, total);
    }

    public Task AddAsync(Doctor doctor, CancellationToken ct = default)
        => _context.Doctors.AddAsync(doctor, ct).AsTask();

    public Task SaveChangesAsync(CancellationToken ct = default)
        => _context.SaveChangesAsync(ct);
}
