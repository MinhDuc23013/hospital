namespace HospitalSystem.AiRcaService.Infrastructure.Llm;

/// <summary>Typed result from Anthropic tool_use structured output.</summary>
public sealed record StructuredAnalysis(
    string RootCause,
    IReadOnlyList<EvidenceItem> Evidence,
    string SuggestedFix,
    string Confidence,              // "High" | "Medium" | "Low"
    string ConfidenceReasoning,
    IReadOnlyList<string> RelatedServices
);

/// <summary>A single evidence item linking a log line to the root cause reasoning.</summary>
public sealed record EvidenceItem(
    string LogLine,
    string Timestamp,
    string Reasoning
);
