# Plan Review: Next.js Patient Client App

**Reviewed:** 2026-03-18
**Plan:** `plans/260318-1719-nextjs-patient-client-app/`
**Reviewer role:** Scope & Complexity Critic (YAGNI enforcer)

---

## Finding 1: Dual Data-Fetching Paths Create Double the Complexity for Zero MVP Gain

- **Severity:** High
- **Location:** Phase 3, section "Architecture" and Phase 4, section "Page Rendering Strategy"
- **Flaw:** The plan mandates a "Hybrid" pattern for every list page: Server Component initial load PLUS TanStack Query client-side refetch. This creates two separate data paths (server fetch via `callGatewayAPI` + client fetch via `/api/proxy`) that must both be implemented, kept in sync, and tested. For an MVP patient portal this is pure over-engineering.
- **Failure scenario:** A developer implementing the Appointments page writes a server fetch in `page.tsx` and a `useAppointments` hook. When a filter changes, TanStack Query fires a second request through the proxy, potentially returning different data than the server render, causing hydration mismatches or stale-data bugs that are hard to debug. Two code paths double the surface area for bugs with no user-visible benefit.
- **Evidence:** Phase 4 table shows 5 of 9 pages as "Hybrid". Phase 3 says "Preferred Pattern: Server Components for initial load, TanStack Query for mutations/refetch" — then immediately plans TanStack hooks for all list pages including read-only ones (medical records, prescriptions).
- **Suggested fix:** For MVP, pick one pattern. Either pure Server Components (simpler, more secure) or pure TanStack Query via the proxy. Reserve hybrid only for pages with known real-time mutation requirements (schedule/cancel appointments). Prescriptions and Medical Records are read-only — server components only.

---

## Finding 2: The API Proxy is an Unnecessary Extra Hop

- **Severity:** High
- **Location:** Phase 3, section "Architecture" and step 3 "Create API proxy route"
- **Flaw:** The plan adds an internal Next.js API proxy (`/api/proxy/[...path]/route.ts`) that catches all client-side requests and forwards them to the Gateway. This is a catch-all wildcard proxy on top of an already-existing API Gateway. You now have: Browser → Next.js proxy → YARP Gateway → Microservice. Three network hops instead of two.
- **Failure scenario:** The proxy route is implemented as a wildcard catch-all. Any client-side request to `/api/proxy/anything` gets forwarded. If the Gateway has endpoints that mutate data or expose admin APIs, the proxy becomes an unintended amplifier — there is no per-route allowlist in the plan. Additionally, streaming responses (large lab results documents) will buffer entirely in the Next.js process before forwarding.
- **Evidence:** Phase 3, step 3: "Catches all `/api/proxy/*` requests... Forwards to Gateway API". The plan's own note says "optional, for client-side" but then all four TanStack Query hooks are written to call `/api/proxy/...` making it mandatory in practice.
- **Suggested fix:** Use Server Components for data fetching (no proxy needed). For mutations (schedule/cancel), call the Gateway URL directly from a Next.js Server Action — no client-exposed proxy required. Delete `app/api/proxy/[...path]/route.ts` from the plan.

---

## Finding 3: `cancel/page.tsx` as a Nested Route is Gold-Plating

- **Severity:** Medium
- **Location:** Phase 4, section "Route Structure" and step 12
- **Flaw:** The plan creates `appointments/[id]/cancel/page.tsx` as a separate navigable route with its own page, layout, and loading state. A cancel confirmation for an appointment is a modal dialog, not a page. Creating a full route for it adds a page file, a URL the user can directly navigate to (which will fail without context), and extra complexity in the router for a one-button confirmation.
- **Failure scenario:** A user bookmarks `localhost:3100/appointments/abc123/cancel` and navigates there directly. The page needs to load appointment detail data independently, handle the case where the appointment is already cancelled, and manage back-navigation — none of which the plan addresses. The `cancel-dialog.tsx` component is already planned in the same list, making the page redundant.
- **Evidence:** Phase 4, Related Code Files: both `app/(dashboard)/appointments/[id]/cancel/page.tsx` and `components/appointments/cancel-dialog.tsx` are listed. Step 12 says "Create cancel dialog/page with confirmation" — the plan itself is unsure which it is.
- **Suggested fix:** Remove `[id]/cancel/page.tsx`. Use `cancel-dialog.tsx` as a modal triggered from the appointment detail page. One component, no extra route.

