using MediatR;
using PharmacyServiceDotnet.Application.Commands;
using PharmacyServiceDotnet.Domain.Entities;
using PharmacyServiceDotnet.Domain.Enums;
using PharmacyServiceDotnet.Infrastructure.Repositories;

namespace PharmacyServiceDotnet.Application.Handlers;

public class CreateDrugBatchHandler : IRequestHandler<CreateDrugBatchCommand, DrugBatchResult>
{
    private readonly IDrugRepository _drugRepo;
    private readonly IDrugBatchRepository _batchRepo;
    private readonly IInventoryAuditLogRepository _auditRepo;

    public CreateDrugBatchHandler(
        IDrugRepository drugRepo,
        IDrugBatchRepository batchRepo,
        IInventoryAuditLogRepository auditRepo)
    {
        _drugRepo = drugRepo;
        _batchRepo = batchRepo;
        _auditRepo = auditRepo;
    }

    public async Task<DrugBatchResult> Handle(CreateDrugBatchCommand cmd, CancellationToken ct)
    {
        var drug = await _drugRepo.GetByIdAsync(cmd.DrugId, ct)
            ?? throw new Domain.Exceptions.NotFoundException("Drug", cmd.DrugId);

        var batch = DrugBatch.Create(cmd.DrugId, cmd.BatchNumber, cmd.ExpiryDate, cmd.Quantity, cmd.ReceivedDate);
        await _batchRepo.AddAsync(batch, ct);

        // Update drug total quantity
        drug.Update(null, null, drug.Quantity + cmd.Quantity, null, null);

        // Audit log: stock received
        var audit = InventoryAuditLog.Create(
            AuditAction.Received, cmd.DrugId, cmd.Quantity,
            drugBatchId: batch.Id, batchNumber: cmd.BatchNumber,
            oldQty: drug.Quantity - cmd.Quantity, newQty: drug.Quantity,
            details: $"Batch {cmd.BatchNumber} received, expiry {cmd.ExpiryDate:yyyy-MM-dd}");
        await _auditRepo.AddAsync(audit, ct);

        await _batchRepo.SaveChangesAsync(ct);
        return PharmacyMapper.ToResult(batch);
    }
}
