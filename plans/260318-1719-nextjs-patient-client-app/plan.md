---
title: "Next.js Patient Client App"
description: "Patient-facing web portal connecting to Hospital HRM microservices via API Gateway"
status: completed
priority: P1
effort: 20h
branch: feature/patient-client-app
tags: [nextjs, patient-portal, frontend, keycloak, docker]
created: 2026-03-18
completed: 2026-03-19
---

# Next.js Patient Client App

## Overview

Patient-facing web app (Next.js 14 App Router + TypeScript) connecting to existing Hospital HRM microservices through the YARP API Gateway. Auth via NextAuth.js v5 + Keycloak OIDC.

**Location:** `client/patient-app/`
**Port:** 3100 (Grafana occupies 3000)
**Stack:** Next.js 14, TypeScript, shadcn/ui, Tailwind, TanStack Query, react-hook-form + Zod

## Research Reports

- [Keycloak Auth](../reports/researcher-260318-1717-nextjs-keycloak-auth.md)
- [Patient Portal UI](../reports/researcher-260318-1717-nextjs-patient-portal.md)

## Phases

| # | Phase | File | Status | Effort |
|---|-------|------|--------|--------|
| 1 | Project Setup | [phase-01](phase-01-project-setup.md) | completed | 3h |
| 2 | Auth Integration | [phase-02](phase-02-auth-integration.md) | completed | 4h |
| 3 | API Layer | [phase-03](phase-03-api-layer.md) | completed | 3h |
| 4 | Core Pages | [phase-04](phase-04-core-pages.md) | completed | 6h |
| 5 | UI Polish | [phase-05](phase-05-ui-polish.md) | completed | 2h |
| 6 | Testing & Deployment | [phase-06](phase-06-testing-and-deployment.md) | completed | 2h |

## Key Architecture Decisions

1. **NextAuth.js v5** for Keycloak OIDC -- no custom OAuth code
2. **Server Components default**, Client Components only for interactivity
3. **API calls through Gateway** (port 8000) -- never call services directly
4. **Route groups** `(auth)` and `(dashboard)` for layout separation
5. **Port 3100** to avoid Grafana (3000) conflict
6. **Multi-stage Docker** for production image (~200MB)

## Dependencies

- Keycloak realm "hospital" with client "hospital-patient-portal" configured
- API Gateway running on port 8000 with CORS for localhost:3100
- All backend services healthy

## Decisions

- **Patient self-registration:** Admin-only for MVP. Patients are created by staff via PatientService API. No registration page needed — removes ~3 Phase 4 files. [AUDIT FIX F5]
- **Provider list:** Verify `/api/providers` gateway route before Phase 4. Fallback: plain text input for MVP. [AUDIT FIX F12]
- **Patient ID mapping:** Resolve Keycloak `sub` vs Patient Service UUID in Phase 3 spike before writing hooks. [AUDIT FIX F8]

## Unresolved Questions

1. Role claim location in Keycloak token (`roles` vs `resource_access`)? — verify during Keycloak client setup (Phase 2 Step 13)
2. Does YARP gateway already have CORS for `localhost:3100`? — check `gateway/HospitalGateway/appsettings.json` before Phase 3

## Validation Log

### Session 1 — 2026-03-19
**Trigger:** Post-audit validation interview
**Questions asked:** 5

#### Questions & Answers

1. **[Architecture]** How does Patient Service identify a patient — Keycloak sub or internal UUID?
   - Options: Same (sub=ID) | Email lookup | Not confirmed
   - **Answer:** Same — Keycloak sub = patient ID
   - **Rationale:** All hooks can use `session.user.id` (Keycloak sub) directly. No ID mapping spike needed.

2. **[Assumptions]** Where is the `patient` role in the Keycloak JWT?
   - Options: Top-level `roles` | resource_access | realm_access | Not confirmed
   - **Answer:** Top-level `roles` array
   - **Rationale:** JWT callback extracts via `token.roles as string[]`. Simple, no client-specific path.

3. **[Assumptions]** Is YARP already configured with CORS for localhost:3100?
   - Options: Yes | No | Not sure
   - **Answer:** Yes — already configured
   - **Rationale:** Phase 3 can proceed without backend changes.

