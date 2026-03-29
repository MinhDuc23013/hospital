using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HospitalShared.Outbox;

/// <summary>
/// Saves events to the outbox table instead of publishing directly to Kafka.
/// Background worker (OutboxPublishWorker) drains the outbox and publishes to Kafka.
/// Guarantees at-least-once delivery even if Kafka is down.
/// </summary>
public class OutboxEventPublisher
{
    private readonly DbContext _dbContext;
    private readonly ILogger<OutboxEventPublisher> _logger;

    public OutboxEventPublisher(DbContext dbContext, ILogger<OutboxEventPublisher> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <summary>Save event to outbox — returns immediately, Kafka publish handled by background worker.</summary>
    public async Task PublishAsync<T>(T @event, CancellationToken ct = default) where T : class
    {
        var topic = GetTopicName<T>();
        var key = Guid.NewGuid().ToString();
        var value = JsonSerializer.Serialize(@event);

        var outbox = EventOutbox.Create(topic, key, value, typeof(T).Name);
        _dbContext.Set<EventOutbox>().Add(outbox);
        await _dbContext.SaveChangesAsync(ct);

        _logger.LogInformation("Event {EventType} saved to outbox for topic {Topic}", typeof(T).Name, topic);
    }

    /// <summary>Save event to outbox with routing key (compat with PatientService).</summary>
    public Task PublishAsync<T>(T @event, string? routingKey, CancellationToken ct = default) where T : class
        => PublishAsync(@event, ct);

    /// <summary>Save event for a specific topic.</summary>
    public async Task SendAsync<T>(string topic, T @event, CancellationToken ct = default) where T : class
    {
        var key = Guid.NewGuid().ToString();
        var value = JsonSerializer.Serialize(@event);

        var outbox = EventOutbox.Create(topic, key, value, typeof(T).Name);
        _dbContext.Set<EventOutbox>().Add(outbox);
        await _dbContext.SaveChangesAsync(ct);

        _logger.LogInformation("Event {EventType} saved to outbox for topic {Topic}", typeof(T).Name, topic);
    }

    private static string GetTopicName<T>()
    {
        var name = typeof(T).Name.Replace("Event", "");
        var kebab = Regex.Replace(name, "(?<!^)([A-Z])", "-$1").ToLowerInvariant();
        return $"hospital.{kebab}";
    }
}
