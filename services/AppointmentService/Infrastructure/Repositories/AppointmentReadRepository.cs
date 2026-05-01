using AppointmentService.Domain.Entities;
using AppointmentService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AppointmentService.Infrastructure.Repositories;

public class AppointmentReadRepository : IAppointmentReadRepository
{
    private readonly AppointmentReadDbContext _context;
    public AppointmentReadRepository(AppointmentReadDbContext context) => _context = context;

    public Task<Appointment?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _context.Appointments.FirstOrDefaultAsync(a => a.Id == id, ct);

    public async Task<(List<Appointment> Items, int Total)> ListAsync(
        Guid? patientId, string? providerId, int page, int pageSize, CancellationToken ct = default)
    {
        var query = _context.Appointments.AsQueryable();
        if (patientId.HasValue) query = query.Where(a => a.PatientId == patientId);
        if (!string.IsNullOrEmpty(providerId)) query = query.Where(a => a.DoctorId == providerId);
        query = query.OrderByDescending(a => a.ScheduledTime);
        var total = await query.CountAsync(ct);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return (items, total);
    }
}
