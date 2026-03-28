using AppointmentService.Domain.Entities;
using AppointmentService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AppointmentService.Infrastructure.Repositories;

public class AppointmentRepository : IAppointmentRepository
{
    private readonly AppointmentDbContext _context;
    public AppointmentRepository(AppointmentDbContext context) => _context = context;

    public Task<Appointment?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _context.Appointments.FirstOrDefaultAsync(a => a.Id == id, ct);

    public async Task<(List<Appointment> Items, int Total)> ListAsync(
        Guid? patientId, string? providerId, int page, int pageSize, CancellationToken ct = default)
    {
        var query = _context.Appointments.AsQueryable();
        if (patientId.HasValue) query = query.Where(a => a.PatientId == patientId);
        if (!string.IsNullOrEmpty(providerId)) query = query.Where(a => a.ProviderId == providerId);
        query = query.OrderByDescending(a => a.ScheduledTime);
        var total = await query.CountAsync(ct);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return (items, total);
    }

    public Task AddAsync(Appointment appt, CancellationToken ct = default)
        => _context.Appointments.AddAsync(appt, ct).AsTask();

    public Task SaveChangesAsync(CancellationToken ct = default)
        => _context.SaveChangesAsync(ct);
}
