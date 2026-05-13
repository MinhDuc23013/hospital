---
phase: 1
status: completed
priority: high
effort: 5 days
---

# Phase 1 (MVP): Service Skeleton + Loki + Anthropic + Redact + Grafana Link

## Context Links

- Plan: [plan.md](plan.md)
- Phase 0 output: `docs/logging-phi-audit.md` (regex pack + key blocklist)
- Reference service: `services/NotificationService/` (template)
- Gateway routing: `gateway/HospitalGateway/appsettings.json`

## Overview

- **Priority**: High — MVP deliverable
- **Status**: pending
- **Description**: Build new .NET 8 microservice `AiRcaService` end-to-end: Loki query → PII redact → Anthropic Claude → render HTML kết quả → Grafana data link trigger.

## Key Insights

- Reuse 100% existing patterns từ NotificationService (Serilog→Loki, Jaeger tracing, Prometheus metrics, exception middleware, `/health`).
- `ILlmProvider` interface bắt buộc — swap được khi production cần đổi provider.
- PII redactor là gate compliance — unit test CI-gated, fail build nếu rớt.
- KHÔNG dùng MediatR/CQRS cho MVP — over-engineer cho 1 endpoint. Plain controller + service classes đủ.

## Requirements

**Functional**:
- Endpoint `GET /api/ai-rca/analyze?service={x}&from={iso}&to={iso}&traceId={optional}` → render HTML
- Query Loki bằng LogQL với time range, max 5000 lines, max 1h window
- Redact PHI trước khi gửi Anthropic (key blocklist + value regex)
- Gọi Anthropic Claude 3.5/4 Sonnet với context logs + system prompt SRE
- Render markdown response → HTML page (server-side, không SPA)
- Rate limit: 10 req/h per IP (sliding window, in-memory cho MVP)
- Health endpoint `/health`

**Non-functional**:
- Response time <15s p95
- Service standalone — không cần DB, không cần message broker
- Follow code-standards.md (DI ctor injection, async suffix, namespace PascalCase)

## Architecture

```
HTTP GET /api/ai-rca/analyze
  ↓
[AnalyzeController]
  ├─ Validate params (TimeRangeValidator: max 1h, from<to)
  ├─ Check IRateLimiter (10/h per IP)
  ├─ [LokiClient].FetchLogsAsync(service, from, to, maxLines=5000)
  ├─ [PiiRedactor].Redact(logs) → returns (redactedLogs, redactCount)
  ├─ [ILlmProvider (AnthropicLlmProvider)].AnalyzeAsync(redactedLogs, systemPrompt)
  ├─ [HtmlRenderer].Render(markdown, metadata)
  └─ Return ContentResult(html, "text/html")
```

## Project Structure

```
services/AiRcaService/
├── AiRcaService.csproj
├── Program.cs
├── Dockerfile
├── appsettings.json
├── appsettings.Development.json
├── Controllers/
│   ├── AnalyzeController.cs
│   └── HealthController.cs
├── Application/
│   ├── AnalyzeRequestDto.cs
│   ├── AnalyzeResultDto.cs
│   └── Services/
│       ├── ILogAnalysisService.cs
│       └── LogAnalysisService.cs        ← orchestrate flow
├── Infrastructure/
│   ├── Loki/
│   │   ├── ILokiClient.cs
│   │   ├── LokiClient.cs                ← HTTP client cho /loki/api/v1/query_range
│   │   └── LokiQueryBuilder.cs
│   ├── Llm/
│   │   ├── ILlmProvider.cs              ← abstraction
│   │   ├── AnthropicLlmProvider.cs      ← Anthropic.SDK NuGet hoặc HTTP direct
│   │   └── PromptTemplates.cs
│   ├── Redaction/
│   │   ├── IPiiRedactor.cs
│   │   ├── PiiRedactor.cs               ← regex + key blocklist
│   │   ├── RedactionRules.cs            ← config (regex + keys)
│   │   └── RedactionResult.cs
│   ├── RateLimit/
│   │   ├── IRateLimiter.cs
│   │   └── InMemoryRateLimiter.cs       ← sliding window 10/h per IP
│   └── Rendering/
│       ├── IHtmlRenderer.cs
│       └── MarkdownHtmlRenderer.cs      ← Markdig NuGet
└── Middleware/
    └── ExceptionMiddleware.cs

tests/AiRcaService.Tests/
├── AiRcaService.Tests.csproj
├── Redaction/
│   └── PiiRedactorTests.cs              ← CI-gated, 20+ test cases
├── Loki/
│   └── LokiQueryBuilderTests.cs
└── Application/
    └── LogAnalysisServiceTests.cs       ← mock Loki + LLM
```

