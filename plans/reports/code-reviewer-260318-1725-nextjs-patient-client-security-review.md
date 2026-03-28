# Security Adversary Review: Next.js Patient Client App
**Plan:** `plans/260318-1719-nextjs-patient-client-app/`
**Reviewer role:** Hostile security adversary
**Date:** 2026-03-18

---

## Finding 1: Insecure Default Secret with Fallback in docker-compose

- **Severity:** Critical
- **Location:** Phase 1, section "Add to docker-compose.yml"
- **Flaw:** `NEXTAUTH_SECRET` has a hardcoded fallback value `change_me_in_production`. Docker Compose will silently use this default if the env var is not set, leaving the NextAuth JWT signing secret insecure in any environment where the operator forgets to set it.
- **Failure scenario:** Developer runs `docker-compose up` without setting `NEXTAUTH_SECRET` in `.env` or shell. The container starts with `NEXTAUTH_SECRET=change_me_in_production`. An attacker knowing this public default (it is in the plan and will be in the committed `docker-compose.yml`) can forge valid NextAuth session JWTs — granting themselves any user's session, including bypassing the Keycloak OIDC flow entirely.
- **Evidence:** `- NEXTAUTH_SECRET=${NEXTAUTH_SECRET:-change_me_in_production}`
- **Suggested fix:** Remove the fallback entirely (`${NEXTAUTH_SECRET}` with no default). Add a startup assertion in `lib/auth-config.ts` that throws if `NEXTAUTH_SECRET` is missing or fewer than 32 bytes. Mark it as required in `.env.example` with an explicit comment.

---

## Finding 2: Open API Proxy — No Path Validation or Authorization Scope Check

- **Severity:** Critical
- **Location:** Phase 3, section "Create API proxy route"
- **Flaw:** The wildcard proxy `app/api/proxy/[...path]/route.ts` forwards ALL paths to the Gateway after injecting a Bearer token. There is no allowlist of permitted paths, no method restriction, and no check that the requested resource belongs to the authenticated patient. Any authenticated patient can craft a request to `/api/proxy/admin/users` or `/api/proxy/patients/<other-patient-id>` and the proxy will forward it with their valid Bearer token.
- **Failure scenario:** Patient A is authenticated. They send `GET /api/proxy/patients/patient-B-id`. The proxy injects Patient A's JWT and forwards to `GET http://hospital-gateway:8000/patients/patient-B-id`. If the Gateway enforces authorization by patient ID in the token, this is stopped there — but the plan states authorization is "enforced by backend + JWT patientId" without verifying this is actually implemented. If the Gateway trusts the Bearer token broadly (e.g., role=patient is sufficient), Patient A reads Patient B's complete medical record.
- **Evidence:** `API Proxy Pattern: Client → /api/proxy/appointments → server gets token → calls Gateway → returns data` — no mention of path allowlisting or ownership verification.
- **Suggested fix:** Implement a strict path allowlist in the proxy (only permit known resource prefixes). For patient-owned resources, extract the `sub` claim from the JWT and enforce that the requested `patientId` matches before forwarding.

---

## Finding 3: accessToken Exposed in Client-Side Session Object

- **Severity:** High
- **Location:** Phase 2, section "Create NextAuth config" (step 3) and "Create NextAuth type augmentation" (step 1)
- **Flaw:** The plan explicitly exposes `accessToken` in the NextAuth `Session` callback so client components can consume it. The Session callback's output is serialized and sent to the browser via the `useSession()` hook. This means the raw Keycloak access token (a full JWT with scope, roles, patient ID) is accessible in browser JavaScript.
- **Failure scenario:** Any XSS vulnerability — in a third-party shadcn component, in a toast message rendering unsanitized user input, or in a future dependency — can steal the `accessToken` from `session.accessToken`. With the token, the attacker can make direct calls to the API Gateway from anywhere until the token expires (Keycloak default: 5 minutes, but refresh token validity is much longer and is also stored in the JWT).
- **Evidence:** "Session callback: expose accessToken and roles" and `Extend Session with accessToken: string, user.roles: string[]` in `types/next-auth.d.ts`.
- **Suggested fix:** Do not expose `accessToken` in the Session object for client-side use. All authenticated API calls should go through the server-side proxy (`/api/proxy/*`), which retrieves the token from the server-only JWT. If client components truly need to call authenticated APIs, they must use the proxy — the plan already supports this pattern, so there is no legitimate reason to expose the token to the browser.

---

## Finding 4: Refresh Token Stored in Client-Accessible JWT Cookie

