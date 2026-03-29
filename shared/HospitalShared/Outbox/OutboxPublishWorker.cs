using Confluent.Kafka;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace HospitalShared.Outbox;

/// <summary>
/// Background worker that polls the event_outbox table and publishes pending events to Kafka.
/// Runs every 5 seconds. Failed events are retried with logged errors.
/// </summary>
public class OutboxPublishWorker<TDbContext> : BackgroundService where TDbContext : DbContext
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IProducer<string, string> _producer;
    private readonly ILogger<OutboxPublishWorker<TDbContext>> _logger;
    private static readonly TimeSpan PollingInterval = TimeSpan.FromSeconds(5);
    private const int BatchSize = 50;
    private const int MaxRetries = 3;

    public OutboxPublishWorker(
        IServiceScopeFactory scopeFactory,
        IProducer<string, string> producer,
        ILogger<OutboxPublishWorker<TDbContext>> logger)
    {
        _scopeFactory = scopeFactory;
        _producer = producer;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("OutboxPublishWorker started (poll={Poll}s, batch={Batch})",
            PollingInterval.TotalSeconds, BatchSize);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPendingEventsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "OutboxPublishWorker error");
            }

            await Task.Delay(PollingInterval, stoppingToken);
        }
    }

    private async Task ProcessPendingEventsAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TDbContext>();

        var pending = await db.Set<EventOutbox>()
            .Where(e => !e.IsSent && e.RetryCount < MaxRetries)
            .OrderBy(e => e.CreatedAt)
            .Take(BatchSize)
            .ToListAsync(ct);

        if (pending.Count == 0) return;

        _logger.LogInformation("OutboxPublishWorker processing {Count} pending events", pending.Count);

        foreach (var item in pending)
        {
            try
            {
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                cts.CancelAfter(TimeSpan.FromSeconds(10));
                await _producer.ProduceAsync(item.Topic, new Message<string, string>
                {
                    Key = item.Id.ToString(),  // Use outbox ID as key for idempotent dedup
                    Value = item.Value,
                    Headers = new Headers { { "outbox-id", System.Text.Encoding.UTF8.GetBytes(item.Id.ToString()) } }
                }, cts.Token);

                item.MarkSent();
                _logger.LogInformation("Outbox published {EventType} to {Topic}", item.EventType, item.Topic);
            }
            catch (Exception ex)
            {
                item.MarkFailed(ex.Message);
                if (item.RetryCount >= MaxRetries)
                    _logger.LogError("Outbox DEAD LETTER: {EventType} to {Topic} failed after {MaxRetries} retries. Id={Id}",
                        item.EventType, item.Topic, MaxRetries, item.Id);
                else
                    _logger.LogWarning(ex, "Outbox retry {Retry}/{Max} failed for {EventType} to {Topic}",
                        item.RetryCount, MaxRetries, item.EventType, item.Topic);
            }
        }

        await db.SaveChangesAsync(ct);
    }
}
