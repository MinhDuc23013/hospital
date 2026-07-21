using System.Net;
using System.Text;
using System.Text.Json;
using Polly;

namespace HospitalSystem.AiRcaService.Infrastructure.Llm;

/// <summary>
/// OpenAI-compatible internal LLM provider.
/// Uses JSON mode (response_format: json_object) for structured output.
/// Retries 2x on 429/503.
/// </summary>
public sealed class InternalLlmProvider : ILlmProvider
{
    private readonly HttpClient _http;
    private readonly IConfiguration _config;
    private readonly ILogger<InternalLlmProvider> _logger;
    private readonly IAsyncPolicy<HttpResponseMessage> _retryPolicy;

    // JSON schema injected into system prompt to force structured output
    private const string StructuredJsonSchema = """
        Respond ONLY with a valid JSON object matching this schema (no markdown, no extra text):
        {
          "root_cause": "<string, root cause in ≤200 chars>",
          "evidence": [
            { "log_line": "<string>", "timestamp": "<string>", "reasoning": "<string>" }
          ],
          "suggested_fix": "<string>",
          "confidence": "<High|Medium|Low>",
          "confidence_reasoning": "<string>",
          "related_services": ["<string>"]
        }
        """;

    public InternalLlmProvider(
        HttpClient http,
        IConfiguration config,
        ILogger<InternalLlmProvider> logger)
    {
        _http   = http;
        _config = config;
        _logger = logger;

        _retryPolicy = Policy
            .Handle<HttpRequestException>()
            .OrResult<HttpResponseMessage>(r =>
                r.StatusCode is HttpStatusCode.TooManyRequests or HttpStatusCode.ServiceUnavailable)
            .WaitAndRetryAsync(2,
                attempt => TimeSpan.FromSeconds(attempt * 5),
                (outcome, delay, attempt, _) =>
                    logger.LogWarning("Internal LLM retry {Attempt} after {Delay}s: {Status}",
                        attempt, delay.TotalSeconds,
                        outcome.Result?.StatusCode.ToString() ?? outcome.Exception?.Message));
    }

    public async Task<LlmResponse> AnalyzeAsync(
        string systemPrompt,
        string userContent,
        CancellationToken cancellationToken = default)
    {
        var (baseUrl, model, maxTokens, maxInput, apiKey) = ReadConfig();

        var estimatedTokens = (systemPrompt.Length + userContent.Length) / 4;
        if (estimatedTokens > maxInput)
        {
            _logger.LogWarning(
                "Estimated input tokens {Estimated} exceeds max {Max} — truncating",
                estimatedTokens, maxInput);
            userContent = TruncateToTokenBudget(userContent, maxInput - systemPrompt.Length / 4);
        }

        var body = BuildChatBody(model, maxTokens, systemPrompt,
            new object[] { new { role = "user", content = userContent } });

        _logger.LogInformation("Calling internal LLM. Model={Model} EstimatedTokens={Tokens}",
            model, estimatedTokens);

        var response = await SendAsync(baseUrl, apiKey, body, cancellationToken);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        return ParseChatResponse(json, model);
    }

    public async Task<LlmResponse> AnalyzeMultiTurnAsync(
        string systemPrompt,
        IReadOnlyList<LlmMessage> messages,
        CancellationToken cancellationToken = default)
    {
        var (baseUrl, model, maxTokens, _, apiKey) = ReadConfig();

        var msgArray = messages.Select(m => (object)new { role = m.Role, content = m.Content }).ToArray();
        var body = BuildChatBody(model, maxTokens, systemPrompt, msgArray);

        _logger.LogInformation("Calling internal LLM (multi-turn). Model={Model} Messages={Count}",
            model, messages.Count);

        var response = await SendAsync(baseUrl, apiKey, body, cancellationToken);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        return ParseChatResponse(json, model);
    }

    public async Task<StructuredAnalysis> AnalyzeStructuredAsync(
        string systemPrompt,
        string userContent,
        CancellationToken cancellationToken = default)
    {
        var (baseUrl, model, maxTokens, maxInput, apiKey) = ReadConfig();

        var estimatedTokens = (systemPrompt.Length + userContent.Length) / 4;
        if (estimatedTokens > maxInput)
        {
            _logger.LogWarning(
                "Estimated input tokens {Estimated} exceeds max {Max} — truncating",
                estimatedTokens, maxInput);
            userContent = TruncateToTokenBudget(userContent, maxInput - systemPrompt.Length / 4);
        }

        // Inject JSON schema into system prompt so model knows the exact output format
        var structuredSystemPrompt = systemPrompt + "\n\n" + StructuredJsonSchema;

        var body = JsonSerializer.Serialize(new
        {
            model,
            max_tokens = maxTokens,
            response_format = new { type = "json_object" },
            messages = new[]
            {
                new { role = "system", content = structuredSystemPrompt },
                new { role = "user", content = userContent }
            }
        });

        _logger.LogInformation(
            "Calling internal LLM (structured/JSON mode). Model={Model} EstimatedTokens={Tokens}",
            model, estimatedTokens);

        var response = await SendAsync(baseUrl, apiKey, body, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errBody = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("Internal LLM error {Status}: {Body}", (int)response.StatusCode, errBody);
            response.EnsureSuccessStatusCode();
        }

        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
        return ParseStructuredResponse(responseJson);
    }

