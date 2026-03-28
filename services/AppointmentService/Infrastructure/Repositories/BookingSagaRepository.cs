using AppointmentService.Domain.Entities;
using AppointmentService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AppointmentService.Infrastructure.Repositories;

public class BookingSagaRepository : IBookingSagaRepository
{
    private readonly AppointmentDbContext _context;
    public BookingSagaRepository(AppointmentDbContext context) => _context = context;

    public Task<BookingSaga?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _context.BookingSagas.FirstOrDefaultAsync(s => s.Id == id, ct);

    public Task AddAsync(BookingSaga saga, CancellationToken ct = default)
        => _context.BookingSagas.AddAsync(saga, ct).AsTask();

    public Task SaveChangesAsync(CancellationToken ct = default)
        => _context.SaveChangesAsync(ct);
}