---

## Finding 4: Dark Mode in Phase 5 is Scope Creep for a Medical MVP

- **Severity:** Medium
- **Location:** Phase 5, section "Requirements" and step 1
- **Flaw:** Dark mode (`next-themes` toggle, CSS variable updates, theme-toggle component, testing on all pages) is explicitly included in a 2h "UI Polish" phase for an internal patient portal MVP. This adds a theme toggle component, a ThemeProvider wrapper, globals.css modifications, and a test pass across all pages — for a feature no patient portal requirements document mentions.
- **Failure scenario:** The 2h estimate for Phase 5 already covers loading skeletons, error boundaries, responsive sidebar (3 breakpoints), accessibility audit, and performance tweaks. Adding dark mode to that scope means either the estimate is wrong by 2x or accessibility/responsive work gets cut. Medical portals have specific contrast requirements that change between light and dark themes — a dark mode done quickly will fail WCAG AA on at least some components.
- **Evidence:** Phase 5 Requirements: "Dark mode toggle (next-themes)". Plan.md stack includes `next-themes` as a core dependency from Phase 1.
- **Suggested fix:** Remove dark mode from MVP scope entirely. The dependency and ThemeProvider can be added later. Focus Phase 5's 2h on skeletons, error boundaries, and mobile sidebar — which are blocking UX issues.

---

## Finding 5: 60%+ Test Coverage Target is Unachievable in 2h

- **Severity:** High
- **Location:** Phase 6, section "Non-functional requirements" and "Todo List"
- **Flaw:** Phase 6 is allocated 2h. The todo list in that phase contains 12 items including: install and configure Vitest + MSW, write schema tests, write API client tests (with mocked session + fetch), write component tests (3 components), write hook tests with MSW, create `.dockerignore`, verify Docker build, test docker-compose, and create + run a smoke test script. Achieving >60% coverage over `lib/` and `components/` (which has ~40 files planned across phases 3–4) in this budget is not feasible.
- **Failure scenario:** A developer follows the plan, spends 1.5h on Vitest/MSW configuration (App Router + RSC mocking is notoriously non-trivial), writes skeleton tests for 3 components, and declares "done". Coverage comes in at 15–20%. Either the phase ships incomplete or other phases get cut to compensate. The success criterion becomes a blocker on delivery.
- **Evidence:** Phase 6, Non-functional: "Test coverage >60% for lib/ and components/". Phase 6, Effort: "2h". Phase 6, Architecture: "E2E (optional): Playwright for login flow" — even listing Playwright as an option in a 2h phase shows scope blindness.
- **Suggested fix:** Reduce coverage target to >40% for MVP. Scope tests to: Zod schemas (fast, high value), API client error handling (critical path), and 2 smoke component renders. Drop MSW entirely for MVP — mock `fetch` directly. Move Docker + smoke tests to a separate ops task, not bundled into 2h.

---

## Finding 6: Breadcrumbs Mentioned as Optional Mid-Implementation

- **Severity:** Medium
- **Location:** Phase 4, step 3 "Create `components/layout/topbar.tsx`"
- **Flaw:** "Breadcrumbs (optional)" is listed as a topbar feature during the core implementation step. Calling something "optional" mid-implementation step without a decision boundary means implementors will build it "just in case" or spend time deciding whether to build it. For a 5-page portal with a sidebar, breadcrumbs are redundant navigation — the sidebar already shows location.
- **Failure scenario:** An implementor builds breadcrumbs in the topbar. This requires knowledge of the current route (adding `usePathname` + a breadcrumb config map), a new component, and responsive truncation logic on mobile. None of this is in the file list or effort estimate. It quietly inflates Phase 4's 6h budget.
- **Evidence:** Phase 4, step 3: "Breadcrumbs (optional)".
- **Suggested fix:** Remove "Breadcrumbs (optional)" entirely. If breadcrumbs are needed post-MVP, add them as a scoped task then.

---

