using LabTestService.Application.Commands;
using LabTestService.Infrastructure.Repositories;
using MediatR;

namespace LabTestService.Application.Handlers;

/// <summary>Cancels a pending or in-progress lab order.</summary>
public class CancelLabOrderHandler : IRequestHandler<CancelLabOrderCommand, bool>
{
    private readonly ILabOrderRepository _repo;

    public CancelLabOrderHandler(ILabOrderRepository repo) => _repo = repo;

    public async Task<bool> Handle(CancelLabOrderCommand cmd, CancellationToken ct)
    {
        var order = await _repo.GetByIdAsync(cmd.OrderId, ct)
            ?? throw new KeyNotFoundException($"LabOrder {cmd.OrderId} not found.");

        order.Cancel();
        await _repo.SaveChangesAsync(ct);
        return true;
    }
}
