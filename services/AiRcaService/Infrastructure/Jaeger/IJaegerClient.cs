namespace HospitalSystem.AiRcaService.Infrastructure.Jaeger;

/// <summary>Fetches trace data from Jaeger HTTP API.</summary>
public interface IJaegerClient
{
    /// <summary>
    /// Fetches a trace by ID and returns a human-readable summary for LLM context.
    /// Returns null if traceId not found or Jaeger is unavailable.
    /// </summary>
    Task<string?> FetchTraceSummaryAsync(string traceId, CancellationToken ct = default);
}