## Related Code Files

**Reuse từ shared**:
- `shared/HospitalShared/Tracing/JaegerTracingExtensions.cs` — `AddJaegerTracing()`
- `shared/HospitalShared/Metrics/*` — Prometheus
- Serilog config copy từ NotificationService/Program.cs

**Modify**:
- `gateway/HospitalGateway/appsettings.json` — add `ai-rca-route` + `ai-rca-cluster`
- `docker-compose.yml` — add `ai-rca-service` block (port 5013, depends_on: loki)

## Implementation Steps

### Step 1: Project skeleton (Day 1, AM)

1. Copy `services/NotificationService/` → `services/AiRcaService/` rename project + namespace `HospitalSystem.AiRcaService`.
2. Strip ra: RabbitMQ consumer, MediatR, DB context, Keycloak (MVP không cần auth).
3. Keep: Serilog (Console + Loki sink), Jaeger tracing, Prometheus metrics, exception middleware, health endpoint, Swagger.
4. Update `appsettings.json`:
   ```json
   {
     "Loki": { "BaseUrl": "http://loki:3100" },
     "Anthropic": { "ApiKey": "${ANTHROPIC_API_KEY}", "Model": "claude-3-5-sonnet-20241022", "MaxTokens": 4096 },
     "RateLimit": { "RequestsPerHour": 10 },
     "Analysis": { "MaxLogLines": 5000, "MaxWindowMinutes": 60, "MaxInputTokens": 50000 }
   }
   ```
5. Add NuGet: `Markdig` (markdown→html), `Polly` (retry), `System.Threading.RateLimiting` (built-in .NET 8).
6. Add `Dockerfile` (copy NotificationService template, update port `5013`).
7. Add docker-compose service entry.
8. **Verify**: `dotnet build` pass.

### Step 2: LokiClient (Day 1, PM)

1. `ILokiClient.FetchLogsAsync(string service, DateTime from, DateTime to, int maxLines)` → `Task<IReadOnlyList<LokiLogEntry>>`.
2. Implement HTTP call:
   - Endpoint: `GET {baseUrl}/loki/api/v1/query_range`
   - Query params: `query={service="X"}`, `start={ns}`, `end={ns}`, `limit={maxLines}`, `direction=backward`
   - Parse JSON response → flatten streams[*].values → entries
3. `LokiLogEntry { DateTime Timestamp, string Level, string Message, Dictionary<string,string> Labels, string? CorrelationId }`.
4. Polly retry 3x với exponential backoff (network resilience).
5. **Test**: Unit test query builder + integration test với mock HTTP handler.

### Step 3: PII Redactor (Day 2, AM-PM) ⚠️ CRITICAL

1. Implement `RedactionRules` từ Phase 0 output:
   ```csharp
   public static class RedactionRules
   {
       public static readonly string[] BlockedKeys = ["patientName", "fullName", "phone", "phoneNumber", "email", "bhyt", "cccd", "address", "diagnosis", "dateOfBirth"];
       public static readonly Dictionary<string, Regex> ValuePatterns = new()
       {
           ["CCCD_12"] = new(@"\b\d{12}\b"),
           ["CCCD_9"] = new(@"\b\d{9}\b"),
           ["PHONE_VN"] = new(@"\b(0|\+84)\d{9,10}\b"),
           ["BHYT"] = new(@"\b[A-Z]{2}\d{13}\b"),
           ["EMAIL"] = new(@"\b[\w.-]+@[\w.-]+\.\w+\b"),
       };
   }
   ```
