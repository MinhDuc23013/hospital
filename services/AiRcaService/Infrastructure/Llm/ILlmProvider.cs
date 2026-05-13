namespace HospitalSystem.AiRcaService.Infrastructure.Llm;

/// <summary>A single turn in a multi-turn conversation sent to the LLM.</summary>
public sealed record LlmMessage(string Role, string Content);

/// <summary>
/// Abstraction for LLM providers — swap Anthropic for another provider without changing callers.
/// </summary>
public interface ILlmProvider
{
    /// <summary>
    /// Sends a system + user prompt to the LLM and returns a raw markdown response.
    /// </summary>
    Task<LlmResponse> AnalyzeAsync(
        string systemPrompt,
        string userContent,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a multi-turn conversation history to the LLM (plain text, no tool_use).
    /// Used for follow-up Q&amp;A where natural prose responses are preferred.
    /// </summary>
    Task<LlmResponse> AnalyzeMultiTurnAsync(
        string systemPrompt,
        IReadOnlyList<LlmMessage> messages,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends prompts using Anthropic tool_use to force structured JSON output.
    /// Returns a typed StructuredAnalysis parsed from the tool_use response.
    /// </summary>
    Task<StructuredAnalysis> AnalyzeStructuredAsync(
        string systemPrompt,
        string userContent,
        CancellationToken cancellationToken = default);
}
