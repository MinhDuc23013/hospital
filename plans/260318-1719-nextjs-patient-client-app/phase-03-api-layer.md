# Phase 3: API Layer

## Context Links

- [Plan Overview](plan.md)
- [System Architecture](../../docs/system-architecture.md)
- [Phase 2: Auth Integration](phase-02-auth-integration.md)
- [Patient Portal UI Research](../../plans/reports/researcher-260318-1717-nextjs-patient-portal.md)

## Overview

- **Priority:** P1 (blocks core pages)
- **Status:** completed
- **Effort:** 3h
- **Description:** Build typed API client for server-side calls, client-side TanStack Query hooks, Zod validation schemas, and TypeScript interfaces matching backend responses.

## Requirements

### Functional
- Server-side API client injects Bearer token from session
- All calls go through API Gateway (port 8000), never directly to services
- TanStack Query hooks for client-side data fetching
- Zod schemas for form validation
- TypeScript types matching backend API responses

### Non-functional
- Type-safe end-to-end (API response → component props)
- Error handling: 401 → redirect to login, 4xx/5xx → meaningful error
- Stale time: 5min for lists, no-cache for mutations
- Server-side fetch uses `cache: 'no-store'` for authenticated data

## Architecture

```
Data Flow:

Server Components (initial page loads, static data):
  page.tsx → getAuthSession() → callGatewayAPI(endpoint) → render

Client Components (ONLY for interactive features: filters, pagination, mutations):
  component.tsx → useQuery(hook) → fetch(/api/proxy/...) → render

Decision rule (AUDIT FIX F13 — reduces dual-path complexity):
  Use Server Component by default.
  Use TanStack Query ONLY when: filter/sort changes without page reload, or mutation needs optimistic update.
  Do NOT implement both server fetch + TanStack hook for the same resource on the same page.
```

## Related Code Files

### Create
- `client/patient-app/lib/api-client.ts` -- server-side API client (Bearer token injection)
- `client/patient-app/lib/api-client-config.ts` -- base URL, error handling
- `client/patient-app/lib/hooks/use-appointments.ts` -- TanStack Query hooks
- `client/patient-app/lib/hooks/use-medical-records.ts`
- `client/patient-app/lib/hooks/use-prescriptions.ts`
- `client/patient-app/lib/hooks/use-patient-profile.ts`
- `client/patient-app/lib/types/patient.ts`
- `client/patient-app/lib/types/appointment.ts`
- `client/patient-app/lib/types/medical-record.ts`
- `client/patient-app/lib/types/prescription.ts`
- `client/patient-app/lib/types/api-response.ts`
- `client/patient-app/lib/validators/appointment-schema.ts`
- `client/patient-app/lib/validators/patient-schema.ts`
- `client/patient-app/lib/validators/index.ts`
- `client/patient-app/lib/query-client-provider.tsx` -- TanStack QueryClientProvider
- `client/patient-app/app/api/proxy/[...path]/route.ts` -- optional API proxy for client calls

### Modify
- `client/patient-app/app/layout.tsx` -- wrap with QueryClientProvider

## Implementation Steps

1. **Define TypeScript types** (from backend API contracts in system-architecture.md)

   `lib/types/api-response.ts`:
   ```typescript
   export interface ApiResponse<T> {
     data: T
     timestamp: string
   }
   export interface PaginatedResponse<T> {
     data: T[]
     pagination: { total: number; page: number; pageSize: number; totalPages: number }
   }
   export interface ApiError {
     error: { message: string; status: number; timestamp: string; correlationId?: string }
   }
   ```

   `lib/types/patient.ts`:
   ```typescript
   export interface Patient {
     id: string; email: string; firstName: string; lastName: string
     dateOfBirth: string; phoneNumber?: string; address?: string
     isActive: boolean; createdAt: string; updatedAt: string
   }
   ```

   `lib/types/appointment.ts`:
   ```typescript
   export interface Appointment {
     id: string; patientId: string; providerId: string
     scheduledTime: string; duration: string
     status: 'Scheduled' | 'InProgress' | 'Completed' | 'Cancelled'
     notes?: string; createdAt: string; updatedAt: string
   }
   ```

   `lib/types/medical-record.ts`:
   ```typescript
   export interface MedicalRecord {
     _id: string; patientId: string; appointmentId: string
     findings: string; diagnosis: string[]
     labResults: LabResult[]; documents: Document[]
     createdBy: string; createdAt: string; updatedAt: string
   }
   export interface LabResult {
     testName: string; result: string; normalRange: string; timestamp: string
   }
   ```

   `lib/types/prescription.ts`:
   ```typescript
   export interface Prescription {
     id: string; patientId: string; drugId: number
     quantity: number; instructions: string
     issuedAt: string; validUntil: string
     status: 'Pending' | 'Dispensed' | 'Expired'
   }
   ```

2. **Patient ID strategy** — no spike needed <!-- Updated: Validation Session 1 - Keycloak sub = Patient Service ID -->
   - Confirmed: Patient Service accepts Keycloak JWT `sub` as patient ID directly
   - All hooks use `session.user.id` (which is `token.sub`) as `patientId` — no extra lookup or mapping

