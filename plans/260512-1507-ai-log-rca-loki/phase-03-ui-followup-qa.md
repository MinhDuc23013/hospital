---
phase: 3
status: completed
priority: low
effort: 3-5 days
---

# Phase 3 (V1.2): Better UI + Follow-up Q&A Session

## Context Links

- Plan: [plan.md](plan.md)
- Depends on: [phase-02-jaeger-cache-structured.md](phase-02-jaeger-cache-structured.md)

## Overview

- **Priority**: Low — nice-to-have, validate adoption Phase 1+2 trước
- **Status**: completed
- **Description**: Thay static HTML response bằng interactive UI cho phép dev hỏi follow-up câu hỏi trên cùng incident context.

## Key Insights

- Static HTML đủ cho 1-shot RCA nhưng dev thường có follow-up: "Còn DB connection nào khác bị timeout không?", "Xem trace của span X?".
- Server-rendered + HTMX = đơn giản, không cần SPA framework, không tăng deploy complexity.
- Session-scoped (in-memory hoặc Redis short TTL) — không persist (theo brainstorm constraint).

## Requirements

**Functional**:
- Initial render giống Phase 1/2.
- Thêm input box "Ask follow-up..." dưới result.
- Submit → POST `/api/ai-rca/followup` với session ID → LLM nhận context cũ + câu hỏi mới → return HTML fragment append vào trang.
- Session lifetime: 30 phút từ last interaction. Tự expire.

**Non-functional**:
- No JS framework dependency (HTMX only).
- Follow-up response <10s.

## Architecture

```
Initial GET /analyze  → render result + sessionId stored in Redis
User input follow-up  → POST /followup with sessionId
  ↓
[FollowupController]
  ├─ Validate session exists
  ├─ Load conversation history từ Redis
  ├─ Append user question
  ├─ Call AnthropicLlmProvider with full history
  ├─ Save updated history
  └─ Return HTML fragment (HTMX swap target)
```

## Project Structure (additions)

```
services/AiRcaService/
├── Controllers/
│   └── FollowupController.cs
├── Application/Services/
│   ├── IConversationStore.cs
│   └── RedisConversationStore.cs       ← 30min TTL
├── Domain/
│   └── Conversation.cs                  ← sessionId, messages[]
├── wwwroot/
│   ├── index.html                       ← shell với HTMX script
│   ├── css/styles.css
│   └── partials/
│       ├── analysis-result.html         ← initial render template
│       └── followup-message.html        ← Q&A bubble
```

## Implementation Steps

### Step 1: HTMX shell page (Day 1)

1. Convert response từ ContentResult → Razor view hoặc plain HTML template.
2. Include HTMX `<script src="https://unpkg.com/htmx.org@1.9.x">` (or self-host).
3. Layout:
   - Header: service / time range / cache status
   - Main: analysis result (root cause / evidence / fix / confidence)
   - Followup section: textarea + submit button (hx-post to /followup)
   - Footer: disclaimer

### Step 2: Conversation store (Day 2)

1. `Conversation { string SessionId, List<Message> Messages, DateTime LastActivity }`.
2. `Message { string Role (user/assistant), string Content, DateTime Timestamp }`.
3. Initial analysis stores first user+assistant turn.
4. Redis key `conversation:{sessionId}`, TTL refresh on each access (30min).

### Step 3: FollowupController (Day 3)

1. `POST /followup`:
   - Body: `{ sessionId, question }`
   - Load conversation → validate exists
   - Build Anthropic messages array với full history
   - Call LLM (cùng tool_use schema HOẶC plain text cho follow-up — chọn plain để natural)
   - Append assistant response to conversation
   - Save → return HTML fragment (Q + A bubbles)

### Step 4: HTMX integration (Day 4)

1. Form submit → `hx-post="/followup"` + `hx-target="#conversation"` + `hx-swap="beforeend"`
2. Loading indicator
3. Auto-scroll to bottom
4. Clear input on success

### Step 5: Cost & rate limit (Day 4 PM)

1. Follow-up cũng count vào rate limit (10 req/h per IP gộp chung).
2. Token budget: max 10 follow-up turns per session (avoid runaway context).
3. Conversation history truncation: nếu total tokens > 80k → drop oldest user/assistant pair.

### Step 6: E2E test (Day 5)

1. Initial analyze → render
2. Ask 3 follow-ups → verify context maintained
3. Wait 31 min → verify session expired → error message
4. Verify rate limit shared

## Todo List

- [ ] Shell HTML + HTMX integration
- [ ] CSS styling (đơn giản, sạch)
- [ ] Conversation domain model
- [ ] RedisConversationStore + tests
- [ ] FollowupController
- [ ] Update AnthropicLlmProvider để nhận messages array (multi-turn)
- [ ] Token budget enforcement + history truncation
- [ ] E2E follow-up test
- [ ] Update docs/ai-rca-setup.md với UI flow

## Success Criteria

- Follow-up Q&A maintained context across 3+ turns
- Session expires đúng 30 min idle
- No JS framework dependency
- Cost không tăng quá 2x per session (vs 1-shot)

## Risk Assessment

| Risk | Mitigation |
|------|-----------|
| Conversation context blow token limit | Hard cap 10 turns + token-based truncation |
| User abuse rate limit qua follow-up | Shared counter với analyze endpoint |
| Session hijack (session ID leak) | Generate cryptographic random GUID, only accept POST với matching IP (optional) |
| HTMX CDN downtime | Self-host HTMX trong wwwroot |

## Security Considerations

- Session ID: 128-bit GUID, non-guessable.
- Redis access internal only.
- Follow-up question từ user TEXT — không redact (user nhập tự viết), nhưng warn UI: "Đừng paste log thật vào ô này".

## Next Steps

- Validate adoption: track follow-up usage rate. Nếu <10% sessions dùng follow-up → revert sang static HTML, kill phase này.
- V2 (defer): vector DB cho historical incident pattern matching, auto-trigger từ Alertmanager.
