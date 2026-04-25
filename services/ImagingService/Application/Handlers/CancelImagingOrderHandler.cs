using ImagingService.Application.Commands;
using ImagingService.Infrastructure.Repositories;
using MediatR;

namespace ImagingService.Application.Handlers;

/// <summary>Cancels an imaging order and persists the status change.</summary>
public class CancelImagingOrderHandler : IRequestHandler<CancelImagingOrderCommand, bool>
{
    private readonly IImagingOrderRepository _repo;

    public CancelImagingOrderHandler(IImagingOrderRepository repo)
    {
        _repo = repo;
    }

    public async Task<bool> Handle(CancelImagingOrderCommand cmd, CancellationToken ct)
    {
        var order = await _repo.GetByIdAsync(cmd.OrderId, ct)
            ?? throw new KeyNotFoundException($"Imaging order {cmd.OrderId} not found.");

        order.Cancel();
        await _repo.SaveChangesAsync(ct);
        return true;
    }
}