2. `PiiRedactor.Redact(LokiLogEntry entry)`:
   - For each blocked key in entry.Labels → replace value với `[REDACTED:KEY]`
   - For message text → apply mỗi regex pattern → replace với `[REDACTED:TYPE]`
   - Return `RedactionResult { RedactedEntry, RedactCount, RedactedTypes }`
3. **Audit logging**: Service log redact count (chỉ count, KHÔNG log redacted value).
4. **Unit tests CI-gated** (`PiiRedactorTests.cs`):
   - Test mỗi pattern: CCCD 9, CCCD 12, phone với prefix 0, prefix +84, BHYT, email
   - Test blocked key replacement
   - Test combined (1 log entry chứa 3 PHI types)
   - Test no-false-positive: numbers that look like CCCD but aren't (vd order ID 9 digits — này có false positive, document trade-off: prefer over-redact)
   - Test edge cases: empty, null, very long string
   - Minimum 20 test cases, 100% pass.

### Step 4: LLM Provider abstraction + Anthropic impl (Day 3)

1. `ILlmProvider.AnalyzeAsync(string systemPrompt, string userContent, CancellationToken ct)` → `Task<LlmResponse>`.
2. `LlmResponse { string Markdown, int InputTokens, int OutputTokens, string Model }`.
3. `AnthropicLlmProvider` — dùng HTTP direct (đơn giản, control tốt) hoặc NuGet `Anthropic.SDK`:
   - Endpoint: `POST https://api.anthropic.com/v1/messages`
   - Headers: `x-api-key`, `anthropic-version: 2023-06-01`
   - Body: `{ model, max_tokens, system, messages: [{role:"user", content}] }`
   - Parse response.content[0].text + usage tokens
4. Polly retry 2x on 429/503.
5. Token counter pre-flight: nếu input > 50k tokens, truncate logs (giữ ERROR/FATAL, drop INFO trước).
6. **Test**: Unit test với mock HttpMessageHandler.

### Step 5: PromptTemplates (Day 3 cont.)

```csharp
public static class PromptTemplates
{
    public const string SystemPrompt = """
        You are a senior SRE analyzing logs from a hospital microservice system.
        Analyze provided logs and identify root cause of errors.

        OUTPUT FORMAT (strict markdown):
        ## Root Cause
        <≤200 chars, specific>

        ## Evidence
        - Quote exact log lines with timestamps that support your conclusion
        - At least 2 evidence items

        ## Suggested Fix
        <concrete, actionable steps. Reference code/config if obvious>

        ## Confidence
        High | Medium | Low — explain why

        ## Related Services (if applicable)
        <list services likely affected upstream/downstream>

        RULES:
        - DO NOT fabricate log content. Only cite logs you see.
        - If logs insufficient, say so and request more data.
        - Prefer specific over generic ("DB connection pool exhausted" not "DB error").
        """;

    public static string BuildUserPrompt(string service, DateTime from, DateTime to, IReadOnlyList<LokiLogEntry> logs)
        => $"""
            Service: {service}
            Time range: {from:o} to {to:o}
            Log lines: {logs.Count}

            LOGS:
            {string.Join("\n", logs.Select(FormatLogLine))}
            """;
}
```

### Step 6: LogAnalysisService orchestration (Day 4 AM)

1. Compose: validate → rate-limit → Loki → redact → LLM → render.
2. Error handling: each step throws specific exception → middleware → JSON error response.
3. Log start/end với traceId enrichment.
4. **Test**: `LogAnalysisServiceTests` mock dependencies, test happy path + each failure mode.

### Step 7: AnalyzeController + HTML render (Day 4 PM)

1. Controller endpoint:
   ```csharp
   [HttpGet("analyze")]
   public async Task<IActionResult> Analyze([FromQuery] AnalyzeRequestDto req, CancellationToken ct)
   ```
