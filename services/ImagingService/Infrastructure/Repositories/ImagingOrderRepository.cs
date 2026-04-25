using ImagingService.Domain.Entities;
using ImagingService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ImagingService.Infrastructure.Repositories;

/// <summary>EF Core implementation of IImagingOrderRepository using ImagingDbContext.</summary>
public class ImagingOrderRepository : IImagingOrderRepository
{
    private readonly ImagingDbContext _context;

    public ImagingOrderRepository(ImagingDbContext context) => _context = context;

    public Task<ImagingOrder?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _context.ImagingOrders
            .Include(o => o.Result)
            .FirstOrDefaultAsync(o => o.Id == id, ct);

    public async Task<(List<ImagingOrder> Items, int Total)> ListAsync(
        Guid? patientId, Guid? appointmentId,
        int page, int pageSize,
        CancellationToken ct = default)
    {
        var query = _context.ImagingOrders
            .Include(o => o.Result)
            .AsQueryable();

        if (patientId.HasValue)
            query = query.Where(o => o.PatientId == patientId.Value);

        if (appointmentId.HasValue)
            query = query.Where(o => o.AppointmentId == appointmentId.Value);

        query = query.OrderByDescending(o => o.OrderedAt);

        var total = await query.CountAsync(ct);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, total);
    }

    public Task AddAsync(ImagingOrder order, CancellationToken ct = default)
        => _context.ImagingOrders.AddAsync(order, ct).AsTask();

    public Task SaveChangesAsync(CancellationToken ct = default)
        => _context.SaveChangesAsync(ct);
}
