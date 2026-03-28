using DoctorScheduleService.Domain.Entities;
using DoctorScheduleService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DoctorScheduleService.Infrastructure.Repositories;

public class DoctorScheduleRepository : IDoctorScheduleRepository
{
    private readonly DoctorScheduleDbContext _context;

    public DoctorScheduleRepository(DoctorScheduleDbContext context) => _context = context;

    public Task<DoctorSchedule?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _context.Schedules
            .Include(s => s.Slots)
            .FirstOrDefaultAsync(s => s.Id == id, ct);

    public async Task<(List<DoctorSchedule> Items, int Total)> ListAsync(
        string? doctorId, DateTime? date, int page, int pageSize, CancellationToken ct = default)
    {
        var query = _context.Schedules.Include(s => s.Slots).AsQueryable();

        if (!string.IsNullOrEmpty(doctorId))
            query = query.Where(s => s.DoctorId == doctorId);

        if (date.HasValue)
            query = query.Where(s => s.Date == date.Value.Date);

        query = query.OrderBy(s => s.Date).ThenBy(s => s.StartTime);

        var total = await query.CountAsync(ct);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return (items, total);
    }

    public Task AddAsync(DoctorSchedule schedule, CancellationToken ct = default)
        => _context.Schedules.AddAsync(schedule, ct).AsTask();

    public Task SaveChangesAsync(CancellationToken ct = default)
        => _context.SaveChangesAsync(ct);
}
