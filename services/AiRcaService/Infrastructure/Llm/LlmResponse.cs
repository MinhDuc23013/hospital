namespace HospitalSystem.AiRcaService.Infrastructure.Llm;

/// <summary>Response returned by an LLM provider after analysis.</summary>
public sealed record LlmResponse(
    string Markdown,
    int InputTokens,
    int OutputTokens,
    string Model
);
