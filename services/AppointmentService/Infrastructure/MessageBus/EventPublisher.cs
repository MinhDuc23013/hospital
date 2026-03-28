using System.Text.Json;
using Confluent.Kafka;

namespace AppointmentService.Infrastructure.MessageBus;

/// <summary>Wraps Kafka producer for domain event dispatching.</summary>
public class EventPublisher : IDisposable
{
    private readonly IProducer<string, string> _producer;
    private readonly ILogger<EventPublisher> _logger;

    public EventPublisher(IProducer<string, string> producer, ILogger<EventPublisher> logger)
    {
        _producer = producer;
        _logger = logger;
    }

    /// <summary>Publish event to a Kafka topic derived from event type name.</summary>
    public async Task PublishAsync<T>(T @event, CancellationToken ct = default) where T : class
    {
        var topic = GetTopicName<T>();
        var key = Guid.NewGuid().ToString();
        var value = JsonSerializer.Serialize(@event);

        var result = await _producer.ProduceAsync(topic, new Message<string, string>
        {
            Key = key,
            Value = value
        }, ct);

        _logger.LogInformation("Published {EventType} to topic {Topic} [partition={Partition}, offset={Offset}]",
            typeof(T).Name, topic, result.Partition.Value, result.Offset.Value);
    }

    /// <summary>Send event to a specific topic (point-to-point).</summary>
    public async Task SendAsync<T>(string topic, T @event, CancellationToken ct = default) where T : class
    {
        var key = Guid.NewGuid().ToString();
        var value = JsonSerializer.Serialize(@event);

        await _producer.ProduceAsync(topic, new Message<string, string>
        {
            Key = key,
            Value = value
        }, ct);

        _logger.LogInformation("Sent {EventType} to topic {Topic}", typeof(T).Name, topic);
    }

    /// <summary>Derives Kafka topic name from event type: AppointmentScheduledEvent → appointment-scheduled</summary>
    private static string GetTopicName<T>()
    {
        var name = typeof(T).Name.Replace("Event", "");
        // PascalCase → kebab-case
        var kebab = System.Text.RegularExpressions.Regex.Replace(name, "(?<!^)([A-Z])", "-$1").ToLowerInvariant();
        return $"hospital.{kebab}";
    }

    public void Dispose() => _producer?.Dispose();
}
