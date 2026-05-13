---
status: completed
created: 2026-05-12
type: feature
---

# AI RCA Service — Tích hợp Grafana Loki

## Context

- **Brainstorm**: [`plans/reports/brainstorm-260512-1507-ai-log-rca-loki.md`](../reports/brainstorm-260512-1507-ai-log-rca-loki.md)
- **Goal**: New .NET 8 microservice cho on-demand root cause analysis. Dev click button trên Grafana log panel → service query Loki, redact PHI, gọi LLM, trả về root cause + fix suggestion.
- **Scope**: MVP test local (no BAA), production compliance defer.

## Architecture

```
Grafana panel → Data Link → AiRcaService
  → LokiClient (HTTP API)
  → PiiRedactor (regex VN)
  → ILlmProvider (Anthropic Claude API)
  → HTML render → browser
```

Service port: **5013**. Gateway route: `/api/ai-rca/*` → `http://ai-rca-service:5013/`.

## Phases

| # | Phase | Status | Effort |
|---|-------|--------|--------|
| 0 | [Audit log schema cho PHI](phase-00-audit-log-schema.md) | completed | 1 ngày |
| 1 | [MVP skeleton + Loki + Anthropic + redact](phase-01-mvp-skeleton.md) | completed | 5 ngày |
| 2 | [Jaeger trace context + Redis cache + structured output](phase-02-jaeger-cache-structured.md) | completed | 2-3 ngày |
| 3 | [Better UI + follow-up Q&A](phase-03-ui-followup-qa.md) | completed | 3-5 ngày |

## Key Dependencies

- **Anthropic API key** (test budget ~$50-100)
- **Existing infra**: Loki:3100, Jaeger:16686, Redis:6379, Gateway (YARP)
- **NotificationService** as template (port 5005, simple pattern)
- **HospitalShared** library cho DI helpers, Serilog, tracing, metrics

## Success Criteria

- Dev click Grafana button → AI response trong <15s
- PII redaction unit tests 100% pass, CI-gated
- Cost <$20/ngày steady state
- Cache hit rate ≥30%
- Zero PHI leak (audit redact count weekly)

## Out of Scope

- Real-time anomaly detection
- Auto-fix / auto-PR
- Multi-turn chat persistence
- Vector DB / RAG
- Custom Grafana plugin
- Slack/Teams integration
- BAA negotiation (defer)

## Production Defer Decisions

- LLM provider strategy: self-host vs cloud + BAA → decide post-MVP
- GPU procurement (nếu self-host)
- SSO/OIDC auth integration
