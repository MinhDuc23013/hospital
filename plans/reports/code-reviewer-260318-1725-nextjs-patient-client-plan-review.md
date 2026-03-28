---
title: "Plan Review: Next.js Patient Client App"
reviewer: code-reviewer
date: 2026-03-18
plan: plans/260318-1719-nextjs-patient-client-app/
---

# Plan Review: Next.js Patient Client App (Assumption Destroyer)

## Finding 1: Docker NEXTAUTH_URL is wrong for container-internal use

- **Severity:** Critical
- **Location:** Phase 1, "Add to docker-compose.yml"
- **Flaw:** `NEXTAUTH_URL` is set to `http://localhost:3100` inside the container environment block. Inside Docker, `localhost` refers to the container itself — which is correct for NextAuth's own callback URL generation. However, when Keycloak redirects the browser back to the callback URL, it uses whatever host the browser resolved. If the app is accessed via a domain or reverse proxy (not `localhost:3100`), the callback URL will be wrong and Keycloak will reject it with a redirect_uri_mismatch error.
- **Failure scenario:** Any non-localhost deployment (staging, production, even a VM-hosted dev box) will break the Keycloak callback. Keycloak enforces exact redirect URI matching. The browser hits `http://192.168.x.x:3100/api/auth/callback/keycloak` but NEXTAUTH_URL says `localhost:3100` — Keycloak rejects with 400.
- **Evidence:** `NEXTAUTH_URL=http://localhost:3100` hardcoded in docker-compose env block. No `${APP_URL}` variable or override documented.
- **Suggested fix:** Make it `NEXTAUTH_URL=${NEXTAUTH_URL:-http://localhost:3100}` in docker-compose and document that it must be set to the externally accessible URL in any non-local environment.

---

## Finding 2: Dockerfile copies node_modules from `deps` stage but builder re-runs npm ci

- **Severity:** High
- **Location:** Phase 1, "Create Dockerfile (multi-stage)"
- **Flaw:** The `deps` stage runs `npm ci --only=production`. The `builder` stage then runs `npm ci` again (without `--only=production`, correctly for build-time deps). The `runner` stage then copies `node_modules` from `deps` — the production-only install. This is the intended pattern, but the `builder` stage never copies from `deps`; it installs its own `node_modules` independently. This means the build layer doubles the node_modules work. More critically, if `next build` requires any devDependencies that are missing from the `deps` stage (e.g., TypeScript, PostCSS plugins), the `runner` stage will be missing them. The `builder` has them, but runner only gets the `deps` node_modules.
- **Failure scenario:** `next start` in `runner` fails with "Cannot find module 'some-dep'" if any runtime-required package was accidentally in devDependencies. The image may also fail the `<250MB` target since node_modules from `deps` includes all transitive production deps.
- **Evidence:** `COPY --from=deps /app/node_modules ./node_modules` in runner stage; runner never installs or copies from builder.
- **Suggested fix:** Either copy `node_modules` from `builder` (which has full deps for the build) and accept a larger image, or use the standalone output feature (`output: 'standalone'` in next.config.ts) which is the standard Next.js Docker pattern and produces images well under 200MB without this complexity.

---

## Finding 3: Token refresh runs "on every request" — race condition with concurrent requests

- **Severity:** High
- **Location:** Phase 2, "Architecture > Token Refresh"
- **Flaw:** The plan states "JWT callback checks expiresAt on every request." NextAuth's JWT callback is invoked on every `getServerSession()` / `auth()` call. If a user has two concurrent requests (e.g., dashboard page making 3 parallel server component fetches), all three will see an expired token simultaneously, all three will attempt a refresh with the same refresh_token, and 2 of the 3 refresh calls will fail because Keycloak's refresh token is single-use (rotation enabled by default).
- **Failure scenario:** User navigates to the dashboard. Three Server Components call `getAuthSession()` concurrently at token expiry time. All three trigger `refreshAccessToken()`. The first succeeds and Keycloak rotates the refresh token. The second and third calls use the now-invalidated refresh token — Keycloak returns 400. All three set the error flag. User is forcibly logged out mid-session even though the first refresh succeeded.
- **Evidence:** "JWT callback checks expiresAt on every request / Update JWT with new access_token" — no mention of locking, deduplication, or mutex for concurrent refresh.
- **Suggested fix:** Add a brief buffer (e.g., refresh only if token expires within 60 seconds) to reduce the window. For production, implement a server-side token cache keyed by user ID with a mutex/lock, or use Next.js `unstable_cache` to deduplicate within a request cycle.