4. **[Scope]** Does Gateway expose GET /api/providers?
   - Options: Yes | No (text input MVP) | Not sure
   - **Answer:** Yes — endpoint exists
   - **Rationale:** Create `useProviders()` hook; render as Select dropdown in schedule form.

5. **[Architecture]** Role enforcement in middleware or Keycloak-only?
   - Options: Keycloak only | Middleware role check | Per-page check
   - **Answer:** Keycloak only — middleware just checks valid session
   - **Rationale:** Simpler middleware. YAGNI — Keycloak controls role at login.

#### Confirmed Decisions
- Patient ID = Keycloak JWT `sub` — no mapping needed, use `session.user.id` in hooks
- Role claim at `token.roles[]` — simple array extraction in JWT callback
- CORS already configured — no gateway changes needed
- Providers endpoint exists — add `useProviders()` hook, render Select in schedule form
- No role check in middleware — valid session is sufficient for MVP

#### Action Items
- [ ] Phase 3: Remove patient ID spike step, use `session.user.id` directly in hooks
- [ ] Phase 2: Extract roles via `token.roles as string[]` (not resource_access)
- [ ] Phase 3: Remove CORS risk/mitigation (already handled)
- [ ] Phase 3: Add `useProviders()` hook for schedule form
- [ ] Phase 2: Middleware only checks for valid session (no role check)

#### Impact on Phases
- Phase 2: JWT callback uses `token.roles`, middleware simplified
- Phase 3: No ID spike needed; add `useProviders()` hook
- Phase 4: Schedule form uses Select with provider list from API

## Project Completion Summary

**Status:** COMPLETED (2026-03-19)
**Total Effort:** 20h (100% complete)
**Quality Gates:** PASSED

### Deliverables

**1. Application Structure**
- Next.js 14 App Router with TypeScript strict mode
- 6 dashboard pages + 2 auth pages + 3 error/layout pages
- 29 UI components + 8 hooks + 7 utilities
- Single source of truth: API Gateway (port 8000)

**2. Authentication & Authorization**
- NextAuth.js v5 + Keycloak OIDC integration
- Token refresh mutex preventing concurrent request race conditions
- Role extraction from JWT (confirmed: top-level `roles` array)
- Middleware route protection (valid session + 401 redirect)
- Logout with Keycloak end_session call

**3. API Integration**
- Server-side Bearer token injection via `callGatewayAPI()`
- Client-side proxy with explicit IDOR-prevention allowlist
- TanStack Query hooks for optimistic updates
- Zod validation for all form inputs
- 404/5xx error handling with user-friendly messages

**4. UI/UX**
- Dashboard layout with responsive sidebar (mobile hamburger overlay)
- Dark mode support via next-themes
- Toast notifications for mutations
- Loading skeletons on all data-fetching pages
- Error boundaries with retry buttons
- Accessibility: WCAG AA labels, headings, color contrast, focus management

**5. Data Pages**
- **Dashboard:** Stats cards (appointments, records, prescriptions), upcoming appointments list, recent records
- **Appointments:** Paginated list + status filter, schedule form (provider Select, date/time picker), detail view, cancel with confirmation
- **Medical Records:** List with date filter, detail view + lab results table
- **Prescriptions:** List with status filter (active/expired)
- **Profile:** View patient info, edit form (name, phone, address)

**6. Testing & Quality**
- 40/40 unit tests passing (Vitest + React Testing Library)
- Coverage: Zod schemas, components, utilities
- `npm run build` passes: 0 TypeScript errors, 0 warnings
- Smoke test script with explicit status assertions
- Docker multi-stage build ready (standalone output)