2. Render markdown response → HTML page với template đơn giản:
   - Header: service, time range, redact count
   - Body: rendered markdown (Markdig)
   - Footer: disclaimer "AI suggestion — dev verify before action"
3. Return `ContentResult { Content = html, ContentType = "text/html" }`.

### Step 8: Gateway routing + Grafana data link (Day 5 AM)

1. Update `gateway/HospitalGateway/appsettings.json`:
   ```json
   "ReverseProxy": {
     "Routes": {
       "ai-rca-route": {
         "ClusterId": "ai-rca-cluster",
         "Match": { "Path": "/api/ai-rca/{**catch-all}" }
       }
     },
     "Clusters": {
       "ai-rca-cluster": {
         "Destinations": { "destination1": { "Address": "http://ai-rca-service:5013/" } }
       }
     }
   }
   ```
2. Grafana setup (manual):
   - Open log panel → Panel options → Data links → New link
   - Title: "Analyze with AI"
   - URL: `http://localhost:8080/api/ai-rca/analyze?service=${__field.labels.service}&from=${__from:date:iso}&to=${__to:date:iso}`
   - Open in new tab: ✅
3. Document trong `docs/ai-rca-setup.md`.

### Step 9: End-to-end smoke test (Day 5 PM)

1. Spin up local docker-compose stack
2. Generate synthetic error logs (e.g., kill 1 service, restart, log NullRefException)
3. Open Grafana → click "Analyze with AI" → verify HTML rendered với root cause
4. Verify redact count log
5. Verify rate limit triggers sau 10 requests
6. Verify health endpoint

## Todo List

- [x] Step 1: Project skeleton, build pass
- [x] Step 2: LokiClient + tests
- [x] Step 3: PiiRedactor + 23 unit tests (CI-gated) — all pass
- [x] Step 4: ILlmProvider + AnthropicLlmProvider + tests
- [x] Step 5: PromptTemplates
- [x] Step 6: LogAnalysisService + tests
- [x] Step 7: AnalyzeController + HTML rendering
- [x] Step 8: Gateway route added to appsettings.json
- [ ] Step 9: E2E smoke test (manual — requires running stack)
- [ ] Setup `ANTHROPIC_API_KEY` env var (local + docker-compose .env)
- [ ] Add CI job: run `dotnet test` for AiRcaService.Tests, fail on PiiRedactor test failure
- [ ] Document setup trong `docs/ai-rca-setup.md`

## Success Criteria

- `dotnet build` + `dotnet test` pass, all 20+ PII tests green
- Local docker-compose up → click Grafana button → render result trong <15s
- Rate limit hoạt động (request 11 → 429)
- Redact count logged for every request
- Anthropic call thành công với synthetic logs
- Gateway route `/api/ai-rca/*` proxy đúng

## Risk Assessment

| Risk | Mitigation |
|------|-----------|
| Anthropic API quota / cost spike | Token cap 50k input, rate limit 10/h, cost alert manual check daily Anthropic console |
| PII regex false negative | Phase 0 audit + peer review tests + prefer over-redact |
| Loki query timeout với window lớn | Cap 1h window + 5000 lines, Polly timeout 30s |
| LLM hallucination | Strict prompt + cite-evidence rule + UI disclaimer |
| Token truncation cắt nhầm context | Truncation strategy: keep ERROR/FATAL first, drop INFO/DEBUG |

## Security Considerations

- **NO production PHI logs** trong MVP test environment. Verbal commit + .env warning.
- `ANTHROPIC_API_KEY` chỉ trong .env (gitignored), không commit.
- Service expose internal network only (docker network), không bind external port.
- Redact count audit log → có thể monitor weekly.

## Next Steps

- Phase 2: thêm Jaeger trace context + Redis cache + structured tool-use output.
- Optional: nếu Anthropic cost spike → switch sang Claude Haiku (cheaper, vẫn ok cho RCA đơn giản).
