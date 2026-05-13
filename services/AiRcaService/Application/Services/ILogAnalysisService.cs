namespace HospitalSystem.AiRcaService.Application.Services;

/// <summary>Result of a full analysis run — HTML page plus the session ID for follow-up Q&A.</summary>
public sealed record AnalysisResult(string Html, string SessionId);

/// <summary>Orchestrates the full log analysis pipeline: Loki → Redact → LLM → HTML.</summary>
public interface ILogAnalysisService
{
    /// <summary>
    /// Runs the full analysis pipeline and returns rendered HTML plus a session ID
    /// that callers can use for follow-up questions via POST /api/ai-rca/followup.
    /// Throws ArgumentException for invalid parameters.
    /// </summary>
    Task<AnalysisResult> AnalyzeAsync(
        string service,
        DateTime from,
        DateTime to,
        string? traceId,
        CancellationToken cancellationToken = default);
}
