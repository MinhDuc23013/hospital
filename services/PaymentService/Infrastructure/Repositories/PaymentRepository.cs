using Microsoft.EntityFrameworkCore;
using PaymentService.Domain.Entities;
using PaymentService.Domain.Enums;
using PaymentService.Infrastructure.Persistence;

namespace PaymentService.Infrastructure.Repositories;

public class PaymentRepository : IPaymentRepository
{
    private readonly PaymentDbContext _context;
    public PaymentRepository(PaymentDbContext context) => _context = context;

    public Task<Payment?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _context.Payments.FirstOrDefaultAsync(p => p.Id == id, ct);

    public Task<Payment?> GetByAppointmentIdAsync(Guid appointmentId, CancellationToken ct = default)
        => _context.Payments.FirstOrDefaultAsync(p => p.AppointmentId == appointmentId, ct);

    public async Task<(List<Payment> Items, int Total)> ListAsync(
        Guid? appointmentId, Guid? patientId, PaymentStatus? status,
        int page, int pageSize, CancellationToken ct = default)
    {
        var query = _context.Payments.AsQueryable();
        if (appointmentId.HasValue) query = query.Where(p => p.AppointmentId == appointmentId);
        if (patientId.HasValue) query = query.Where(p => p.PatientId == patientId);
        if (status.HasValue) query = query.Where(p => p.Status == status);
        query = query.OrderByDescending(p => p.CreatedAt);
        var total = await query.CountAsync(ct);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return (items, total);
    }

    public Task AddAsync(Payment payment, CancellationToken ct = default)
        => _context.Payments.AddAsync(payment, ct).AsTask();

    public Task SaveChangesAsync(CancellationToken ct = default)
        => _context.SaveChangesAsync(ct);
}