## Finding 7: `lib/api-client-config.ts` is a File That Shouldn't Exist Separately

- **Severity:** Medium
- **Location:** Phase 3, "Related Code Files" → Create list
- **Flaw:** The plan creates two files: `lib/api-client.ts` (the actual client) and `lib/api-client-config.ts` (base URL, error handling). For a client that calls a single Gateway URL with a Bearer token, splitting config from implementation is premature abstraction. The "config" is 2–3 constants and a base URL string.
- **Failure scenario:** A developer reads the plan and creates `api-client-config.ts` with `GATEWAY_BASE_URL`, `DEFAULT_HEADERS`, and `handleApiError()`. Then `api-client.ts` imports from it. Now any modification to error handling requires touching two files. New team members must discover which file owns what. This split adds zero value at this scale.
- **Evidence:** Phase 3, Related Code Files: both `lib/api-client.ts` and `lib/api-client-config.ts` listed as separate files to create.
- **Suggested fix:** Merge into a single `lib/api-client.ts`. Config constants and the client implementation belong together until there are multiple clients.

---

## Finding 8: `login-form.tsx` Component is a Wrapper Around a Single Button

- **Severity:** Medium
- **Location:** Phase 2, "Related Code Files" and step 8
- **Flaw:** The plan creates `components/auth/login-form.tsx` as a dedicated component whose sole purpose is to render a "Sign In with Keycloak" button that calls `signIn("keycloak", { callbackUrl: "/dashboard" })`. This is a one-liner wrapped in a component file. The plan also creates a separate `login/page.tsx` that renders this component. Two files for a single button.
- **Failure scenario:** A developer creates `login-form.tsx`, exports `LoginForm`, imports it in `login/page.tsx`, and the component file contains 15 lines including imports. Any change to the button text or callback URL requires opening `login-form.tsx`. The abstraction boundary provides no reusability — this component is used in exactly one place.
- **Evidence:** Phase 2, step 8: "login page... 'Sign In with Keycloak' button using `signIn(...)`". Phase 2, Related Code Files: `components/auth/login-form.tsx` listed separately from `app/(auth)/login/page.tsx`.
- **Suggested fix:** Inline the sign-in button directly in `login/page.tsx`. Remove `login-form.tsx` from the file list. If future login options are added (username/password), extract then.

---

## Finding 9: Unresolved Question #3 (Self-Registration) is a Blocking Scope Decision, Not a Note

- **Severity:** Critical
- **Location:** `plan.md`, "Unresolved Questions", item 3
- **Flaw:** "Patient self-registration or admin-only creation for MVP?" is listed as an unresolved question in the plan overview. This is not a minor detail — it determines whether Phase 2 needs to implement a registration page, whether Phase 4 needs a registration form, whether Keycloak needs a self-service registration flow configured, and whether the API Gateway needs a public (unauthenticated) patient creation endpoint. The entire auth and page scope changes depending on the answer.
- **Failure scenario:** Implementation begins with the assumption of admin-only creation. At Phase 4 delivery, stakeholders ask why patients cannot self-register. Adding self-registration at that point requires: a new Keycloak registration flow, a new public API endpoint, a new registration page, and form validation — retroactively touching phases 1–4. Alternatively, a developer assumes self-registration is needed and builds it, adding ~4h of unscoped work.
- **Evidence:** `plan.md`, Unresolved Questions #3. None of the 6 phase files address this or document an assumed answer.
- **Suggested fix:** Resolve this before implementation begins. Document the answer explicitly in `plan.md` and in Phase 2's Requirements. Given YAGNI, default to admin-only creation for MVP and close the question.

---

## Unresolved Questions

1. Is the "Hybrid" rendering pattern explicitly required by a backend constraint (e.g., Gateway does not support streaming), or is it a design preference? If preference, it should be cut.
2. What is the actual backend API contract for appointments — specifically, does the Gateway endpoint already scope results to the authenticated patient via JWT, or does the client need to pass `patientId` explicitly? (Risk listed in Phase 4 as "Medium/High" but not resolved before implementation.)
3. Phase 6 lists both Vitest and Jest as options ("Vitest or Jest via Next.js default") without making a decision. This ambiguity means the implementor will spend time choosing instead of building.
