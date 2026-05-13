# Brainstorm: AI RCA Service tích hợp Loki

**Date:** 2026-05-12
**Type:** Brainstorm summary
**Status:** Đã chốt approach, sẵn sàng plan

---

## 1. Problem statement

Hệ thống microservices .NET (16 services) đã có centralized logging stack (Loki + Seq + ELK + Jaeger + Prometheus + Grafana). Cần thêm khả năng AI-assisted root cause analysis: dev chọn time range + service trên Grafana log panel → click button → AI đọc log từ Loki, phân tích, trả về root cause + suggested fix.

---

## 2. Requirements (đã chốt với user)

| # | Requirement | Decision |
|---|------------|----------|
| 1 | Trigger pattern | **On-demand** (không real-time alerting) |
| 2 | Entry point | **Grafana panel button** (Data link) |
| 3 | Output channel | **Web UI inline** (không Slack/Teams) |
| 4 | Log scale | <10GB/day → no RAG/vector DB |
| 5 | PHI level | Light → bắt buộc redaction layer |
| 6 | Compliance (MVP) | **Bỏ BAA cho MVP** — test local với dev/staging data only |
| 6.1 | Compliance (production) | Defer — quyết định khi go-live (self-host hoặc cloud + BAA) |
| 7 | History | KHÔNG lưu (Redis cache 1h only) |
| 8 | Cloud account | **Chưa có** — không dùng Azure/AWS cho MVP |
| 9 | Auth | **Internal network only** (no SSO) |
| 10 | DNS | Path trên Gateway hiện tại: `api.hospital.local/ai-rca/*` |
| 11 | PHI log keys | Chưa audit — Phase 0 audit log schema |

---

## 3. Architecture

```
[Grafana log panel]
   │ Data link: https://ai-rca.internal/analyze?service=X&from=...&to=...&traceId=...
   ▼
[AiRcaService - new .NET 8 microservice]
   ├─ 1. Validate + rate-limit (per user)
   ├─ 2. Redis cache lookup (hash query → result)
   ├─ 3. Loki HTTP API → fetch logs trong window
   ├─ 4. Jaeger API → fetch trace (nếu có traceId)
   ├─ 5. PII Redactor (regex pack VN context)
   ├─ 6. LLM call → Azure OpenAI GPT-4o (BAA covered)
   ├─ 7. Format markdown
   ├─ 8. Cache vào Redis (TTL 1h)
   └─ 9. Render HTML response (server-side)
   ▼
[Browser tab] - dev xem result inline
```

**Không build:** Slack bot, Grafana custom plugin, vector DB, DB persistence, fine-tuned model.

---

## 4. LLM provider decision

### MVP Final: **Anthropic Claude API direct** (Claude 3.5 Sonnet hoặc Claude 4 Sonnet)

Strategy: test local nhanh, bỏ BAA cho MVP, dùng dev/staging logs only. **Production deployment defer quyết định compliance.**

**Lý do chọn Anthropic direct cho MVP:**
- Context 200k tokens → fit cả batch log + traces + stack trace cùng lúc
- Tool use API ngon cho structured output (root cause / evidence / fix / confidence)
- Quality phân tích log/RCA top tier
- Setup nhanh: API key → ready (vài giờ)
- Không cần cloud subscription procurement

**LLM client abstraction (bắt buộc):** Wrap behind `ILlmProvider` interface → swap dễ khi go production:
- Production option A: self-host Ollama/vLLM (no BAA needed)
- Production option B: Azure OpenAI / AWS Bedrock (BAA covered)
- Decision defer đến lúc có data về adoption + cost

| Provider | Use case | When |
|----------|----------|------|
| Anthropic API direct | MVP / local test | NOW |
| Self-host Ollama (Llama 3.3 70B) | Production, no cloud | Khi go-live, on-prem GPU available |
| Azure OpenAI / Bedrock | Production, BAA path | Khi go-live, cloud procurement xong |

**Constraint cho MVP:** TUYỆT ĐỐI không dùng production logs có PHI thật. Test với:
- Dev/staging environment logs
- Synthetic logs (generate từ error scenarios)
- Anonymized log samples

---

## 5. PII Redaction (compliance gate)

Bắt buộc chạy TRƯỚC khi gửi LLM. Built-in .NET, không thêm Python service (Presidio).

**Regex pack VN context:**
- CCCD/CMND: `\b\d{9}\b`, `\b\d{12}\b`
- BHYT: `[A-Z]{2}\d{13}`
- Phone VN: `(0|\+84)\d{9,10}`
- Email: standard RFC
- Patient name (nếu structured log có key `patientName` / `fullName` → mask value)
- Address: skip cho MVP (low-risk)

**Output:** replace với `[REDACTED:CCCD]`, `[REDACTED:PHONE]`, etc. **Log redact count** cho audit (chỉ count, không log value).

**Unit test bắt buộc:** test suite với sample logs chứa PHI → đảm bảo 100% redacted trước khi gọi LLM. CI fail nếu test rớt.

---

