namespace OrchestratorService.Domain.Entities;

/// <summary>
/// Idempotency record — one row per Kafka message successfully processed.
/// Inserted inside the same DB transaction as business logic, so duplicate
/// Kafka deliveries hit the unique constraint and are safely skipped.
/// </summary>
public class ProcessedEvent
{
    public Guid EventId { get; private set; }
    public string ConsumerGroup { get; private set; } = string.Empty;
    public DateTime ProcessedAt { get; private set; }

    private ProcessedEvent() { }

    public static ProcessedEvent Create(Guid eventId, string consumerGroup) => new()
    {
        EventId = eventId,
        ConsumerGroup = consumerGroup,
        ProcessedAt = DateTime.UtcNow
    };
}
