using HospitalShared;
namespace OrchestratorService.Domain.Entities;

/// <summary>
/// Stores failed compensation actions for background retry.
/// When a compensation step fails (e.g. service down), it's saved here
/// and retried by a background worker until success or max retries exceeded.
/// </summary>
public class CompensationOutbox
{
    public Guid Id { get; private set; }
    public Guid SagaId { get; private set; }
    public string ActionType { get; private set; } = string.Empty;  // RefundPayment, ReleaseSlot, CancelAppointment
    public string Payload { get; private set; } = string.Empty;     // JSON with IDs needed for the action
    public int RetryCount { get; private set; }
    public int MaxRetries { get; private set; }
    public DateTime? NextRetryAt { get; private set; }
    public bool IsCompleted { get; private set; }
    public string? LastError { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private CompensationOutbox() { }

    public static CompensationOutbox Create(Guid sagaId, string actionType, string payload, int maxRetries = 10)
    {
        return new CompensationOutbox
        {
            Id = GuidV7.NewGuid(),
            SagaId = sagaId,
            ActionType = actionType,
            Payload = payload,
            RetryCount = 0,
            MaxRetries = maxRetries,
            NextRetryAt = DateTime.Now,
            IsCompleted = false,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };
    }

    /// <summary>Mark retry failed — increment count, set next retry with exponential backoff.</summary>
    public void MarkRetryFailed(string error)
    {
        RetryCount++;
        LastError = error;
        // Exponential backoff: 10s, 20s, 40s, 80s, ... capped at 30 minutes
        var delaySeconds = Math.Min(10 * Math.Pow(2, RetryCount - 1), 1800);
        NextRetryAt = DateTime.Now.AddSeconds(delaySeconds);
        UpdatedAt = DateTime.Now;
    }

    /// <summary>Mark action completed successfully.</summary>
    public void MarkCompleted()
    {
        IsCompleted = true;
        NextRetryAt = null;
        UpdatedAt = DateTime.Now;
    }

    public bool HasExceededMaxRetries => RetryCount >= MaxRetries;
}
