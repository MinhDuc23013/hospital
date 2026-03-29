using MediatR;
using PharmacyServiceDotnet.Application;
using PharmacyServiceDotnet.Application.Commands;
using PharmacyServiceDotnet.Domain.Exceptions;
using PharmacyServiceDotnet.Infrastructure.Repositories;

namespace PharmacyServiceDotnet.Application.Handlers;

public class UpdateDrugHandler : IRequestHandler<UpdateDrugCommand, DrugResult>
{
    private readonly IDrugRepository _repo;
    public UpdateDrugHandler(IDrugRepository repo) => _repo = repo;

    public async Task<DrugResult> Handle(UpdateDrugCommand cmd, CancellationToken ct)
    {
        var drug = await _repo.GetByIdAsync(cmd.Id, ct)
            ?? throw new NotFoundException("Drug", cmd.Id);

        drug.Update(cmd.Name, cmd.Dosage, cmd.Quantity, cmd.Price, cmd.LowStockThreshold);
        await _repo.SaveChangesAsync(ct);
        return PharmacyMapper.ToResult(drug);
    }
}