3. **Create server-side API client** (`lib/api-client.ts`)
   - `getAuthSession()` to get access token from JWT (server-side only)
   - `callGatewayAPI<T>(endpoint, options)` -- generic, typed
   - Error handling: 401 throw AuthError, 4xx/5xx throw ApiError
   - Base URL from `GATEWAY_API_URL` env var (server-only, no `NEXT_PUBLIC_` prefix)

3. **Create API proxy route** (`app/api/proxy/[...path]/route.ts`)
   > **[AUDIT FIX F2]** Catch-all proxy with no allowlist = open IDOR vector. An authenticated patient can probe `/api/proxy/admin/anything`. Add explicit path allowlist.
   ```typescript
   // Allowed proxy paths (patient-facing only)
   const ALLOWED_PATHS = [
     /^appointments(\/[^/]+)?(\/cancel)?$/,
     /^medical-records(\/[^/]+)?$/,
     /^prescriptions(\/[^/]+)?$/,
     /^patients\/[^/]+$/,  // own profile only — backend also enforces
   ]
   export async function GET(req: Request, { params }: { params: { path: string[] } }) {
     const path = params.path.join('/')
     if (!ALLOWED_PATHS.some(r => r.test(path))) {
       return NextResponse.json({ error: 'Not allowed' }, { status: 403 })
     }
     // ... inject bearer token and forward
   }
   ```
   - `GATEWAY_API_URL` from server-only env — never `NEXT_PUBLIC_GATEWAY_URL`

4. **Configure TanStack Query** (`lib/query-client-provider.tsx`)
   - Client component with QueryClientProvider
   - Default options: staleTime 5min, retry 1, refetchOnWindowFocus true
   - Add to root layout

5. **Create TanStack Query hooks**

   `lib/hooks/use-appointments.ts`:
   - `useAppointments(filters?)` -- GET /api/proxy/appointments
   - `useAppointment(id)` -- GET /api/proxy/appointments/:id
   - `useScheduleAppointment()` -- POST mutation
   - `useCancelAppointment()` -- DELETE mutation

   `lib/hooks/use-medical-records.ts`:
   - `useMedicalRecords(patientId)` -- GET /api/proxy/medical-records/:patientId
   - `useMedicalRecord(id)` -- GET /api/proxy/medical-records/:id

   `lib/hooks/use-prescriptions.ts`:
   - `usePrescriptions(patientId?)` -- GET /api/proxy/prescriptions

   `lib/hooks/use-patient-profile.ts`:
   - `usePatientProfile()` -- GET /api/proxy/patients/:id (uses session.user.id as :id)
   - `useUpdateProfile()` -- PUT mutation

   `lib/hooks/use-providers.ts`: <!-- Updated: Validation Session 1 - /api/providers endpoint confirmed -->
   - `useProviders()` -- GET /api/proxy/providers (confirmed endpoint exists)
   - Used by schedule appointment form Select dropdown

6. **Create Zod validation schemas**

   `lib/validators/appointment-schema.ts`:
   - `ScheduleAppointmentSchema` (providerId, scheduledTime, duration, notes)
   - `CancelAppointmentSchema` (appointmentId, reason)

   `lib/validators/patient-schema.ts`:
   - `UpdatePatientSchema` (firstName, lastName, phoneNumber, address)

7. **Verify type compilation** -- `npx tsc --noEmit`

## Todo List

- [x] Create `lib/types/api-response.ts`
- [x] Create `lib/types/patient.ts`
- [x] Create `lib/types/appointment.ts`
- [x] Create `lib/types/medical-record.ts`
- [x] Create `lib/types/prescription.ts`
- [x] Create `lib/api-client.ts` (server-side)
- [x] Create `app/api/proxy/[...path]/route.ts`
- [x] Create `app/api/auth/token/route.ts`
- [x] Create `lib/hooks/use-appointments.ts`
- [x] Create `lib/hooks/use-medical-records.ts`
- [x] Create `lib/hooks/use-prescriptions.ts`
- [x] Create `lib/hooks/use-patient-profile.ts`
- [x] Create `lib/hooks/use-providers.ts`
- [x] Create `lib/validators/appointment-schema.ts`
- [x] Create `lib/validators/patient-schema.ts`
- [x] Create `lib/validators/index.ts`
- [x] Create `lib/utils/date-utils.ts`
- [x] Create `lib/utils/format-utils.ts`
- [x] Create `lib/types/index.ts` (barrel export)
- [x] Create `lib/types/provider.ts`
- [x] Fix package.json (@radix-ui/react-calendar non-existent → @radix-ui/react-popover)
- [x] Verify TypeScript compiles (tsc --noEmit: clean)

## Success Criteria

- `callGatewayAPI()` injects Bearer token from session
- TanStack Query hooks fetch via /api/proxy (no direct Gateway calls from browser)
- TypeScript types match backend API response shapes
- Zod schemas validate form inputs
- 401 errors trigger redirect to login
- `npx tsc --noEmit` passes

## Risk Assessment

| Risk | Likelihood | Impact | Mitigation |
|------|-----------|--------|------------|
| API response shape mismatch | High | Medium | Log actual responses during dev, adjust types |
| Proxy adds latency | Low | Low | Acceptable for security trade-off |
| Token expired mid-request | Medium | Medium | API client catches 401, triggers re-auth |
| CORS | None | — | Confirmed configured in gateway <!-- Validation Session 1 --> |

## Next Steps

- Phase 4: Core Pages (consumes API hooks and server-side client)
