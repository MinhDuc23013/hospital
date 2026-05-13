using HospitalSystem.AiRcaService.Infrastructure.Loki;

namespace HospitalSystem.AiRcaService.Infrastructure.Redaction;

/// <summary>Result of a PII redaction pass over a batch of Loki log entries.</summary>
public sealed record RedactionResult(
    IReadOnlyList<LokiLogEntry> RedactedEntries,
    int TotalRedactions,
    IReadOnlyDictionary<string, int> RedactionsByType
);
