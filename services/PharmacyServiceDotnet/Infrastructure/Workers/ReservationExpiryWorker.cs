using Microsoft.EntityFrameworkCore;
using PharmacyServiceDotnet.Domain.Enums;
using PharmacyServiceDotnet.Infrastructure.Persistence;

namespace PharmacyServiceDotnet.Infrastructure.Workers;

/// <summary>
/// Background worker that releases expired stock reservations every 60 seconds.
/// Also cancels associated dispensing sagas stuck in AwaitingPayment.
/// </summary>
public class ReservationExpiryWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ReservationExpiryWorker> _logger;
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(60);

    public ReservationExpiryWorker(IServiceScopeFactory scopeFactory, ILogger<ReservationExpiryWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ReservationExpiryWorker started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ReleaseExpiredReservationsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ReservationExpiryWorker error");
            }

            await Task.Delay(PollInterval, stoppingToken);
        }
    }

    private async Task ReleaseExpiredReservationsAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PharmacyDbContext>();

        // Find expired reservations that are still in Reserved status
        var expired = await db.StockReservations
            .Where(r => r.Status == ReservationStatus.Reserved && r.ExpiresAt < DateTime.Now)
            .ToListAsync(ct);

        if (expired.Count == 0) return;

        // Group by prescription to release together
        var grouped = expired.GroupBy(r => r.PrescriptionId);

        foreach (var group in grouped)
        {
            foreach (var reservation in group)
            {
                // Release the batch reservation
                var batch = await db.DrugBatches.FirstOrDefaultAsync(b => b.Id == reservation.DrugBatchId, ct);
                batch?.Release(reservation.Quantity);
                reservation.Release();
            }

            // Cancel associated saga if stuck in AwaitingPayment
            var saga = await db.DispensingSagas
                .FirstOrDefaultAsync(s => s.PrescriptionId == group.Key
                    && s.CurrentStep == DispensingSagaStep.AwaitingPayment, ct);
            saga?.MarkFailed("Reservation expired — payment timed out");

            _logger.LogWarning("Released {Count} expired reservations for prescription {PrescriptionId}",
                group.Count(), group.Key);
        }

        await db.SaveChangesAsync(ct);
        _logger.LogInformation("ReservationExpiryWorker released {Total} expired reservations", expired.Count);
    }
}
