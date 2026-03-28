# Plan Failure Mode Analysis: Next.js Patient Client App

**Plan:** `plans/260318-1719-nextjs-patient-client-app/`
**Reviewer role:** Failure Mode Analyst
**Date:** 2026-03-18

---

## Finding 1: NEXTAUTH_URL Docker/Container Split-Brain Breaks OAuth Callback

- **Severity:** Critical
- **Location:** Phase 1, "Implementation Steps" step 9 (docker-compose snippet)
- **Flaw:** `NEXTAUTH_URL` is set to `http://localhost:3100` inside the container, but Keycloak's redirect URI must resolve from the browser. Inside Docker, the container address is not `localhost` to Keycloak, yet the plan also sets `KEYCLOAK_ISSUER=http://keycloak:8080` (internal hostname). This creates a three-way mismatch: Keycloak validates the redirect against its registered URIs, NextAuth validates the incoming callback against `NEXTAUTH_URL`, and the browser navigates to `localhost:3100`. If they disagree even by trailing slash, Keycloak rejects the callback with `invalid_redirect_uri`.
- **Failure scenario:** Deploy with docker-compose, user clicks "Sign In", browser lands on Keycloak login, submits credentials. Keycloak checks the callback URL `http://localhost:3100/api/auth/callback/keycloak` against allowed redirect URIs. If the YARP gateway or Keycloak itself is addressed by Docker internal hostname, the redirect back to the browser fails with a 400 from Keycloak. No login is possible.
- **Evidence:** Phase 1 sets `NEXTAUTH_URL=http://localhost:3100` in the docker-compose `environment` block, while Phase 2 documents a known risk "Keycloak issuer URL differs in Docker vs host" but only says "use env vars" without providing the actual split configuration values needed.
- **Suggested fix:** Document two explicit env profiles: dev-host (NEXTAUTH_URL=http://localhost:3100, KEYCLOAK_ISSUER=http://localhost:8080/realms/hospital) and docker (NEXTAUTH_URL=http://localhost:3100 stays for browser, KEYCLOAK_ISSUER=http://keycloak:8080/realms/hospital for server-side token exchange). State this split explicitly and add a config validation step before Phase 2 testing proceeds.

---

## Finding 2: Token Refresh Race Condition on Concurrent Requests

- **Severity:** Critical
- **Location:** Phase 2, "Architecture" section — Token Refresh block
- **Flaw:** The plan describes token refresh in the JWT callback: "JWT callback checks expiresAt on every request — if expired → call Keycloak token endpoint." NextAuth v5 JWT callbacks are called per-request. If multiple server components (Dashboard page fetches appointments, records, and prescription count in parallel — see Phase 4 Step 4) fire simultaneously with an expired token, each callback will independently detect expiry and independently call Keycloak's token endpoint with the same refresh_token. Keycloak refresh tokens are single-use. The first call succeeds; every subsequent call receives `invalid_grant` and sets the `error` flag — logging the user out mid-render.
- **Failure scenario:** User's access token expires after 5 minutes. Dashboard page loads — server makes 3 parallel data calls. All three JWT callbacks run concurrently, all see `expiresAt` in the past, all call Keycloak simultaneously with the same refresh_token. First request gets new tokens; second and third get `invalid_grant`. NextAuth sets error state. User is kicked to login mid-session despite valid credentials.
- **Evidence:** Phase 4 Step 4 explicitly states: "Fetch: upcoming appointments (limit 5), recent records (limit 3), prescription count" as a single server component — these are concurrent calls. Phase 2 auth-refresh.ts is described as a simple function with no locking mechanism.
- **Suggested fix:** Add a mutex / in-memory token refresh lock (e.g., a `Map<userId, Promise>` keyed by sub claim) in `lib/auth-refresh.ts` so concurrent callbacks for the same session reuse the in-flight refresh promise rather than firing multiple Keycloak calls.

---

## Finding 3: API Proxy Route Has No Authorization Check — IDOR Risk

- **Severity:** Critical
- **Location:** Phase 3, "Implementation Steps" step 3 — API proxy route
- **Flaw:** The plan describes the `/api/proxy/[...path]` catch-all route as: "Gets server session, injects Bearer token, forwards to Gateway API." The plan does not specify any path validation or allowlist. A patient can craft a request to `/api/proxy/admin/users` or `/api/proxy/patients/other-patient-id` and the proxy will blindly forward it with a valid Bearer token. Authorization is described as "enforced by backend + JWT patientId" in Phase 4 Security Considerations — but there is no plan to verify the backend actually enforces this for all endpoints, and the proxy itself adds zero resistance.
- **Failure scenario:** Patient A is authenticated. They send `GET /api/proxy/patients/patient-B-id`. The proxy forwards this with Patient A's valid Bearer token. If the patient microservice does not enforce ownership at the record level (or if a bug exists), Patient A reads Patient B's medical records. The proxy provides no additional barrier.
- **Evidence:** Phase 3 proxy description: "Catches all /api/proxy/* requests... Forwards to Gateway API... returns response." Phase 4 security note: "Patient can only view their own data (enforced by backend + JWT patientId)" — this is an assumption, not a verified constraint.
- **Suggested fix:** Add an explicit path allowlist in the proxy (only permit known safe prefixes: `/appointments`, `/medical-records`, `/prescriptions`, `/patients`). Additionally, for patient-scoped routes, extract the patientId from the JWT and validate it matches the path parameter before proxying. Document this as a contract, not an assumption.

---

## Finding 4: Dockerfile Copies node_modules from `deps` Stage but Runs App from `builder` — Silent Runtime Failure

- **Severity:** High
- **Location:** Phase 1, "Implementation Steps" step 8 — Dockerfile
- **Flaw:** The multi-stage Dockerfile copies `node_modules` from the `deps` stage (which ran `npm ci --only=production`) into the `runner` stage, but copies `.next` from the `builder` stage. Next.js standalone output (required for minimal Docker images) needs to be explicitly enabled in `next.config.ts` via `output: 'standalone'`. Without `output: 'standalone'`, the `.next` folder alone is not runnable — it requires the full `node_modules` and `next` binary. The plan never sets `output: 'standalone'` in `next.config.ts`.
- **Failure scenario:** `docker build` succeeds. `docker run` executes `npm start`. npm resolves to `next start` which tries to serve the app, but the Next.js server requires server-side code at `.next/server/` which references modules. Without standalone output, chunks are missing. Container starts, health check passes on root `/` returning an HTML shell, but any SSR page that imports a library crashes at runtime with module-not-found errors.
- **Evidence:** Phase 1 Dockerfile runner stage: `COPY --from=deps /app/node_modules ./node_modules` + `COPY --from=builder /app/.next ./.next`. No `COPY --from=builder /app/next.config.ts` or `output: 'standalone'` configuration anywhere in the plan. Phase 1 success criteria checks image size <250MB but does not verify that all pages render in the container.
- **Suggested fix:** Add `output: 'standalone'` to `next.config.ts` and update the Dockerfile runner stage to use `COPY --from=builder /app/.next/standalone ./` + `COPY --from=builder /app/.next/static ./.next/static` + `CMD ["node", "server.js"]`. This is the canonical Next.js Docker pattern.

---

## Finding 5: No Patient ID Resolution Strategy — Blocks All Data Fetching

- **Severity:** High
- **Location:** Phase 4, "Risk Assessment" — "Patient ID not in session" row
- **Flaw:** The plan acknowledges "Patient ID not in session" as a medium-likelihood, high-impact risk but defers resolution to "Extract from JWT sub claim or fetch profile on login." This is the linchpin of the entire data layer: every API call is patient-scoped (`/api/proxy/medical-records/:patientId`, `useMedicalRecords(patientId)`, `usePatientProfile()`). If the backend patient service uses its own UUID (not the Keycloak `sub` claim), all data fetching is broken on first run.
- **Failure scenario:** Patient logs in. JWT `sub` is a Keycloak UUID like `f47ac10b-58cc...`. Patient microservice stores its own `id` as an auto-increment integer or separate UUID. `callGatewayAPI('/patients/f47ac10b-58cc...')` returns 404. Dashboard renders empty or crashes. No appointments, no records, no prescriptions are loadable. The app is non-functional until this mapping is resolved.
- **Evidence:** Phase 3 hook: `usePatientProfile()` — `GET /api/proxy/patients/:id`. Phase 3 type: `Patient { id: string }` — the plan does not specify whether this `id` is the Keycloak sub or an internal ID. Phase 4 risk row defers resolution without a concrete plan.
- **Suggested fix:** Before Phase 3 implementation, inspect the patient microservice API and confirm whether it accepts Keycloak `sub` as the patient lookup key or requires a separate call to `/patients/me` using the Bearer token. Document the chosen resolution in Phase 3 and update all hooks and API client calls accordingly. This must be resolved before Phase 4 starts, not treated as a runtime risk.

---

## Finding 6: Token Stored in JWT Cookie — No Revocation Path on Logout

- **Severity:** High
- **Location:** Phase 2, "Requirements" functional section + Security Considerations
- **Flaw:** The plan uses JWT strategy with 24h `maxAge`. On logout, `signOut()` clears the NextAuth cookie client-side, but the Keycloak access token and refresh token remain valid until their own expiry. There is no step to call Keycloak's `end_session_endpoint` or revoke the refresh token. If the session cookie is stolen (XSS, network interception), the attacker can use the access token for up to the token's remaining lifetime after the user "logs out."
- **Failure scenario:** Patient logs out via the logout button. `signOut({ callbackUrl: '/login' })` is called. The Next.js session cookie is cleared. However, the Keycloak access token (typically 5 minutes) and refresh token (typically 30 minutes to hours, depending on realm config) remain valid. Any party who captured the token before logout can continue making authenticated API calls to the Gateway.
- **Evidence:** Phase 2 step 11: "logout button component — calls `signOut({ callbackUrl: '/login' })`." No mention of Keycloak `end_session_endpoint` call or token revocation in any phase. Security Considerations do not mention logout token invalidation.
- **Suggested fix:** Add Keycloak OIDC `end_session_endpoint` call on logout using the `idToken` stored in the JWT session. NextAuth v5 Keycloak provider supports this via the `events.signOut` callback or a custom `signOut` action that calls `https://keycloak/realms/hospital/protocol/openid-connect/logout` with `id_token_hint`.

---

## Finding 7: `NEXT_PUBLIC_GATEWAY_URL` Leaks Internal Network Topology

- **Severity:** High
- **Location:** Phase 1, "Implementation Steps" step 9 (docker-compose environment) + step 7 (.env.example)
- **Flaw:** `NEXT_PUBLIC_GATEWAY_URL=http://localhost:8000` is a public env var (NEXT_PUBLIC_ prefix), meaning it is embedded in the client-side JavaScript bundle and visible to any user who inspects the page source. The plan also defines `GATEWAY_API_URL=http://hospital-gateway:8000` as the server-side URL. A patient's browser will see the internal gateway hostname and port. While the proxy pattern in Phase 3 is meant to hide this, the public env var defeats that by broadcasting the gateway address.
- **Failure scenario:** The plan lists "API calls through Gateway (port 8000) — never call services directly" as a key architecture decision. If `NEXT_PUBLIC_GATEWAY_URL` is referenced anywhere in client-side code (easy mistake during development), browser requests bypass the proxy, expose the gateway URL, and may also bypass CORS restrictions. Even if no client code uses it, the value appears in the JS bundle and can be read by any patient.
- **Evidence:** Phase 1 `.env.example`: `NEXT_PUBLIC_GATEWAY_URL=http://localhost:8000`. Phase 3 API proxy is described as hiding the gateway URL from the browser — but `NEXT_PUBLIC_GATEWAY_URL` undoes this by design.
- **Suggested fix:** Remove `NEXT_PUBLIC_GATEWAY_URL` entirely. Client-side code should only call `/api/proxy/*` (relative URL). Only `GATEWAY_API_URL` (server-only, no NEXT_PUBLIC_ prefix) should exist for server-side use. Add a lint rule or build check to ensure no `NEXT_PUBLIC_GATEWAY_URL` references exist.

---

## Finding 8: Smoke Test Does Not Verify Auth or Data — Passes on Broken App

- **Severity:** Medium
- **Location:** Phase 6, "Implementation Steps" step 9 — smoke test script
- **Flaw:** The smoke test script only checks HTTP status codes and prints them without asserting correctness. Line `echo "Dashboard status: $STATUS"` prints the status but does not `exit 1` on unexpected values. Line `echo "API proxy status: $STATUS"` similarly prints without asserting. Only the root health check (`curl -f`) actually fails the script on error. The script can print "Dashboard status: 500" and still exit 0.
- **Failure scenario:** A broken build deploys. Root `/` returns 200 (just an HTML redirect page). Dashboard `/dashboard` returns 500 (SSR crash). API proxy `/api/proxy/patients` returns 500 (auth config broken). The smoke test prints both status codes and exits 0. CI/CD marks the deployment green. Patients land on a broken app.
- **Evidence:** Phase 6 smoke script steps 9: `STATUS=$(curl -s -o /dev/null -w "%{http_code}" http://localhost:3100/dashboard)` followed by `echo "Dashboard status: $STATUS"` — no `[ "$STATUS" -eq 307 ] || exit 1` assertion.
- **Suggested fix:** Add explicit assertions: `[[ "$STATUS" == "307" || "$STATUS" == "302" ]] || { echo "FAIL: Dashboard did not redirect"; exit 1; }` for each check. Also add a test that `/api/proxy/patients` with no auth cookie returns exactly 401.

---

## Finding 9: 60% Test Coverage Target Excludes Middleware and Auth — The Highest Risk Code Goes Untested

- **Severity:** Medium
- **Location:** Phase 6, "Requirements" non-functional + "Implementation Steps" steps 2-5
- **Flaw:** The plan targets ">60% coverage for lib/ and components/" but lists no tests for `middleware.ts` (route protection), `lib/auth-config.ts` (token callbacks), or `lib/auth-refresh.ts` (token refresh logic). These are the three most critical failure points: a misconfigured middleware regex could expose all routes, a broken JWT callback silently stores null tokens, and a buggy refresh function causes mass logouts. They are conspicuously absent from the test file list.
- **Failure scenario:** The middleware `matcher` regex is slightly wrong (e.g., accidentally excludes `(dashboard)` routes). All pages are publicly accessible. Since middleware is not tested, this ships undetected. 60% coverage is met by covering Zod schemas and UI components.
- **Evidence:** Phase 6 test files listed: `api-client.test.ts`, `auth-config.test.ts` (present), `appointment-schema.test.ts`, `patient-schema.test.ts`, `appointment-card.test.tsx`, `stats-cards.test.tsx`, `use-appointments.test.ts`. No `middleware.test.ts` or `auth-refresh.test.ts` in the list. Phase 2 step 7 middleware is complex with role checks and matcher patterns.
- **Suggested fix:** Add `__tests__/middleware.test.ts` (verify protected routes redirect, public routes pass through, matcher patterns are correct) and `__tests__/lib/auth-refresh.test.ts` (verify refresh logic, concurrent call handling, error state on invalid_grant) to the mandatory test list. Raise coverage requirement for `lib/` to 80%.

---

## Unresolved Questions

1. Is the patient microservice ID the Keycloak `sub` claim or a separate internal ID? (Blocks all data fetching — must be answered before Phase 3 implementation.)
2. Does YARP gateway validate Bearer tokens itself, or does it pass them through to downstream services raw? (Affects whether a stolen access token can bypass the patient portal entirely.)
3. What is the Keycloak access token lifetime in the "hospital" realm? (Determines how long a logged-out user's token remains valid and how often the refresh race condition can trigger.)
4. Is there a `/patients/me` endpoint that resolves the current user from the Bearer token without requiring a patient ID in the URL path?