- **Severity:** High
- **Location:** Phase 2, section "Create token refresh utility" and "Create NextAuth config"
- **Flaw:** NextAuth's JWT strategy stores the entire JWT (including `refreshToken`) in a signed cookie. While HttpOnly prevents JS access, the plan stores `refreshToken` in the JWT callback (`JWT extended with refreshToken`). If the cookie is stolen via network interception (HTTP in dev) or a subdomain cookie-theft attack, the attacker possesses a long-lived Keycloak refresh token that can generate new access tokens indefinitely until the Keycloak session is revoked.
- **Failure scenario:** The dev environment runs over plain HTTP (`http://localhost:3100`). The cookie has `Secure=false` in dev. A developer's machine on a shared Wi-Fi has this cookie sniffed. The attacker uses the refresh token to silently obtain access tokens for the patient's account for days (Keycloak SSO session default: 10 hours, offline sessions: 30 days).
- **Evidence:** `JWT callback: store access_token, refresh_token, roles` — refresh_token is placed in the JWT payload. Dev NEXTAUTH_URL is `http://` not `https://`.
- **Suggested fix:** Set `NEXTAUTH_URL` to HTTPS even in development (use mkcert for local TLS). Ensure `useSecureCookies: true` in NextAuth config. Consider setting `maxAge` on the session to a short value (e.g., 1 hour) to limit refresh token lifetime in the cookie.

---

## Finding 5: No CSRF Protection on API Proxy Mutation Endpoints

- **Severity:** High
- **Location:** Phase 3, section "Create API proxy route" (POST/DELETE mutations)
- **Flaw:** The plan describes proxy mutations (schedule appointment via POST, cancel via DELETE) going through `/api/proxy/[...path]`. The plan does not specify CSRF token validation on these mutation routes. NextAuth provides CSRF protection for its own routes but not for custom API routes.
- **Failure scenario:** A patient visits a malicious website while logged in. The malicious page sends a cross-origin form POST to `https://patient-portal.hospital.com/api/proxy/appointments` (browser includes the HttpOnly session cookie automatically). The proxy authenticates the request from the cookie, injects the Bearer token, and forwards the appointment cancellation to the Gateway. Patient's appointments are cancelled without their knowledge.
- **Evidence:** The proxy route plan (`app/api/proxy/[...path]/route.ts`) has no mention of CSRF token validation, `SameSite` cookie policy verification, or `Origin` header checks.
- **Suggested fix:** Set `SameSite=Strict` on NextAuth cookies (configure `cookies` in NextAuth options). Add an `Origin` header check in the proxy route handler, rejecting requests from unexpected origins. For mutating operations, require a custom header (e.g., `X-Requested-With: XMLHttpRequest`) that browser cross-origin form submissions cannot set.

---

## Finding 6: `callbackUrl` Parameter Open Redirect on Login

- **Severity:** High
- **Location:** Phase 2, section "Create login page" (step 8)
- **Flaw:** The login page calls `signIn("keycloak", { callbackUrl: "/dashboard" })`. However, the plan does not mandate validating the `callbackUrl` query parameter that will arrive when users are redirected to `/login?callbackUrl=...` from the middleware. NextAuth v5 has some built-in protections, but they can be bypassed with certain URL encodings. If `callbackUrl` is not restricted to same-origin paths, any link of the form `/login?callbackUrl=https://evil.com` can redirect a freshly-authenticated patient to a phishing site.
- **Failure scenario:** Attacker sends a patient the link: `http://patient-portal.hospital.com/login?callbackUrl=https%3A%2F%2Fphishing-hospital.com%2Fsteal`. Patient logs in. NextAuth redirects to the attacker's site post-login. Attacker captures any data the patient submits there, or harvests credentials for other systems.
- **Evidence:** No `callbackUrl` allowlist or validation step mentioned anywhere in the auth phase. `signIn("keycloak", { callbackUrl: "/dashboard" })` is only the hardcoded path — it does not prevent manipulation of the `callbackUrl` param passed in the URL by the middleware redirect.
- **Suggested fix:** Explicitly validate `callbackUrl` in the login page and the NextAuth config to only allow relative paths starting with `/` and matching an allowlist of known routes. In NextAuth config, set `pages.signIn` and validate that `callbackUrl` never contains a protocol or external host.

---

## Finding 7: Patient ID Derivation is Undefined — IDOR Risk

- **Severity:** High
- **Location:** Phase 4, "Risk Assessment" table and Phase 3, hooks (`useMedicalRecords(patientId)`, `usePatientProfile()`)
- **Flaw:** The plan acknowledges "Patient ID not in session" as a risk, with mitigation "Extract from JWT sub claim or fetch profile on login." This is deferred and unresolved. Multiple API hooks accept `patientId` as a parameter from the calling component. If the client passes `patientId` from URL params or component state rather than from the server-verified JWT `sub` claim, any patient can substitute another patient's ID.
- **Failure scenario:** The hook `useMedicalRecords(patientId)` is called in a page that reads `patientId` from route params `app/(dashboard)/medical-records/[id]/page.tsx`. If the page is `medical-records/patient-B-id`, Patient A's browser (authenticated) calls `/api/proxy/medical-records/patient-B-id`. The proxy does not validate ownership (Finding 2). Patient B's complete medical history is returned to Patient A.
- **Evidence:** `useMedicalRecords(patientId)` — no plan-level spec requiring patientId to always be sourced from the server-side JWT `sub` claim. Risk table item: "Patient ID not in session — Extract from JWT sub claim or fetch profile on login."
- **Suggested fix:** The patient's own ID must be extracted exclusively from the server-side JWT `sub` claim in `getAuthSession()` and injected server-side. No client component should ever accept or pass `patientId` as a prop derived from URL params. The API hooks must source `patientId` only from the server-validated session.

