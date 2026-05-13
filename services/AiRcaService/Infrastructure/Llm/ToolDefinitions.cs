namespace HospitalSystem.AiRcaService.Infrastructure.Llm;

/// <summary>
/// Anthropic tool definitions for structured output via tool_use API.
/// Schema matches StructuredAnalysis POCO fields.
/// </summary>
public static class ToolDefinitions
{
    public static object SubmitAnalysisTool => new
    {
        name        = "submit_analysis",
        description = "Submit root cause analysis findings",
        input_schema = new
        {
            type       = "object",
            properties = new
            {
                root_cause = new
                {
                    type        = "string",
                    description = "Root cause in ≤200 chars"
                },
                evidence = new
                {
                    type  = "array",
                    items = new
                    {
                        type       = "object",
                        properties = new
                        {
                            log_line  = new { type = "string" },
                            timestamp = new { type = "string" },
                            reasoning = new { type = "string" }
                        },
                        required = new[] { "log_line", "reasoning" }
                    }
                },
                suggested_fix = new { type = "string" },
                confidence    = new
                {
                    type    = "string",
                    @enum   = new[] { "High", "Medium", "Low" }
                },
                confidence_reasoning = new { type = "string" },
                related_services     = new
                {
                    type  = "array",
                    items = new { type = "string" }
                }
            },
            required = new[] { "root_cause", "evidence", "suggested_fix", "confidence" }
        }
    };
}
