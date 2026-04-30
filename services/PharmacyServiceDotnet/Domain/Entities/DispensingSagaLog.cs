using HospitalShared;
namespace PharmacyServiceDotnet.Domain.Entities;

/// <summary>Audit log entry for each step transition in the dispensing saga.</summary>
public class DispensingSagaLog
{
    public Guid Id { get; private set; }
    public Guid SagaId { get; private set; }
    public string FromStep { get; private set; } = string.Empty;
    public string ToStep { get; private set; } = string.Empty;
    public string? Message { get; private set; }
    public string? Details { get; private set; }
    public DateTime Timestamp { get; private set; }

    private DispensingSagaLog() { }

    public static DispensingSagaLog Create(
        Guid sagaId, string fromStep, string toStep,
        string? message = null, string? details = null)
    {
        return new DispensingSagaLog
        {
            Id = GuidV7.NewGuid(),
            SagaId = sagaId,
            FromStep = fromStep,
            ToStep = toStep,
            Message = message,
            Details = details,
            Timestamp = DateTime.Now
        };
    }
}
