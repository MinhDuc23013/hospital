using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Polly;

namespace HospitalSystem.AiRcaService.Infrastructure.Llm;

/// <summary>
/// Anthropic Claude provider via direct HTTP.
/// Retries 2x on 429/503. Truncates input if estimated tokens exceed MaxInputTokens.
/// Supports both plain text (AnalyzeAsync) and tool_use structured output (AnalyzeStructuredAsync).
/// </summary>
public sealed class AnthropicLlmProvider : ILlmProvider
{
    private readonly HttpClient _http;
    private readonly IConfiguration _config;
    private readonly ILogger<AnthropicLlmProvider> _logger;
    private readonly IAsyncPolicy<HttpResponseMessage> _retryPolicy;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    public AnthropicLlmProvider(
        HttpClient http,
        IConfiguration config,
        ILogger<AnthropicLlmProvider> logger)
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
                    logger.LogWarning("Anthropic retry {Attempt} after {Delay}s: {Status}",
                        attempt, delay.TotalSeconds,
                        outcome.Result?.StatusCode.ToString() ?? outcome.Exception?.Message));
    }

    public async Task<LlmResponse> AnalyzeAsync(
        string systemPrompt,
        string userContent,
        CancellationToken cancellationToken = default)
    {
        var model      = _config["Anthropic:Model"] ?? "claude-sonnet-4-6";
        var maxTokens  = _config.GetValue<int>("Anthropic:MaxTokens", 4096);
        var maxInput   = _config.GetValue<int>("Anthropic:MaxInputTokens", 50000);
        var apiKey     = _config["Anthropic:ApiKey"] ?? string.Empty;

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            _logger.LogWarning("ANTHROPIC_API_KEY not configured — returning demo response");
            return new LlmResponse(
                "**[Demo Mode]** ANTHROPIC_API_KEY chưa được cấu hình.\n\nĐặt biến môi trường `ANTHROPIC_API_KEY` trong `.env` để kích hoạt phân tích thật.",
                0, 0, model);
        }

        // Pre-flight: rough token estimate (4 chars ≈ 1 token)
        var estimatedTokens = (systemPrompt.Length + userContent.Length) / 4;
        if (estimatedTokens > maxInput)
        {
            _logger.LogWarning(
                "Estimated input tokens {Estimated} exceeds max {Max} — truncating",
                estimatedTokens, maxInput);
            userContent = TruncateToTokenBudget(userContent, maxInput - systemPrompt.Length / 4);
        }

        var json = JsonSerializer.Serialize(new
        {
            model,
            max_tokens = maxTokens,
            system     = systemPrompt,
            messages   = new[] { new { role = "user", content = userContent } }
        });

        _logger.LogInformation("Calling Anthropic API. Model={Model} EstimatedTokens={Tokens}",
            model, estimatedTokens);

        var response = await _retryPolicy.ExecuteAsync(
            async ct =>
            {
                // HttpRequestMessage must be created fresh per retry (cannot be reused after Send)
                var req = new HttpRequestMessage(HttpMethod.Post, "https://api.anthropic.com/v1/messages")
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                };
                req.Headers.Add("x-api-key", apiKey);
                req.Headers.Add("anthropic-version", "2023-06-01");
                return await _http.SendAsync(req, ct);
            },
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errBody = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("Anthropic API error {Status}: {Body}", (int)response.StatusCode, errBody);
            response.EnsureSuccessStatusCode();
        }

        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
        return ParseResponse(responseJson, model);
    }

    public async Task<LlmResponse> AnalyzeMultiTurnAsync(
        string systemPrompt,
        IReadOnlyList<LlmMessage> messages,
        CancellationToken cancellationToken = default)
    {
        var model     = _config["Anthropic:Model"] ?? "claude-sonnet-4-6";
        var maxTokens = _config.GetValue<int>("Anthropic:MaxTokens", 4096);
        var apiKey    = _config["Anthropic:ApiKey"] ?? string.Empty;

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            _logger.LogWarning("ANTHROPIC_API_KEY not configured — returning demo response");
            return new LlmResponse(
                "**[Demo Mode]** ANTHROPIC_API_KEY chưa được cấu hình.",
                0, 0, model);
        }

        var msgArray = messages.Select(m => new { role = m.Role, content = m.Content }).ToArray();

        var json = JsonSerializer.Serialize(new
        {
            model,
            max_tokens = maxTokens,
            system     = systemPrompt,
            messages   = msgArray
        });

        _logger.LogInformation(
            "Calling Anthropic API (multi-turn). Model={Model} Messages={Count}",
            model, messages.Count);

        var response = await _retryPolicy.ExecuteAsync(
            async ct =>
            {
                var req = new HttpRequestMessage(HttpMethod.Post, "https://api.anthropic.com/v1/messages")
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                };
                req.Headers.Add("x-api-key", apiKey);
                req.Headers.Add("anthropic-version", "2023-06-01");
                return await _http.SendAsync(req, ct);
            },
            cancellationToken);

        response.EnsureSuccessStatusCode();

        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
        return ParseResponse(responseJson, model);
    }

    public async Task<StructuredAnalysis> AnalyzeStructuredAsync(
        string systemPrompt,
        string userContent,
        CancellationToken cancellationToken = default)
    {
        var model     = _config["Anthropic:Model"] ?? "claude-sonnet-4-6";
        var maxTokens = _config.GetValue<int>("Anthropic:MaxTokens", 4096);
        var maxInput  = _config.GetValue<int>("Anthropic:MaxInputTokens", 50000);
        var apiKey    = _config["Anthropic:ApiKey"] ?? string.Empty;

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            _logger.LogWarning("ANTHROPIC_API_KEY not configured — returning demo response");
            return new StructuredAnalysis(
                "[Demo Mode] ANTHROPIC_API_KEY chưa được cấu hình. Đặt biến môi trường này trong .env để kích hoạt phân tích thật.",
                [],
                "Thêm ANTHROPIC_API_KEY=sk-ant-... vào file .env rồi restart service.",
                "Low", "API key chưa có", []);
        }

        var estimatedTokens = (systemPrompt.Length + userContent.Length) / 4;
        if (estimatedTokens > maxInput)
        {
            _logger.LogWarning(
                "Estimated input tokens {Estimated} exceeds max {Max} — truncating",
                estimatedTokens, maxInput);
            userContent = TruncateToTokenBudget(userContent, maxInput - systemPrompt.Length / 4);
        }

        var body = JsonSerializer.Serialize(new
        {
            model,
            max_tokens  = maxTokens,
            system      = systemPrompt,
            messages    = new[] { new { role = "user", content = userContent } },
            tools       = new[] { ToolDefinitions.SubmitAnalysisTool },
            tool_choice = new { type = "tool", name = "submit_analysis" }
        });

        _logger.LogInformation(
            "Calling Anthropic API (tool_use). Model={Model} EstimatedTokens={Tokens}",
            model, estimatedTokens);

        var response = await _retryPolicy.ExecuteAsync(
            async ct =>
            {
                var req = new HttpRequestMessage(HttpMethod.Post, "https://api.anthropic.com/v1/messages")
                {
                    Content = new StringContent(body, Encoding.UTF8, "application/json")
                };
                req.Headers.Add("x-api-key", apiKey);
                req.Headers.Add("anthropic-version", "2023-06-01");
                return await _http.SendAsync(req, ct);
            },
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errBody = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("Anthropic API error {Status}: {Body}", (int)response.StatusCode, errBody);
            response.EnsureSuccessStatusCode();
        }

        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
        return ParseStructuredResponse(responseJson);
    }

    private static StructuredAnalysis ParseStructuredResponse(string json)
    {
        using var doc  = JsonDocument.Parse(json);
        var content    = doc.RootElement.GetProperty("content");

        // Find the tool_use block
        JsonElement? toolInput = null;
        foreach (var block in content.EnumerateArray())
        {
            if (block.TryGetProperty("type", out var typeEl) &&
                typeEl.GetString() == "tool_use" &&
                block.TryGetProperty("input", out var inputEl))
            {
                toolInput = inputEl;
                break;
            }
        }

        if (toolInput is null)
            throw new InvalidOperationException("Anthropic response contained no tool_use block.");

        var input = toolInput.Value;

        var rootCause  = input.GetProperty("root_cause").GetString() ?? string.Empty;
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
                var logLine   = item.TryGetProperty("log_line", out var ll) ? ll.GetString() ?? "" : "";
                var timestamp = item.TryGetProperty("timestamp", out var ts) ? ts.GetString() ?? "" : "";
                var reasoning = item.TryGetProperty("reasoning", out var rs) ? rs.GetString() ?? "" : "";
                evidence.Add(new EvidenceItem(logLine, timestamp, reasoning));
            }
        }

        var relatedServices = new List<string>();
        if (input.TryGetProperty("related_services", out var svcArr))
        {
            foreach (var svc in svcArr.EnumerateArray())
            {
                var name = svc.GetString();
                if (!string.IsNullOrEmpty(name))
                    relatedServices.Add(name);
            }
        }

        return new StructuredAnalysis(
            rootCause, evidence, suggestedFix,
            confidence, confidenceReasoning, relatedServices);
    }

    private static LlmResponse ParseResponse(string json, string model)
    {
        using var doc = JsonDocument.Parse(json);
        var root      = doc.RootElement;

        var markdown    = root.GetProperty("content")[0].GetProperty("text").GetString() ?? string.Empty;
        var inputTokens  = root.TryGetProperty("usage", out var usage)
            ? usage.GetProperty("input_tokens").GetInt32() : 0;
        var outputTokens = usage.ValueKind != JsonValueKind.Undefined
            ? usage.GetProperty("output_tokens").GetInt32() : 0;

        return new LlmResponse(markdown, inputTokens, outputTokens, model);
    }

    /// <summary>
    /// Truncates log content to fit within token budget.
    /// Strategy: keep ERROR/FATAL lines first, then fill with remaining lines.
    /// </summary>
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
