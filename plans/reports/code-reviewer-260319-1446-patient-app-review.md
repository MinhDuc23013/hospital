# Code Review — Patient App (client/patient-app/)
Date: 2026-03-19 | Reviewer: code-reviewer agent

---

## Scope
- Files: auth-config.ts, auth-refresh.ts, auth-session.ts, middleware.ts, proxy route, token route, api-client.ts, all hooks, dashboard layout/page, login page, logout-button.tsx, next.config.js, Dockerfile
- LOC: ~550 (excluding node_modules)
- Focus: security, correctness, YAGNI/KISS

## Overall Assessment
Solid, well-structured implementation. Auth layer correctly separates server-side token handling from client session. Proxy pattern is properly enforced. Most security best practices are applied. A few real issues need attention before production.

---

## Score: 7.5/10

---

## Critical Issues

### C1 — `NEXT_PUBLIC_KEYCLOAK_ISSUER` is undefined — logout silently falls back (logout-button.tsx:24)
`process.env.NEXT_PUBLIC_KEYCLOAK_ISSUER` is read in the client component but this variable is **not in `.env.example`** and is never set anywhere. The variable `KEYCLOAK_ISSUER` is server-only (no `NEXT_PUBLIC_` prefix). Result: `keycloakIssuer` is always `undefined`, the Keycloak `end_session` branch never executes, and the fallback `signOut({ callbackUrl: "/login" })` is always used. This means **Keycloak SSO session is never revoked** — the user's SSO session remains active after logout, allowing silent re-login on any Keycloak-integrated app.

Fix options:
1. Add `NEXT_PUBLIC_KEYCLOAK_ISSUER=...` to `.env.example` and document it (exposes issuer URL to browser, which is acceptable for public OIDC providers).
2. Or let the `/api/auth/token` endpoint also return the issuer, and drop the `NEXT_PUBLIC_` var entirely.

### C2 — Proxy response: `upstream.json()` throws on non-JSON responses (proxy route.ts:59)
```ts
const data = await upstream.json();
return NextResponse.json(data, { status: upstream.status });
```
If the API Gateway returns a non-JSON body (network error, HTML gateway error, 502/504), `upstream.json()` throws an unhandled exception. This surfaces as a 500 with no meaningful error to the client, and can crash the route handler context.

Fix: wrap in try/catch, fall back to `{ error: "Upstream error" }` with the upstream status.

---

## High Issues

### H1 — `session.error` is never handled in the UI
`auth-config.ts` sets `session.error = "RefreshTokenError"` when token refresh fails. `next-auth.d.ts` types it correctly. But no page or layout reads `session.error` to force re-login. Users with an expired refresh token will silently get stale/failed data fetches rather than a redirect to `/login`.

Fix: In `DashboardLayout`, after `getAuthSession()`, check `(session as any).error === "RefreshTokenError"` and `redirect("/login")`.

### H2 — `DashboardPage` passes `patientId` in query params — IDOR risk (page.tsx:16-18)
```ts
callGatewayAPI(`/api/appointments?patientId=${patientId}&limit=5`)
callGatewayAPI(`/api/prescriptions?patientId=${patientId}`)
```
The gateway receives `patientId` from a query param injected by the client-side session. If the API Gateway does NOT independently validate that the Bearer token's `sub` matches the requested `patientId`, a compromised/modified session could request another patient's data. This is a layered concern — the gateway should enforce this — but the client should not pass `patientId` at all if the gateway can derive it from the Bearer token `sub`. This is also inconsistent with how `usePrescriptions` (server-side proxy) works.

Fix: Confirm gateway enforces `sub == patientId`; ideally drop the param and let gateway extract from token.

### H3 — `useCancelAppointment` uses DELETE with a JSON body (use-appointments.ts:77-82)
```ts
method: "DELETE",
headers: { "Content-Type": "application/json" },
body: JSON.stringify({ reason }),
```
HTTP DELETE with a body is technically valid (RFC 7230) but many proxies, load balancers, and servers strip or reject it. The allowlist regex `/^appointments(\/[^/]+)?(\/cancel)?$/` permits `appointments/{id}/cancel` — if the gateway expects a `POST /appointments/{id}/cancel` pattern this will silently fail. Verify the gateway contract.

---

## Medium Issues

### M1 — In-process refresh lock is per-process only (auth-refresh.ts:65)
`refreshLocks` is a module-level `Map`. In a multi-replica or serverless (multiple lambda warm instances) deployment this provides no cross-process protection. Keycloak single-use refresh tokens can still be raced across Node processes. This is acceptable for single-process Docker deployments but is a documented limitation that should be noted in a comment for ops.

### M2 — `idToken` leak surface in `/api/auth/token` endpoint (token/route.ts)
The endpoint returns `idToken` to any authenticated session caller (no CSRF token, no `SameSite` check beyond cookie). While the `idToken` is not an access credential, a CSRF attack or subdomain XSS could exfiltrate it. The response lacks `Cache-Control: no-store` header, so it could be cached.

Fix: Add `Cache-Control: no-store` to the response headers.

### M3 — Middleware excludes `/api/auth` but not `/api/auth/token` (middleware.ts:10)
The matcher `/((?!api/auth|...).*)/` excludes **all** `/api/auth/*` routes from auth check, which includes `/api/auth/token`. This is correct (NextAuth handles its own auth), but `/api/auth/token` itself checks `auth()` internally — so it's safe. Minor: the comment should clarify this endpoint is self-protected.