---

## Finding 4: API proxy route is an open forward proxy — no path allowlist

- **Severity:** High
- **Location:** Phase 3, "Create API proxy route (`app/api/proxy/[...path]/route.ts`)"
- **Flaw:** The plan describes a catch-all proxy: "Catches all `/api/proxy/*` requests... Forwards to Gateway API." There is no mention of a path allowlist or validation of what paths are forwarded. An authenticated patient can call `/api/proxy/admin/users`, `/api/proxy/staff/salary`, or any other gateway endpoint by constructing the path themselves.
- **Failure scenario:** A logged-in patient opens browser devtools and calls `fetch('/api/proxy/patients?all=true')` or `/api/proxy/staff/anything`. The proxy injects their valid Bearer token and forwards the request. The backend must enforce authorization — but the plan's security section says "Patient can only view their own data (enforced by backend + JWT patientId)." If the gateway or any backend service has a misconfigured route, patient data from other patients or privileged endpoints becomes accessible.
- **Evidence:** "Catches all `/api/proxy/*` requests" — no allowlist. Security section does not mention proxy path restriction.
- **Suggested fix:** Define an explicit allowlist of permitted proxy paths (`/appointments`, `/medical-records`, `/prescriptions`, `/patients/me`). Return 403 for anything not in the list. This is defense-in-depth on top of backend authorization.

---

## Finding 5: Patient ID source is unresolved but load-bearing for all data fetching

- **Severity:** High
- **Location:** Phase 4, "Risk Assessment" row: "Patient ID not in session"
- **Flaw:** Every API hook is scoped to a patient ID: `useMedicalRecords(patientId)`, `usePatientProfile()` calls `/api/proxy/patients/:id`, `usePrescriptions(patientId?)`. The plan acknowledges this as a risk — "Extract from JWT sub claim or fetch profile on login" — but defers the decision. This is not a risk to mitigate later; it is a foundational data flow question that must be answered before Phase 3 code can be written correctly. The JWT `sub` claim in Keycloak is the Keycloak user UUID, which may not equal the backend `Patient.id` (a separate UUID from the patient service).
- **Failure scenario:** `useMedicalRecords(session.user.sub)` is called. The backend patient service stores patients with its own UUID, not the Keycloak UUID. The call returns 404 or an empty result for every patient. The entire app renders empty states. No error is shown because 404 is treated as "no records."
- **Evidence:** Phase 4 risk table: "Patient ID not in session | Medium | High | Extract from JWT sub claim or fetch profile on login." Phase 3 type definitions define `Patient.id: string` and `MedicalRecord.patientId: string` with no documented mapping to Keycloak `sub`.
- **Suggested fix:** Resolve this before Phase 3. Options: (a) backend embeds patient service UUID in the Keycloak token as a custom claim, (b) a "resolve patient" API call is made on first login and the ID is stored in the session. Document the chosen approach in Phase 2 before any API hook is written.

---

## Finding 6: Schedule appointment form assumes a provider list API exists — not verified

- **Severity:** High
- **Location:** Phase 4, Step 10: "Schedule form with: provider select..."
- **Flaw:** The schedule form requires a provider select dropdown. This implies a "list available providers" API endpoint accessible to patients through the gateway. Phase 4's own risk table acknowledges this: "Provider list API not available | Medium | Medium | Hardcode sample providers for MVP." This is not a mitigation — it is a placeholder that will ship broken or with fake data.
- **Failure scenario:** The schedule form renders with hardcoded provider names. A patient selects "Dr. Smith" (hardcoded), submits the form, and the backend receives a `providerId` that does not exist. The appointment service returns 400 or 422. The form shows a generic error. The entire appointments scheduling feature is non-functional for real data.
- **Evidence:** Phase 4 risk: "Hardcode sample providers for MVP." No investigation of whether a `/providers` or `/doctors` endpoint exists in the gateway. No task in Phase 3 to verify this endpoint or create a `useProviders()` hook.
- **Suggested fix:** Before Phase 4 implementation, verify whether the gateway exposes a provider/doctor listing endpoint accessible to patients. If not, this is a backend dependency that must be raised with the backend team. "Hardcode" is not acceptable for a medical scheduling form.

---

## Finding 7: 20-hour total effort estimate assumes zero backend integration friction