    // ── Private helpers ───────────────────────────────────────────────────────────

    private (string BaseUrl, string Model, int MaxTokens, int MaxInput, string ApiKey) ReadConfig() =>
    (
        _config["InternalLlm:BaseUrl"]     ?? "http://localhost:11434",
        _config["InternalLlm:Model"]       ?? "llama3",
        _config.GetValue<int>("InternalLlm:MaxTokens", 4096),
        _config.GetValue<int>("InternalLlm:MaxInputTokens", 50000),
        _config["InternalLlm:ApiKey"]      ?? string.Empty
    );

    private static string BuildChatBody(string model, int maxTokens, string systemPrompt, IEnumerable<object> userMessages)
    {
        var systemMsg = new { role = "system", content = systemPrompt };
        var allMsgs   = new object[] { systemMsg }.Concat(userMessages).ToArray();
        return JsonSerializer.Serialize(new { model, max_tokens = maxTokens, messages = allMsgs });
    }

    private async Task<HttpResponseMessage> SendAsync(
        string baseUrl, string apiKey, string body, CancellationToken ct)
    {
        var endpoint = baseUrl.TrimEnd('/') + "/v1/chat/completions";

        return await _retryPolicy.ExecuteAsync(async token =>
        {
            var req = new HttpRequestMessage(HttpMethod.Post, endpoint)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json")
            };
            if (!string.IsNullOrEmpty(apiKey))
                req.Headers.Add("Authorization", $"Bearer {apiKey}");
            return await _http.SendAsync(req, token);
        }, ct);
    }

    private static LlmResponse ParseChatResponse(string json, string model)
    {
        using var doc   = JsonDocument.Parse(json);
        var root        = doc.RootElement;
        var text        = root.GetProperty("choices")[0]
                             .GetProperty("message")
                             .GetProperty("content")
                             .GetString() ?? string.Empty;

        int inputTokens  = 0, outputTokens = 0;
        if (root.TryGetProperty("usage", out var usage))
        {
            usage.TryGetProperty("prompt_tokens",     out var pt);
            usage.TryGetProperty("completion_tokens", out var ct2);
            inputTokens  = pt.ValueKind  == JsonValueKind.Number ? pt.GetInt32()  : 0;
            outputTokens = ct2.ValueKind == JsonValueKind.Number ? ct2.GetInt32() : 0;
        }

        return new LlmResponse(text, inputTokens, outputTokens, model);
    }

    private static StructuredAnalysis ParseStructuredResponse(string responseJson)
    {
        using var outer = JsonDocument.Parse(responseJson);
        var content     = outer.RootElement
                               .GetProperty("choices")[0]
                               .GetProperty("message")
                               .GetProperty("content")
                               .GetString() ?? "{}";

        using var doc = JsonDocument.Parse(content);
        var input     = doc.RootElement;

        var rootCause = input.TryGetProperty("root_cause", out var rc)
            ? rc.GetString() ?? string.Empty : string.Empty;
        var suggestedFix = input.TryGetProperty("suggested_fix", out var sf)
            ? sf.GetString() ?? string.Empty : string.Empty;
        var confidence = input.TryGetProperty("confidence", out var conf)
            ? conf.GetString() ?? "Low" : "Low";
        var confidenceReasoning = input.TryGetProperty("confidence_reasoning", out var cr)
            ? cr.GetString() ?? string.Empty : string.Empty;

        var evidence = new List<EvidenceItem>();
        if (input.TryGetProperty("evidence", out var evidenceArr))
        {
            foreach (var item in evidenceArr.EnumerateArray())
            {
                var logLine   = item.TryGetProperty("log_line",  out var ll) ? ll.GetString()  ?? "" : "";
                var timestamp = item.TryGetProperty("timestamp", out var ts) ? ts.GetString()  ?? "" : "";
                var reasoning = item.TryGetProperty("reasoning", out var rs) ? rs.GetString()  ?? "" : "";
                evidence.Add(new EvidenceItem(logLine, timestamp, reasoning));
            }
        }

        var relatedServices = new List<string>();
        if (input.TryGetProperty("related_services", out var svcArr))
        {
            foreach (var svc in svcArr.EnumerateArray())
            {
                var name = svc.GetString();
                if (!string.IsNullOrEmpty(name)) relatedServices.Add(name);
            }
        }

        return new StructuredAnalysis(
            rootCause, evidence, suggestedFix,
            confidence, confidenceReasoning, relatedServices);
    }

    private static string TruncateToTokenBudget(string userContent, int maxChars)
    {
        var lines      = userContent.Split('\n');
        var errorLines = lines.Where(l =>
            l.Contains("[ERROR]", StringComparison.OrdinalIgnoreCase) ||
            l.Contains("[FATAL]", StringComparison.OrdinalIgnoreCase) ||
            l.Contains("[FTL]",   StringComparison.OrdinalIgnoreCase) ||
            l.Contains("[ERR]",   StringComparison.OrdinalIgnoreCase)).ToList();

        var otherLines = lines.Except(errorLines).ToList();
        var result     = new List<string>();
        int chars      = 0;

        foreach (var line in errorLines.Concat(otherLines))
        {
            if (chars + line.Length > maxChars) break;
            result.Add(line);
            chars += line.Length;
        }

        return string.Join("\n", result) + "\n[...truncated due to token limit...]";
    }
}
