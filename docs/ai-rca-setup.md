# AI RCA Service Setup Guide

**Service:** AiRcaService (Port 5013)
**Purpose:** On-demand root cause analysis for logs via Grafana integration

---

## Prerequisites

1. **Anthropic API Key**
   - Sign up at [console.anthropic.com](https://console.anthropic.com)
   - Create API key with at least $50-100 test budget
   - Set `ANTHROPIC_API_KEY` in `.env`:
     ```bash
     ANTHROPIC_API_KEY=sk-ant-xxxxxxxxxxxxx
     ```

2. **Running Infrastructure**
   - Loki: http://loki:3100 (log aggregation)
   - Redis: redis:6379 (caching)
   - Jaeger: http://jaeger:16686 (optional, tracing)
   - Grafana: http://grafana:3000 (UI integration)

---

## Grafana Data Link Configuration

1. Navigate to **Grafana → Dashboards → Loki logs panel**
2. Edit panel → **Data Links** tab
3. Add new data link:
   ```
   Title: AI Root Cause Analysis
   URL: http://localhost:5013/api/ai-rca/analyze?query=${__value.raw}&limit=100
   ```
4. Save dashboard

Now click **"AI Root Cause Analysis"** button on any log line → opens new tab with RCA result.

---

## How to Use

**Workflow:**
1. Dev views logs in Grafana/Loki
2. Clicks "AI Root Cause Analysis" data link on error log
3. AiRcaService queries Loki for context (100 lines around error)
4. Redacts PHI (phone numbers, emails, IDs)
5. Sends to Claude API for analysis
6. Returns HTML: root cause hypothesis + suggested fix
7. Dev reads result in browser new tab

**Example Output:**
```
Root Cause: Database connection timeout after 30s (MySQL connection pool exhausted)

Suggested Fix:
- Increase pool.maxConnections from 10 to 25
- Add connection retry logic with exponential backoff
- Monitor pool.waitQueueSize metric in Prometheus
```

---

## Rate Limits & Costs

| Limit | Value |
|---|---|
| Requests per minute | 60 (Claude API limit) |
| Cache hit target | ≥30% (Redis TTL: 1 hour) |
| Estimated cost | <$20/day (steady state, ~1000 analyses/day) |
| Response time | <15s (P95) |

---

## PHI/Data Protection (MVP)

**IMPORTANT — Development Use Only**

This MVP is **NOT HIPAA-compliant**. Redaction is basic regex-based:
- Phone: `\+?[\d\s-]{10,}` → `[REDACTED_PHONE]`
- Email: `[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}` → `[REDACTED_EMAIL]`
- ID: `\d{9,12}` → `[REDACTED_ID]`

**Do NOT use with production PHI logs.** For BAA compliance, see phase-04-production-compliance.md (deferred).

---

## Follow-up Q&A Sessions (Phase 3)

**Feature:** Multi-turn conversation support for deeper incident analysis

**How it works:**
1. Initial analysis generates unique `sessionId` stored in Redis
2. Dev can ask follow-up questions without re-submitting logs
3. LLM maintains full conversation context across turns
4. Session auto-expires after 30 minutes of inactivity

**Constraints:**
- Max 10 follow-up turns per session (cost control)
- Session TTL: 30 minutes (no persistence)
- Total token budget: 80k per session (conversation truncation if exceeded)
- Response time: <10s per follow-up

**Example Flow:**
```
1. Initial: "Analyze timeout error in DB logs" → sessionId: abc123
2. Follow-up: "Check other MySQL connections around that time?" → context maintained
3. Follow-up: "Show me the trace spans for query X?" → full history retained
4. After 30 min idle: Session expires → "Please start new analysis"
```

**UI Integration:**
- Server-rendered HTML response
- HTMX for follow-up form submission (no JS framework)
- Conversation bubbles append to page
- Auto-scroll + loading indicator

**Rate Limiting:**
- Follow-up requests counted in shared rate limit (10 req/h per IP)
- Prevents session-based abuse

---

## Troubleshooting

**Service not starting?**
```bash
# Check API key is set
echo $ANTHROPIC_API_KEY

# Verify Loki is reachable
curl http://loki:3100/loki/api/v1/query_range?query={job="some-job"}

# Check logs
docker-compose logs ai-rca-service
```

**Slow responses?**
- Check Redis connection: `redis-cli ping`
- Check Loki query latency in Grafana metrics
- Verify ANTHROPIC_API_KEY rate limits not exceeded

**Wrong RCA output?**
- Increase log context limit (phase-03-ui-followup-qa.md)
- Refine redaction patterns for your domain
- Add example traces for Claude fine-tuning (future phase)

---

## Next Steps

- Phase 3: UI improvements (follow-up Q&A, parameter tuning)
- Phase 4: Production BAA + SSO integration
- Phase 5: Fine-tuning on your hospital's common RCAs