- **Severity:** High
- **Location:** plan.md, "Effort: 20h"
- **Flaw:** The 20-hour estimate allocates 2h for Testing & Deployment. The plan has three explicitly unresolved questions at the time of writing (role claim location, CORS configuration, patient self-registration). Each of these could add 2-8 hours of debugging. The Keycloak integration alone (token refresh race condition, role extraction, redirect URI mismatch) historically takes longer than estimated. The estimate has no buffer.
- **Failure scenario:** Phase 2 auth integration hits the Keycloak issuer URL problem (Docker vs. host), the role claim is in `resource_access` not `roles`, and CORS is not configured on the gateway. Each issue requires investigation, a backend team coordination loop, and re-testing. Phase 2 alone exceeds its 4h budget. Phases 3 and 4 are blocked. The 20h estimate becomes 35-40h, and the project stalls.
- **Evidence:** plan.md Unresolved Questions: "1. Role claim location in Keycloak token? 2. Does YARP gateway already have CORS for client origins?" These are unknowns that directly block Phase 2 and Phase 3 respectively, yet neither has a resolution task or spike allocated.
- **Suggested fix:** Add a Phase 0 or pre-flight spike (2-4h) to resolve all three unresolved questions before implementation begins. The spike output gates Phase 2 start. Revise total effort to 28-32h with a 20% buffer.

---

## Finding 8: Middleware uses `withAuth` from NextAuth v4 — breaking change in v5

- **Severity:** Critical
- **Location:** Phase 2, Step 7: "Use NextAuth withAuth middleware"
- **Flaw:** NextAuth v5 (Auth.js v5) removed `withAuth` and replaced it with a completely different middleware pattern using `auth()` as middleware directly. The plan pins `next-auth@5` in Phase 1 dependencies but then references the NextAuth v4 middleware API (`withAuth`) in Phase 2. This is a confirmed breaking change.
- **Failure scenario:** Developer installs `next-auth@5`, implements `middleware.ts` using `import { withAuth } from 'next-auth/middleware'` as described, and gets a runtime error: "withAuth is not a function" or a module not found error. Route protection does not work. All protected routes are publicly accessible. The developer must rewrite middleware using the v5 `auth()` export before any protected page can be tested.
- **Evidence:** Phase 2, Step 7: "Use NextAuth withAuth middleware." Phase 1, Step 4: `npm install next-auth@5`. The two phases are internally inconsistent.
- **Suggested fix:** Replace all `withAuth` references with the NextAuth v5 pattern: `export { auth as middleware } from '@/lib/auth-config'` with a `callbacks.authorized` function. Update the middleware implementation steps to match the v5 API.

---

## Finding 9: No CSRF protection documented for the API proxy route (POST/PUT/DELETE)

- **Severity:** Medium
- **Location:** Phase 3, "Create API proxy route"
- **Flaw:** The proxy route handles mutations (POST scheduling, PUT profile update, DELETE cancel). NextAuth uses `httpOnly` session cookies. Any third-party site can craft a form or fetch call targeting `http://localhost:3100/api/proxy/appointments` and the browser will send the session cookie automatically. The plan has no mention of CSRF tokens, `SameSite` cookie validation, or `Origin` header verification on the proxy route.
- **Failure scenario:** A patient visits a malicious site while logged into the portal. The malicious page executes `fetch('http://localhost:3100/api/proxy/appointments', { method: 'POST', credentials: 'include', body: ... })`. The request goes through with the patient's valid session cookie. An appointment is booked or cancelled without the patient's knowledge.
- **Evidence:** Security section in Phase 3 and Phase 4 makes no mention of CSRF. Phase 2 security section mentions `httpOnly` cookies but not SameSite or CSRF mitigations for state-changing proxy calls.
- **Suggested fix:** Document that the proxy route must verify `Origin`/`Referer` header matches expected origin, or use NextAuth's built-in CSRF token (available via `getCsrfToken()`). For mutations, require the CSRF token in request headers. Alternatively, rely on `SameSite=Lax` cookies (NextAuth default) and document this as the chosen mitigation.

---

## Unresolved Questions

1. What is the Keycloak token claim path for patient role — the plan lists this as unknown but does not assign an owner or pre-implementation verification task.
2. Is the YARP gateway already configured to forward `Authorization` headers to all backend services, or does each route need explicit configuration? The plan assumes it works but does not verify.
3. What is the exact REST path structure for each backend service through the gateway? Phase 3 types and hooks assume endpoint shapes (`/api/proxy/appointments`, `/api/proxy/medical-records/:patientId`) without a single reference to verified gateway routing configuration.
