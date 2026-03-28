# Phase 07 — Docker & Tests

**Status:** Complete
**Priority:** Medium
**Effort:** Small
**Blocked by:** Phases 01–06

## Context Links

- Reference: `client/patient-app/Dockerfile`
- Reference: `docker-compose.yml`
- Reference: `client/patient-app/package.json` (vitest config)

## Overview

Wire `doctor-app` into Docker Compose (port 3200), write Vitest unit tests for
critical utilities and components, confirm build passes.

## Related Code Files

**Create:**
- `client/doctor-app/vitest.config.ts`
- `client/doctor-app/tests/setup.ts`
- `client/doctor-app/tests/unit/proxy-allowlist.test.ts`
- `client/doctor-app/tests/unit/validators.test.ts`
- `client/doctor-app/tests/unit/date-utils.test.ts`
- `client/doctor-app/tests/unit/components/stats-cards.test.tsx`

**Modify:**
- `docker-compose.yml` — add `doctor-app` service

## Implementation Steps

### 1. Vitest config (`vitest.config.ts`)

Copy from patient-app — identical setup (jsdom, @testing-library/react).

### 2. Tests

**`tests/unit/proxy-allowlist.test.ts`** — highest value test:
```ts
// Verify allowed paths pass and non-allowed paths return false
it("allows appointments path", () => expect(isAllowedPath("appointments")).toBe(true));
it("blocks admin path", () => expect(isAllowedPath("admin/users")).toBe(false));
it("allows appointments/:id/complete", () => ...);
```
Export `isAllowedPath` from proxy route for testability.

**`tests/unit/validators.test.ts`** — test Zod schemas:
```ts
// createMedicalRecordSchema: valid + invalid inputs
// createPrescriptionSchema: valid + invalid inputs
```

**`tests/unit/date-utils.test.ts`** — copy from patient-app.

**`tests/unit/components/stats-cards.test.tsx`** — render test:
```ts
it("renders today count", () => {
  render(<StatsCards todayCount={5} completedCount={2} pendingPrescriptions={1} />);
  expect(screen.getByText("5")).toBeInTheDocument();
});
```

### 3. Docker Compose entry

Add to `docker-compose.yml` after `patient-app` (if present) or search services:

```yaml
doctor-app:
  build:
    context: .
    dockerfile: client/doctor-app/Dockerfile
  container_name: doctor-app
  ports:
    - "${DOCTOR_APP_PORT:-3200}:3200"
  environment:
    - NEXTAUTH_URL=http://localhost:3200
    - NEXTAUTH_SECRET=${NEXTAUTH_SECRET:-changeme}
    - KEYCLOAK_CLIENT_ID=doctor-app
    - KEYCLOAK_CLIENT_SECRET=${DOCTOR_APP_KEYCLOAK_SECRET:-}
    - KEYCLOAK_ISSUER=http://keycloak:8080/realms/hospital
    - GATEWAY_API_URL=http://hospital-gateway:8000
  depends_on:
    hospital-gateway:
      condition: service_healthy
    keycloak:
      condition: service_healthy
  healthcheck:
    test: ["CMD-SHELL", "wget --spider -q http://localhost:3200 || exit 1"]
    interval: 15s
    timeout: 5s
    retries: 5
    start_period: 30s
  restart: unless-stopped
  networks:
    - hospital-network
```

### 4. `.env.example` additions

Add to root `.env.example`:
```
DOCTOR_APP_PORT=3200
DOCTOR_APP_KEYCLOAK_SECRET=
```

## Todo

- [ ] Create vitest.config.ts
- [ ] Create tests/setup.ts
- [ ] Create tests/unit/proxy-allowlist.test.ts (export isAllowedPath)
- [ ] Create tests/unit/validators.test.ts
- [ ] Create tests/unit/date-utils.test.ts
- [ ] Create tests/unit/components/stats-cards.test.tsx
- [ ] Add doctor-app service to docker-compose.yml
- [ ] Add DOCTOR_APP_PORT + DOCTOR_APP_KEYCLOAK_SECRET to root .env.example
- [ ] Run `npm run build` — confirm no build errors
- [ ] Run `npm test` — confirm all tests pass

## Success Criteria

- `npm run build` exits 0
- `npm test` exits 0 with all tests passing
- `docker-compose up -d doctor-app` starts and passes healthcheck
- Service accessible at `http://localhost:3200`

## Unresolved Questions

- Is there a patient-app entry in docker-compose.yml? If not, confirm whether frontend apps
  are run locally only (outside Docker). If so, skip docker-compose entry.