**7. Security Fixes Applied**
- [F1] NEXTAUTH_SECRET: no default, required on startup
- [F2] API proxy: explicit allowlist prevents IDOR (patients can only access own data paths)
- [F3] NEXTAUTH_URL: environment variable (not hardcoded localhost)
- [F4] Token refresh: in-process mutex prevents single-use refresh token race
- [F6] Middleware: NextAuth v5 pattern (auth export, not withAuth)
- [F7] Session callback: accessToken kept server-side only (not exposed to client)
- [F9] Dockerfile: standalone output with proper multi-stage build
- [F10] Gateway URL: server-only env var (no NEXT_PUBLIC prefix)
- [F11] Logout: Keycloak end_session called before session clear
- [F14] CSP header: default-src 'self' configured in next.config.ts
- [F15] Smoke test: assertions + exit 1 on failure (not silent pass)

### Architecture Decisions Ratified

1. **JWT Strategy** — No custom OAuth; rely entirely on NextAuth v5 + Keycloak OIDC
2. **Server Components by Default** — Only Client Components for interactive features (filters, forms, mutations)
3. **Single API Gateway** — Never call services directly; all paths via localhost:8000 (or gateway:8000 in Docker)
4. **Port 3100** — Avoids Grafana (3000) conflict
5. **Role Enforcement at Keycloak** — Middleware only checks for valid session; Keycloak controls patient role assignment at login
6. **Patient ID = Keycloak JWT Sub** — No mapping spike; use `session.user.id` directly in hooks
7. **Provider List via API** — GET /api/providers endpoint confirmed; render as Select dropdown

### Known Limitations & Deferred Tasks

**Docker Build Verification:** Deferred (MVP for Windows local dev; CI pipeline will test)
**E2E Tests:** Future phase (Playwright full login flow)
**Hook Unit Tests:** Deferred (MSW + App Router complexity; simple fetch mocks sufficient for MVP)
**API Client Unit Tests:** Deferred (requires next-auth mocking; deferred per risk note)

### Post-Implementation Checklist

- [x] All 6 phases marked `completed`
- [x] All phase todos checked
- [x] npm run test passes (40/40)
- [x] npm run build passes (0 errors)
- [x] TypeScript strict mode clean
- [x] Docker Dockerfile created (multi-stage, standalone)
- [x] docker-compose.yml updated with patient-client service
- [x] Smoke test script created + reviewed
- [x] All 15 audit findings applied
- [x] Code review feedback integrated (Keycloak logout URL, proxy 204/non-JSON, session errors, Cache-Control headers)
- [x] Plan documentation 100% updated

## Audit Review

### Session — 2026-03-18
**Findings:** 15 (15 accepted, 0 rejected)
**Severity breakdown:** 6 Critical, 7 High, 2 Medium

| # | Finding | Severity | Disposition | Applied To |
|---|---------|----------|-------------|------------|
| 1 | NEXTAUTH_SECRET insecure default/fallback | Critical | Accept | Phase 1 |
| 2 | Open proxy catch-all — IDOR/unauthorized access | Critical | Accept | Phase 3 |
| 3 | NEXTAUTH_URL localhost hardcoded — Docker OAuth breaks | Critical | Accept | Phase 1 |
| 4 | Token refresh race — concurrent requests invalidate single-use token | Critical | Accept | Phase 2 |
| 5 | Self-registration scope unresolved — changes 4 phases | Critical | Accept | plan.md |
| 6 | `withAuth` removed in NextAuth v5 — wrong middleware API | Critical | Accept | Phase 2 |
| 7 | accessToken exposed in client-side session — PHI via XSS | High | Accept | Phase 2 |
| 8 | Patient ID mapping unresolved — blocks all data fetching | High | Accept | Phase 3 |
| 9 | Dockerfile missing `output: 'standalone'` — SSR runtime crash | High | Accept | Phase 1 |
| 10 | NEXT_PUBLIC_GATEWAY_URL leaks internal topology | High | Accept | Phase 1 |
| 11 | No Keycloak end_session on logout — tokens valid post-logout | High | Accept | Phase 2 |
| 12 | Provider list API unverified for appointment scheduling | High | Accept | Phase 4 |
| 13 | Dual data-fetching hybrid adds complexity without clear rule | High | Accept | Phase 3 |
| 14 | No CSP/HTTP security headers | Medium | Accept | Phase 1 |
| 15 | Smoke test prints status but never asserts — exits 0 on failure | Medium | Accept | Phase 6 |
