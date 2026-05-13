---
phase: 2
status: pending
priority: medium
effort: 2-3 days
---

# Phase 2 (V1.1): Jaeger Trace Context + Redis Cache + Structured Output

## Context Links

- Plan: [plan.md](plan.md)
- Depends on: [phase-01-mvp-skeleton.md](phase-01-mvp-skeleton.md) (must be merged & smoke-tested)
- Brainstorm: [`brainstorm-260512-1507-ai-log-rca-loki.md`](../reports/brainstorm-260512-1507-ai-log-rca-loki.md) §6 Context assembly + §8 Rate limit

## Overview

- **Priority**: Medium — quality boost, không phải critical path
- **Status**: pending
- **Description**: Enrich LLM context với Jaeger trace data + cache kết quả qua Redis + dùng Anthropic tool_use cho structured output (thay vì parse markdown).

## Key Insights

- Cross-service errors khó analyze nếu chỉ có logs 1 service. Jaeger trace cho thấy đầy đủ call chain.
- Cùng 1 incident sẽ có nhiều dev xem → cache giảm cost + latency.
- Tool use (structured output) cho phép UI render từng phần riêng (root cause, evidence, fix) → tốt hơn parse markdown bằng regex.

## Requirements

**Functional**:
- Khi `traceId` có trong query param → fetch Jaeger trace, extract upstream/downstream services + their error spans.
- Cache key = hash(service + from + to + traceId). TTL 1h. Hit/miss metric.
- LLM response qua tool_use schema (Anthropic tool definition) → typed object thay vì raw markdown.

**Non-functional**:
- Cache hit response time <500ms.
- Redis connection reuse từ `HospitalShared.Caching`.

## Architecture

```
HTTP GET /api/ai-rca/analyze?traceId=X
  ↓
[AnalyzeController]
  ├─ Validate + rate-limit
  ├─ [IAnalysisCache].GetAsync(cacheKey) → hit? return cached HTML
  ├─ MISS:
  │   ├─ LokiClient.FetchLogs (parallel)
  │   ├─ JaegerClient.FetchTrace (parallel, if traceId)  ← NEW
  │   ├─ PiiRedactor.Redact
  │   ├─ AnthropicLlmProvider.AnalyzeWithToolUse  ← changed
  │   ├─ HtmlRenderer.Render(StructuredAnalysis)  ← changed
  │   └─ Cache.SetAsync(cacheKey, html, ttl=1h)
  └─ Return ContentResult
```

## Project Structure (additions)

```
services/AiRcaService/
├── Infrastructure/
│   ├── Jaeger/
│   │   ├── IJaegerClient.cs
│   │   ├── JaegerClient.cs              ← HTTP client cho /api/traces/{id}
│   │   └── JaegerTraceDto.cs
│   ├── Cache/
│   │   ├── IAnalysisCache.cs
│   │   └── RedisAnalysisCache.cs
│   └── Llm/
│       ├── ToolDefinitions.cs           ← Anthropic tool schema
│       └── StructuredAnalysis.cs        ← typed result

tests/AiRcaService.Tests/
├── Jaeger/
│   └── JaegerClientTests.cs
└── Cache/
    └── RedisCacheKeyTests.cs
```

## Related Code Files

**Modify**:
- `services/AiRcaService/Application/Services/LogAnalysisService.cs` — add cache + Jaeger
- `services/AiRcaService/Infrastructure/Llm/AnthropicLlmProvider.cs` — switch sang tool_use
- `services/AiRcaService/Application/AnalyzeResultDto.cs` — typed structure
- `services/AiRcaService/appsettings.json` — Redis + Jaeger config

**Reuse**:
- `shared/HospitalShared/Caching/*` — Redis extensions

## Implementation Steps

### Step 1: JaegerClient (Day 1)

1. `IJaegerClient.FetchTraceAsync(string traceId, CancellationToken ct)` → `Task<JaegerTrace?>`.
2. HTTP call: `GET http://jaeger:16686/api/traces/{traceId}`.
3. Parse response:
   - Extract list spans, mỗi span có: serviceName, operationName, startTime, duration, tags (status code, error flag).
   - Filter error spans (`error=true` tag hoặc `http.status_code >= 500`).
4. Build summary string cho LLM prompt:
   ```
   TRACE SUMMARY:
   - Total spans: 12
   - Services involved: gateway → patient-service → medical-record-service → postgres
   - Error spans: 2
     • medical-record-service.GetRecord (500, duration 234ms, error="Connection timeout")
     • postgres.query (timeout 30s)
   ```
5. Append vào user prompt sau logs section.
6. **Test**: mock HTTP, verify parse + summary generation.

### Step 2: Redis cache (Day 2 AM)

