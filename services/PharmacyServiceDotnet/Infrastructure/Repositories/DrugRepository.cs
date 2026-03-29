using Microsoft.EntityFrameworkCore;
using PharmacyServiceDotnet.Domain.Entities;
using PharmacyServiceDotnet.Infrastructure.Persistence;

namespace PharmacyServiceDotnet.Infrastructure.Repositories;

public class DrugRepository : IDrugRepository
{
    private readonly PharmacyDbContext _context;
    public DrugRepository(PharmacyDbContext context) => _context = context;

    public Task<Drug?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _context.Drugs.FirstOrDefaultAsync(d => d.Id == id, ct);

    public Task<Drug?> GetByCodeAsync(string code, CancellationToken ct = default)
        => _context.Drugs.FirstOrDefaultAsync(d => d.Code == code, ct);

    public async Task<(List<Drug> Items, int Total)> ListAsync(
        string? name, bool? lowStock, int page, int pageSize, CancellationToken ct = default)
    {
        var query = _context.Drugs.AsQueryable();
        if (!string.IsNullOrEmpty(name))
            query = query.Where(d => d.Name.Contains(name));
        if (lowStock == true)
            query = query.Where(d => d.Quantity < d.LowStockThreshold);
        query = query.OrderBy(d => d.Name);
        var total = await query.CountAsync(ct);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return (items, total);
    }

    public Task AddAsync(Drug drug, CancellationToken ct = default)
        => _context.Drugs.AddAsync(drug, ct).AsTask();

    public Task SaveChangesAsync(CancellationToken ct = default)
        => _context.SaveChangesAsync(ct);
}
