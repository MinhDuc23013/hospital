using HospitalSystem.AiRcaService.Infrastructure.Loki;

namespace HospitalSystem.AiRcaService.Infrastructure.Redaction;

/// <summary>Redacts PHI from Loki log entries before sending to external LLM.</summary>
public interface IPiiRedactor
{
    /// <summary>
    /// Redacts all PHI from a batch of log entries.
    /// Logs redaction count (never logs redacted values).
    /// </summary>
    RedactionResult Redact(IReadOnlyList<LokiLogEntry> entries);
}
