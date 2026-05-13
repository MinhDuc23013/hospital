using HospitalSystem.AiRcaService.Infrastructure.Loki;

namespace HospitalSystem.AiRcaService.Infrastructure.Llm;

/// <summary>Static prompt templates for SRE log analysis.</summary>
public static class PromptTemplates
{
    /// <summary>
    /// System prompt for tool_use structured output.
    /// Instructs the model to call submit_analysis instead of returning markdown.
    /// </summary>
    public const string SystemPrompt = """
        You are a senior SRE analyzing logs from a hospital microservice system.
        Analyze the provided logs (and optional Jaeger trace context) to identify the root cause of errors.

        RULES:
        - DO NOT fabricate log content. Only cite log lines you actually see.
        - If logs are insufficient, state that in root_cause and set confidence to "Low".
        - Prefer specific over generic root causes.
        - For cross-service issues, list all affected services in related_services.

        Call the submit_analysis tool with your findings.
        """;

    /// <summary>
    /// Builds the user prompt with log context.
    /// Appends Jaeger trace summary when available.
    /// </summary>
    public static string BuildUserPrompt(
        string service,
        DateTime from,
        DateTime to,
        IReadOnlyList<LokiLogEntry> logs,
        string? traceSummary = null)
    {
        var logSection = $"""
            Service: {service}
            Time range: {from:o} to {to:o}
            Log lines: {logs.Count}

            LOGS:
            {string.Join("\n", logs.Select(FormatLogLine))}
            """;

        if (string.IsNullOrWhiteSpace(traceSummary))
            return logSection;

        return $"""
            {logSection}

            JAEGER TRACE:
            {traceSummary}
            """;
    }

    private static string FormatLogLine(LokiLogEntry entry)
        => $"[{entry.Timestamp:yyyy-MM-dd HH:mm:ss.fff}] [{entry.Level.ToUpperInvariant()}] {entry.Message}";
}
