using MediatR;
using PharmacyServiceDotnet.Application;
using PharmacyServiceDotnet.Application.Queries;
using PharmacyServiceDotnet.Infrastructure.Repositories;

namespace PharmacyServiceDotnet.Application.Handlers;

public class GetDrugHandler : IRequestHandler<GetDrugQuery, DrugResult?>
{
    private readonly IDrugRepository _repo;
    public GetDrugHandler(IDrugRepository repo) => _repo = repo;

    public async Task<DrugResult?> Handle(GetDrugQuery query, CancellationToken ct)
    {
        var drug = await _repo.GetByIdAsync(query.Id, ct);
        return drug is null ? null : PharmacyMapper.ToResult(drug);
    }
}