### M4 — `searchParams` type in login page is non-async (login/page.tsx:11)
In Next.js 15, `searchParams` in page components becomes a Promise. While the app appears to use Next.js 14, this is a forward-compat concern worth noting. The `LoginPageProps` interface types it as a plain object, not `Promise<{...}>`.

### M5 — Proxy allowlist: `medical-records` pattern ambiguity (proxy route.ts:12)
`/^medical-records(\/[^/]+)?$/` matches both `medical-records/{patientId}` and `medical-records/{recordId}`. The hook uses the same path shape for both listing (by patientId) and detail (by recordId) — which is by design but means the proxy pattern doesn't distinguish. This is fine as long as the gateway differentiates correctly. Low risk, but worth documenting.

### M6 — CSP includes `unsafe-eval` — should be removed for production (next.config.js:27)
`script-src 'self' 'unsafe-inline' 'unsafe-eval'` — `unsafe-eval` is overly permissive. Next.js does not require `unsafe-eval` in production builds. Remove for production, gate behind `NODE_ENV !== 'production'` if needed only for dev.

---

## Low Issues

### L1 — Dockerfile uses `node:18-alpine` — consider pinning to a digest
`node:18-alpine` is a floating tag. Pin to a specific digest or at minimum `node:18.20-alpine` for reproducible builds.

### L2 — `useMedicalRecords` and `useMedicalRecord` share the same path shape
Both `fetchMedicalRecords(patientId)` and `fetchMedicalRecord(id)` call `/api/proxy/medical-records/{id}`. This works if the backend differentiates by whether the ID is a patient UUID vs record UUID, but the hook naming creates confusion. No bug, just a readability concern.

### L3 — `useUpdateProfile` fires with empty `patientId` until session loads
If `patientId` is `""` when mutation fires (session not yet loaded), it calls `PUT /api/proxy/patients/` — which the proxy allowlist `/^patients\/[^/]+$/` would reject (empty segment). The `enabled: !!patientId` guard exists on the query but not on the mutation. The mutation itself has no guard. In practice this won't fire without user interaction, but it's worth a runtime guard.

---

## Edge Cases Found

- **Concurrent tab logout**: If user has two tabs and logs out in one, the second tab's `LogoutButton` will still attempt to fetch `/api/auth/token` which returns `{ idToken: null }` (session gone), falling through to `signOut({ callbackUrl: "/login" })`. Benign but will not revoke Keycloak session from the second tab.
- **Refresh lock with `token.sub = undefined`**: `refreshAccessTokenSafe` uses `token.sub ?? "default"` as the lock key. If `sub` is missing, all such tokens share one lock, which could cause a race. Should be treated as an error.
- **`upstream.json()` on empty body**: Some 204 No Content responses from the gateway will cause `upstream.json()` to throw. DELETE cancel endpoint likely returns 204.

---

## Positive Observations
- Access token correctly kept server-side only; client session never exposes it.
- Proxy allowlist is explicit and restrictive — good defense-in-depth.
- Refresh lock pattern correctly handles Keycloak single-use token constraint.
- Custom error types (`AuthError`, `GatewayError`) enable structured handling.
- Security headers in `next.config.js` are well-considered.
- Dockerfile correctly uses non-root user, multi-stage build, standalone output.
- Validators use Zod with appropriate constraints.
- No secrets exposed; `.env.example` is safe and well-documented.
- Login page correctly avoids open-redirect by not forwarding `callbackUrl` from query params.

---

## Recommended Actions (Priority Order)

1. **[Critical]** Add `NEXT_PUBLIC_KEYCLOAK_ISSUER` to `.env.example` and document it, OR restructure logout to not need it — Keycloak SSO session currently never revoked.
2. **[Critical]** Wrap `upstream.json()` in proxy route in try/catch to handle non-JSON/empty upstream responses (including 204).
3. **[High]** Handle `session.error === "RefreshTokenError"` in `DashboardLayout` — redirect to `/login`.
4. **[High]** Confirm/document that API Gateway validates `sub == patientId`; consider dropping client-passed `patientId` query params.
5. **[Medium]** Add `Cache-Control: no-store` to `/api/auth/token` response.
6. **[Medium]** Remove `unsafe-eval` from CSP in production (`NODE_ENV === 'production'`).
7. **[Low]** Pin `node:18-alpine` to a specific version tag in Dockerfile.
8. **[Low]** Add guard in `useUpdateProfile` to prevent mutation with empty `patientId`.

---

## Recommendation: **CONDITIONAL PASS**

The implementation is architecturally sound and passes on correctness, type safety, and most security concerns. However, C1 (Keycloak SSO session not revoked on logout) and C2 (proxy crash on non-JSON response) must be resolved before production deployment. H1 (silent token expiry handling) should also be resolved.

---

## Unresolved Questions

1. Does the API Gateway independently validate that the Bearer token `sub` matches the `patientId` query param? (affects H2 severity)
2. Does `DELETE /appointments/{id}` return 204 No Content or a JSON body? (affects C2 urgency for that path)
3. Is `NEXT_PUBLIC_KEYCLOAK_ISSUER` intentionally omitted (i.e., was Keycloak logout deprioritized) or is it a gap?
4. Are there plans to scale patient-app to multiple replicas? If so, M1 (in-process refresh lock) needs a distributed lock or Keycloak session refresh endpoint that tolerates concurrent calls.
