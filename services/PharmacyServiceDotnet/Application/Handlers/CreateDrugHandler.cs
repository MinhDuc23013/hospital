using MediatR;
using PharmacyServiceDotnet.Application;
using PharmacyServiceDotnet.Application.Commands;
using PharmacyServiceDotnet.Domain.Entities;
using PharmacyServiceDotnet.Infrastructure.Repositories;

namespace PharmacyServiceDotnet.Application.Handlers;

public class CreateDrugHandler : IRequestHandler<CreateDrugCommand, DrugResult>
{
    private readonly IDrugRepository _repo;
    public CreateDrugHandler(IDrugRepository repo) => _repo = repo;

    public async Task<DrugResult> Handle(CreateDrugCommand cmd, CancellationToken ct)
    {
        var drug = Drug.Create(cmd.Name, cmd.Code, cmd.Dosage, cmd.Quantity, cmd.Price, cmd.LowStockThreshold);
        await _repo.AddAsync(drug, ct);
        await _repo.SaveChangesAsync(ct);
        return PharmacyMapper.ToResult(drug);
    }
}
