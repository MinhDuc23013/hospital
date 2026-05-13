namespace HospitalSystem.AiRcaService.Infrastructure.Loki;

/// <summary>Single parsed log entry from a Loki query_range response.</summary>
public sealed record LokiLogEntry(
    DateTime Timestamp,
    string Level,
    string Message,
    Dictionary<string, string> Labels,
    string? CorrelationId
);
