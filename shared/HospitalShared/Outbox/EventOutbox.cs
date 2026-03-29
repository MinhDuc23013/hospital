namespace HospitalShared.Outbox;

/// <summary>
/// Transactional outbox entry for Kafka events.
/// Events are saved to DB first, then published by a background worker.
/// Guarantees at-least-once delivery even if Kafka is down.
/// </summary>
public class EventOutbox
{
    public Guid Id { get; private set; }
    public string Topic { get; private set; } = string.Empty;
    public string Key { get; private set; } = string.Empty;
    public string Value { get; private set; } = string.Empty;
    public string EventType { get; private set; } = string.Empty;
    public bool IsSent { get; private set; }
    public int RetryCount { get; private set; }
    public string? LastError { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? SentAt { get; private set; }

    private EventOutbox() { }

    public static EventOutbox Create(string topic, string key, string value, string eventType)
    {
        return new EventOutbox
        {
            Id = Guid.NewGuid(),
            Topic = topic,
            Key = key,
            Value = value,
            EventType = eventType,
            IsSent = false,
            RetryCount = 0,
            CreatedAt = DateTime.Now
        };
    }

    public void MarkSent()
    {
        IsSent = true;
        SentAt = DateTime.Now;
    }

    public void MarkFailed(string error)
    {
        RetryCount++;
        LastError = error;
    }
}