## 6. Context assembly strategy

Quyết định 80% chất lượng output. Prompt LLM nhận:

1. **Error logs** (ERROR/FATAL level trong window — max 1000 lines)
2. **Surrounding context** (±5 phút INFO/WARN cùng correlationId — max 2000 lines)
3. **Jaeger trace** nếu traceId có → upstream/downstream services
4. **Service metadata**: tên service, version (từ Serilog enricher)
5. **System prompt** (persona "senior SRE", strict output format):
   - Root cause (≤200 chars)
   - Evidence (cite exact log lines với line number)
   - Suggested fix (concrete, actionable)
   - Confidence: High/Medium/Low
   - Related services (nếu cross-service issue)

**Token budget:** ~50k input + 4k output → fit cả GPT-4o 128k và Claude 200k thoải mái.

---

## 7. Grafana integration

**Approach: Panel Data Link** (không build plugin).

Trong Grafana log panel → Panel options → Data links → New link:
```
URL: http://localhost:5000/api/ai-rca/analyze?service=${__field.labels.service}&from=${__from:date:iso}&to=${__to:date:iso}&traceId=${__data.fields.traceId}
Open in new tab: ✅
```

(MVP local: `localhost:<port>`. Production: route qua Gateway hiện tại `api.hospital.local/ai-rca/*`.)

Click trên log line → mở tab mới với params → AiRcaService render kết quả.

**Upgrade path (V1.2):** nếu cần better UX → build Grafana app plugin với React panel inline.

---

## 8. Rate limit & cost control

| Control | Value |
|---------|-------|
| Per user rate limit | 10 requests / hour |
| Max time window query | 1 hour |
| Max log lines fetched | 5000 |
| Max input tokens | 50k |
| Max output tokens | 4k |
| Redis cache TTL | 1 hour |
| Daily budget alert | $30/day (Azure cost alert) |

---

## 9. Phased rollout

| Phase | Scope | Effort |
|-------|-------|--------|
| **MVP** | New .NET service + Loki client + PII redact + Azure OpenAI + Grafana data link + simple HTML render | **5 ngày** |
| **V1.1** | Jaeger trace context, Redis cache, structured tool-use output, redact audit logging | 2-3 ngày |
| **V1.2** | Better UI (HTMX hoặc Blazor), follow-up Q&A trên cùng session | 3-5 ngày |
| **V2 (defer)** | Vector DB cho historical incident matching, auto-trigger từ alert rule | TBD |

---

## 10. Stack reuse

Tận dụng infra hiện có, **không** thêm dependency mới (ngoài SDK Azure):
- ✅ Loki (existing) — log source
- ✅ Jaeger (existing) — trace context
- ✅ Redis (existing) — cache
- ✅ Serilog correlation ID (existing) — links logs ↔ traces
- ✅ .NET microservice template (existing) — copy structure
- ➕ Azure.AI.OpenAI NuGet — LLM client
- ➕ Azure OpenAI resource (BAA covered)

---

## 11. Risks & mitigation

| Risk | Mitigation |
|------|-----------|
| PHI leak ra cloud LLM | Redaction unit tests CI-gated + audit count log + BAA signed |
| AI hallucinate fix sai | Buộc cite log line evidence + UI disclaimer "AI suggestion, dev verify" + Confidence level |
| Cost runaway | Per-user rate limit + Redis cache + token cap + Azure cost alert |
| Loki query timeout window lớn | Cap 1h window + 5000 lines |
| Service goes down | Non-critical service — degrade gracefully, không block dev workflow |
| LLM provider downtime | Show error message, dev fallback xem log thủ công (acceptable) |

---

## 12. Success metrics

- **Adoption:** ≥5 dev/tuần sử dụng (track via access log, no PII)
- **Cache hit rate:** ≥30% (chỉ ra incident được nhiều người xem)
- **Avg response time:** <15s từ click button đến render result
- **PHI leak incidents:** 0 (audit redact count weekly)
- **Cost:** <$20/ngày steady state

---

## 13. Out of scope (DO NOT BUILD)

- Real-time anomaly detection / auto-alerting
- Auto-fix / auto-PR generation
- Multi-turn chat history persistence
- Fine-tuning custom model
- Vector DB / RAG pipeline
- Custom Grafana plugin (use data link)
- Slack/Teams bot integration
- Multi-tenant / multi-hospital support

---

## 14. Unresolved questions (defer to later phases)

1. **Production LLM strategy:** Sau MVP, đi self-host (Ollama + GPU) hay cloud + BAA (Azure/AWS)? → quyết định sau khi đo adoption + cost.
2. **PHI log audit:** Phase 0 cần audit toàn bộ structured log keys → list ra field nào có thể chứa PII/PHI để build regex pack chính xác.
3. **GPU procurement:** nếu chọn self-host production, cần spec hardware (1x A100 40GB hoặc 2x A6000 đủ cho Llama 3.3 70B Q4).
4. **Anthropic API budget:** team cần approve ngân sách test (~$50-100 cho MVP test phase).
