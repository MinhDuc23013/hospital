using System.Text.Json;
using OrchestratorService.Infrastructure.HttpClients;
using OrchestratorService.Infrastructure.Persistence;
using OrchestratorService.Infrastructure.Repositories;

namespace OrchestratorService.Infrastructure.BackgroundJobs;

/// <summary>
/// Background worker that periodically retries failed compensation actions
/// from the outbox table. Uses exponential backoff with max retries.
/// CancelAppointment action calls AppointmentServiceClient (no direct DB).
/// </summary>
public class CompensationRetryWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<CompensationRetryWorker> _logger;
    private static readonly TimeSpan PollingInterval = TimeSpan.FromSeconds(15);

    public CompensationRetryWorker(IServiceScopeFactory scopeFactory, ILogger<CompensationRetryWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("CompensationRetryWorker started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPendingCompensations(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CompensationRetryWorker error during batch processing");
            }

            await Task.Delay(PollingInterval, stoppingToken);
        }
    }

    private async Task ProcessPendingCompensations(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OrchestratorDbContext>();
        var outboxRepo = scope.ServiceProvider.GetRequiredService<ICompensationOutboxRepository>();
        var scheduleClient = scope.ServiceProvider.GetRequiredService<DoctorScheduleServiceClient>();
        var paymentClient = scope.ServiceProvider.GetRequiredService<PaymentServiceClient>();
        var appointmentClient = scope.ServiceProvider.GetRequiredService<AppointmentServiceClient>();

        // Row lock (FOR UPDATE SKIP LOCKED) — replicas share batch without double-refund/release.
        await using var tx = await db.Database.BeginTransactionAsync(ct);

        var pending = await outboxRepo.GetPendingAsync(batchSize: 20, ct);
        if (pending.Count == 0)
        {
            await tx.CommitAsync(ct);
            return;
        }

        _logger.LogInformation("CompensationRetryWorker processing {Count} pending items", pending.Count);

        foreach (var item in pending)
        {
            try
            {
                var success = item.ActionType switch
                {
                    "ReleaseSlot" => await RetryReleaseSlot(item, scheduleClient, ct),
                    "RefundPayment" => await RetryRefundPayment(item, paymentClient, ct),
                    // Cancel via AppointmentService HTTP — no direct DB access
                    "CancelAppointment" => await RetryCancelAppointment(item, appointmentClient, ct),
                    _ => throw new InvalidOperationException($"Unknown action: {item.ActionType}")
                };

                if (success)
                {
                    item.MarkCompleted();
                    _logger.LogInformation("Compensation {Id} ({Action}) completed on retry #{Retry}",
                        item.Id, item.ActionType, item.RetryCount);
                }
            }
            catch (Exception ex)
            {
                item.MarkRetryFailed(ex.Message);
                _logger.LogWarning("Compensation {Id} ({Action}) retry #{Retry} failed: {Error}",
                    item.Id, item.ActionType, item.RetryCount, ex.Message);

                if (item.HasExceededMaxRetries)
                    _logger.LogError("Compensation {Id} ({Action}) EXCEEDED max retries — requires manual intervention",
                        item.Id, item.ActionType);
            }
        }

        await outboxRepo.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
    }

    private static async Task<bool> RetryReleaseSlot(
        Domain.Entities.CompensationOutbox item, DoctorScheduleServiceClient client, CancellationToken ct)
    {
        var payload = JsonSerializer.Deserialize<ReleaseSlotPayload>(item.Payload)!;
        return await client.ReleaseSlotAsync(payload.ScheduleId, payload.SlotId, ct);
    }

    private static async Task<bool> RetryRefundPayment(
        Domain.Entities.CompensationOutbox item, PaymentServiceClient client, CancellationToken ct)
    {
        var payload = JsonSerializer.Deserialize<RefundPaymentPayload>(item.Payload)!;
        return await client.RefundPaymentAsync(payload.PaymentId, ct);
    }

    private static async Task<bool> RetryCancelAppointment(
        Domain.Entities.CompensationOutbox item, AppointmentServiceClient client, CancellationToken ct)
    {
        var payload = JsonSerializer.Deserialize<CancelAppointmentPayload>(item.Payload)!;
        return await client.CancelAppointmentAsync(payload.AppointmentId, ct);
    }
}

// Payload records for JSON deserialization
public record ReleaseSlotPayload(Guid ScheduleId, Guid SlotId);
public record RefundPaymentPayload(Guid PaymentId);
public record CancelAppointmentPayload(Guid AppointmentId);
