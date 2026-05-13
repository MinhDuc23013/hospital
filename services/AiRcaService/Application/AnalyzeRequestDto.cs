namespace HospitalSystem.AiRcaService.Application;

/// <summary>Query parameters for the analyze endpoint.</summary>
public sealed class AnalyzeRequestDto
{
    /// <summary>Service label as stored in Loki (e.g. "patient-service").</summary>
    public string Service { get; init; } = string.Empty;

    /// <summary>Start of the analysis window (ISO 8601).</summary>
    public DateTime From { get; init; }

    /// <summary>End of the analysis window (ISO 8601).</summary>
    public DateTime To { get; init; }

    /// <summary>Optional trace/correlation ID to filter logs.</summary>
    public string? TraceId { get; init; }
}
