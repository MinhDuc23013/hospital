using AppointmentService.Domain.Entities;
using AppointmentService.Domain.Enums;
using AppointmentService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AppointmentService.Infrastructure.Repositories;

public class BookingSagaRepository : IBookingSagaRepository
{
    private readonly AppointmentDbContext _context;
    public BookingSagaRepository(AppointmentDbContext context) => _context = context;

    public Task<BookingSaga?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _context.BookingSagas.FirstOrDefaultAsync(s => s.Id == id, ct);

    public Task<BookingSaga?> GetByAppointmentIdAsync(Guid appointmentId, CancellationToken ct = default)
        => _context.BookingSagas.FirstOrDefaultAsync(s => s.AppointmentId == appointmentId, ct);

    public Task<List<BookingSaga>> GetByStepOlderThanAsync(BookingSagaStep step, DateTime cutoff, CancellationToken ct = default)
        => _context.BookingSagas
            .Where(s => s.CurrentStep == step && s.UpdatedAt <= cutoff)
            .ToListAsync(ct);

    public Task AddAsync(BookingSaga saga, CancellationToken ct = default)
        => _context.BookingSagas.AddAsync(saga, ct).AsTask();

    public Task SaveChangesAsync(CancellationToken ct = default)
        => _context.SaveChangesAsync(ct);
}
