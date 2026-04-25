using LabTestService.Domain.Entities;
using LabTestService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LabTestService.Infrastructure.Repositories;

/// <summary>EF Core implementation of ILabOrderRepository using LabTestDbContext.</summary>
public class LabOrderRepository : ILabOrderRepository
{
    private readonly LabTestDbContext _context;

    public LabOrderRepository(LabTestDbContext context) => _context = context;

    public Task<LabOrder?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _context.LabOrders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == id, ct);

    public async Task<(List<LabOrder> Items, int Total)> ListAsync(
        Guid? patientId, Guid? appointmentId, int page, int pageSize, CancellationToken ct = default)
    {
        var query = _context.LabOrders.Include(o => o.Items).AsQueryable();

        if (patientId.HasValue)
            query = query.Where(o => o.PatientId == patientId.Value);

        if (appointmentId.HasValue)
            query = query.Where(o => o.AppointmentId == appointmentId.Value);

        query = query.OrderByDescending(o => o.OrderedAt);

        var total = await query.CountAsync(ct);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);

        return (items, total);
    }

    public Task AddAsync(LabOrder order, CancellationToken ct = default)
        => _context.LabOrders.AddAsync(order, ct).AsTask();

    public Task SaveChangesAsync(CancellationToken ct = default)
        => _context.SaveChangesAsync(ct);
}
