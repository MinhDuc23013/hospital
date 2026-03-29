using MediatR;
using PharmacyServiceDotnet.Application;
using PharmacyServiceDotnet.Application.Queries;
using PharmacyServiceDotnet.Infrastructure.Repositories;

namespace PharmacyServiceDotnet.Application.Handlers;

public class ListDrugsHandler : IRequestHandler<ListDrugsQuery, (List<DrugResult> Items, int Total)>
{
    private readonly IDrugRepository _repo;
    public ListDrugsHandler(IDrugRepository repo) => _repo = repo;

    public async Task<(List<DrugResult> Items, int Total)> Handle(ListDrugsQuery query, CancellationToken ct)
    {
        var (items, total) = await _repo.ListAsync(query.Name, query.LowStock, query.Page, query.PageSize, ct);
        return (items.Select(PharmacyMapper.ToResult).ToList(), total);
    }
}