---

## Finding 8: Sensitive PHI in Error Messages Surfaced to Client

- **Severity:** Medium
- **Location:** Phase 2, section "Create auth error page" and Phase 3, section "Error handling: 401 throw AuthError, 4xx/5xx throw ApiError"
- **Flaw:** The error page displays `error message from searchParams` directly. The `ApiError` type includes `correlationId`, `message`, and `status` — potentially containing backend stack traces, internal service names, or PHI from response bodies that are forwarded through the error chain.
- **Failure scenario:** A backend service returns a 500 error with a message like `"Patient record 'f47ac10b' not found in mongodb://hospital-db:27017/hrm"`. The API client wraps this in `ApiError.message` and throws. The error boundary catches it and renders the raw message in the error page. The patient (or an attacker monitoring error pages) sees internal hostnames and database identifiers.
- **Evidence:** "Display error message from searchParams" (auth error page). `ApiError: { message: string; status: number; timestamp: string; correlationId? }` — no sanitization step specified.
- **Suggested fix:** The API client must sanitize error messages before surfacing them: map backend error codes to user-friendly strings, never forward raw backend messages to the UI. Correlate errors via `correlationId` logged server-side only.

---

## Finding 9: Docker Image Built Without Non-Root User for Next.js Standalone Output

- **Severity:** Medium
- **Location:** Phase 1, section "Create Dockerfile (multi-stage)"
- **Flaw:** The Dockerfile creates a `nextjs` user but copies `node_modules` and `.next` (standard output) rather than using Next.js `output: 'standalone'` mode. Without standalone mode the image includes the full `node_modules` (~200MB+). More critically, the `COPY --from=deps` stage copies `node_modules` owned by root before `USER nextjs` is set, leaving files owned by root. If Next.js or a dependency writes to the filesystem at runtime, it may escalate to run as root due to file ownership issues.
- **Failure scenario:** A `node_modules/.cache` or runtime write by Next.js fails because the `nextjs` user lacks write permission on root-owned `node_modules`. The process falls back to running as root to resolve the permission error (depending on how the base image handles this). A container escape or RCE in a Node.js dependency then runs as root inside the container.
- **Evidence:** `COPY --from=deps /app/node_modules ./node_modules` before `RUN addgroup/adduser` and `USER nextjs`. No `chown` directive. No `output: 'standalone'` in next.config.ts.
- **Suggested fix:** Add `RUN chown -R nextjs:nodejs /app` after all COPY steps and before `USER nextjs`. Use `output: 'standalone'` in `next.config.ts` to reduce image size and eliminate `node_modules` from the runner stage.

---

## Finding 10: No Content Security Policy Defined

- **Severity:** Medium
- **Location:** Phase 1 (`next.config.ts` setup) and Phase 5 (UI Polish — no mention of HTTP security headers)
- **Flaw:** No Content Security Policy (CSP), `X-Frame-Options`, `X-Content-Type-Options`, or `Referrer-Policy` headers are planned anywhere across all 6 phases. A patient portal handling PHI (diagnoses, prescriptions, lab results) is a high-value XSS target with no defense in depth past input sanitization.
- **Failure scenario:** A stored XSS is introduced via a medication instruction string containing `<script>` that is rendered in the prescription detail page without escaping. Without CSP, the script executes, exfiltrates `document.cookie` (or calls `useSession()` via injected React), and sends the patient's session data to an attacker-controlled endpoint. Without `X-Frame-Options`, the portal can be embedded in an iframe on a malicious site for clickjacking attacks against the appointment scheduling form.
- **Evidence:** No `headers()` configuration in `next.config.ts` steps. Phase 5 "Performance tweaks" and "Accessibility audit" sections have no mention of security headers.
- **Suggested fix:** Add `headers()` to `next.config.ts` defining: `Content-Security-Policy` (restrict `script-src` to self + nonce), `X-Frame-Options: DENY`, `X-Content-Type-Options: nosniff`, `Strict-Transport-Security`, `Referrer-Policy: no-referrer`. This is a one-time addition to `next.config.ts`.

---

## Unresolved Questions

1. Does the YARP Gateway enforce per-patient authorization (i.e., reject a request for Patient B's records when the JWT `sub` is Patient A)? If not, Findings 2 and 7 are Critical rather than High.
2. What is the Keycloak access token lifetime? If it exceeds 15 minutes, Finding 3 (token in client session) becomes more severe.
3. Is the patient portal accessible over HTTPS in staging/production? Finding 4 (cookie security) depends on TLS enforcement outside this plan's scope.
4. Does the plan intend role-based access control beyond "patient" role? Phase 2 states role check is "optional for MVP" — this should be explicitly confirmed as required before go-live, not deferred.