1. Cache key gen: `SHA256(service + from.Ticks + to.Ticks + traceId ?? "")[..16]` → hex.
2. `IAnalysisCache`:
   ```csharp
   Task<string?> GetHtmlAsync(string key, CancellationToken ct);
   Task SetHtmlAsync(string key, string html, TimeSpan ttl, CancellationToken ct);
   ```
3. Implement với `IDistributedCache` từ Microsoft.Extensions.Caching.StackExchangeRedis.
4. Add Prometheus counter `airca_cache_hits_total`, `airca_cache_misses_total`.
5. **Test**: unit test cache key determinism + TTL behavior.

### Step 3: Anthropic tool_use structured output (Day 2 PM)

1. Define tool schema:
   ```csharp
   public static class ToolDefinitions
   {
       public static readonly object SubmitAnalysis = new
       {
           name = "submit_analysis",
           description = "Submit root cause analysis of the logs",
           input_schema = new {
               type = "object",
               properties = new {
                   root_cause = new { type = "string", maxLength = 200 },
                   evidence = new { type = "array", items = new { type = "object", properties = new {
                       log_line = new { type = "string" },
                       timestamp = new { type = "string" },
                       reasoning = new { type = "string" }
                   } } },
                   suggested_fix = new { type = "string" },
                   confidence = new { type = "string", @enum = new[] { "High", "Medium", "Low" } },
                   confidence_reasoning = new { type = "string" },
                   related_services = new { type = "array", items = new { type = "string" } }
               },
               required = new[] { "root_cause", "evidence", "suggested_fix", "confidence" }
           }
       };
   }
   ```
2. Update `AnthropicLlmProvider`:
   - Pass `tools: [SubmitAnalysis]` + `tool_choice: { type: "tool", name: "submit_analysis" }` in API body.
   - Parse response.content first item type=tool_use → input field → deserialize to `StructuredAnalysis`.
3. Update prompt: bỏ "OUTPUT FORMAT" rules markdown, replace bằng "Call submit_analysis tool với findings".
4. `StructuredAnalysis` POCO mirror schema.

### Step 4: HtmlRenderer update (Day 3 AM)

1. Render từng phần riêng từ `StructuredAnalysis`:
   - Root cause: H2 + paragraph
   - Evidence: bulleted list với timestamp + monospace log line + reasoning
   - Suggested fix: H2 + code block / paragraph
   - Confidence: badge (Green/Yellow/Red)
   - Related services: chip list
2. Disclaimer footer giữ nguyên.

### Step 5: Wire up + smoke test (Day 3 PM)

1. Register `IJaegerClient`, `IAnalysisCache` trong `Program.cs`.
2. Update `LogAnalysisService` flow:
   - Cache lookup FIRST
   - Parallel fetch Loki + Jaeger (use `Task.WhenAll`)
   - Pass both to LLM prompt
   - Cache result HTML sau khi render
3. Trigger 2 lần cùng query → verify request 2 trả về từ cache + metric `cache_hits_total` increment.
4. Trigger với traceId thật từ Jaeger UI → verify trace summary có trong prompt context.

## Todo List

- [ ] JaegerClient + tests
- [ ] Jaeger summary builder
- [ ] Redis cache key + storage + tests
- [ ] Prometheus cache hit/miss metrics
- [ ] Tool definition schema + Anthropic tool_use call
- [ ] StructuredAnalysis POCO
- [ ] HtmlRenderer rewrite cho structured input
- [ ] Update LogAnalysisService với cache + Jaeger
- [ ] Parallel Loki+Jaeger fetch
- [ ] Smoke test cache hit/miss
- [ ] Smoke test trace context end-to-end
- [ ] Update prompt templates (remove markdown rules)

## Success Criteria

- Same query repeated → 2nd request <500ms (cache hit)
- Prometheus shows hit rate ≥30% sau 1 tuần usage
- Với traceId → LLM response cite cross-service errors
- Structured output: UI render từng phần riêng (root cause / evidence / fix)
- Tool use không break existing tests

## Risk Assessment

| Risk | Mitigation |
|------|-----------|
| Jaeger trace lớn (>1000 spans) blow context | Truncate: keep error spans + top 50 slowest |
| Redis down → service crash | Graceful degrade: log warn, skip cache, continue |
| Tool_use schema mismatch → LLM fail | Strict JSON schema validation post-receive, fallback parse text |
| Cache stale sau code change | Include service version trong cache key (optional) |

## Security Considerations

- Cache value HTML có thể chứa redacted log snippets. OK vì đã redact. Verify redact count log không bị cache miss.
- Redis access: internal docker network only, không expose external.

## Next Steps

- Phase 3: web UI tốt hơn (HTMX hoặc Blazor) với follow-up Q&A session.
