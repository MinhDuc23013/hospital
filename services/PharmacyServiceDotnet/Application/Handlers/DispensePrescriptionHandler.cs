using HospitalShared.Events;
using MediatR;
using PharmacyServiceDotnet.Application;
using PharmacyServiceDotnet.Application.Commands;
using PharmacyServiceDotnet.Domain.Exceptions;
using PharmacyServiceDotnet.Infrastructure.MessageBus;
using PharmacyServiceDotnet.Infrastructure.Repositories;

namespace PharmacyServiceDotnet.Application.Handlers;

public class DispensePrescriptionHandler : IRequestHandler<DispensePrescriptionCommand, PrescriptionResult>
{
    private readonly IPrescriptionRepository _prescriptionRepo;
    private readonly IDrugRepository _drugRepo;
    private readonly EventPublisher _publisher;

    public DispensePrescriptionHandler(
        IPrescriptionRepository prescriptionRepo,
        IDrugRepository drugRepo,
        EventPublisher publisher)
    {
        _prescriptionRepo = prescriptionRepo;
        _drugRepo = drugRepo;
        _publisher = publisher;
    }

    public async Task<PrescriptionResult> Handle(DispensePrescriptionCommand cmd, CancellationToken ct)
    {
        var prescription = await _prescriptionRepo.GetByIdAsync(cmd.Id, ct)
            ?? throw new NotFoundException("Prescription", cmd.Id);

        prescription.Dispense();

        // Decrement stock for each item and publish low-stock events if needed
        var jsonOpts = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var items = System.Text.Json.JsonSerializer.Deserialize<List<PrescriptionItem>>(prescription.Items, jsonOpts) ?? [];
        foreach (var item in items)
        {
            var drug = await _drugRepo.GetByIdAsync(item.DrugId, ct);
            if (drug is null) continue;

            var isLow = drug.UpdateStock(item.Quantity);
            if (isLow)
            {
                await _publisher.PublishAsync(new InventoryLowEvent
                {
                    DrugId = drug.Id.ToString(),
                    DrugName = drug.Name,
                    CurrentStock = drug.Quantity,
                    MinimumStock = drug.LowStockThreshold,
                    Timestamp = DateTime.Now
                }, ct);
            }
        }

        await _prescriptionRepo.SaveChangesAsync(ct);
        return PharmacyMapper.ToResult(prescription);
    }
}
