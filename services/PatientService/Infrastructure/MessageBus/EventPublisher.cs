using System.Text.Json;
using Confluent.Kafka;

namespace PatientService.Infrastructure.MessageBus;

/// <summary>Wraps Kafka producer for domain event dispatching (audit/trace).</summary>
public class EventPublisher : IDisposable
{
    private readonly IProducer<string, string> _producer;
    private readonly ILogger<EventPublisher> _logger;

    public EventPublisher(IProducer<string, string> producer, ILogger<EventPublisher> logger)
    {
        _producer = producer;
        _logger = logger;
    }

    /// <summary>Publish event to Kafka topic. routingKey param kept for API compat (ignored).</summary>
    public async Task PublishAsync<T>(T @event, string? routingKey = null, CancellationToken ct = default) where T : class
    {
        var topic = GetTopicName<T>();
        var key = Guid.NewGuid().ToString();
        var value = JsonSerializer.Serialize(@event);

        var result = await _producer.ProduceAsync(topic, new Message<string, string>
        {
            Key = key,
            Value = value
        }, ct);

        _logger.LogInformation("Published {EventType} to Kafka topic {Topic} [partition={Partition}, offset={Offset}]",
            typeof(T).Name, topic, result.Partition.Value, result.Offset.Value);
    }

    private static string GetTopicName<T>()
    {
        var name = typeof(T).Name.Replace("Event", "");
        var kebab = System.Text.RegularExpressions.Regex.Replace(name, "(?<!^)([A-Z])", "-$1").ToLowerInvariant();
        return $"hospital.{kebab}";
    }

    public void Dispose() => _producer?.Dispose();
}
