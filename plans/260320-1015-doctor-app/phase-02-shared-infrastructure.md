# Phase 02 — Shared Infrastructure

**Status:** Complete
**Priority:** Critical (blocks phases 3–6)
**Effort:** Medium
**Blocked by:** Phase 01

## Context Links

- Reference: `client/patient-app/lib/api-client.ts`
- Reference: `client/patient-app/app/api/proxy/[...path]/route.ts`
- Reference: `client/patient-app/components/layout/`
- Reference: `client/patient-app/components/shared/`
- Reference: `client/patient-app/components/ui/`

## Overview

Build the reusable infrastructure layer: server-side API client, proxy route handler,
shared types, utility functions, layout components (sidebar/topbar), and shared UI components.
This mirrors patient-app patterns but with doctor-scoped proxy allowlist and navigation.

## Related Code Files

**Create:**
- `client/doctor-app/lib/api-client.ts`
- `client/doctor-app/lib/utils/cn.ts`
- `client/doctor-app/lib/utils/date-utils.ts`
- `client/doctor-app/lib/utils/format-utils.ts`
- `client/doctor-app/lib/types/api-response.ts`
- `client/doctor-app/lib/types/appointment.ts`
- `client/doctor-app/lib/types/patient.ts`
- `client/doctor-app/lib/types/medical-record.ts`
- `client/doctor-app/lib/types/prescription.ts`
- `client/doctor-app/lib/types/provider.ts`
- `client/doctor-app/lib/types/index.ts`
- `client/doctor-app/lib/validators/appointment-schema.ts`
- `client/doctor-app/lib/validators/medical-record-schema.ts`
- `client/doctor-app/lib/validators/prescription-schema.ts`
- `client/doctor-app/lib/validators/index.ts`
- `client/doctor-app/app/api/proxy/[...path]/route.ts`
- `client/doctor-app/app/(dashboard)/layout.tsx`
- `client/doctor-app/components/layout/sidebar.tsx`
- `client/doctor-app/components/layout/topbar.tsx`
- `client/doctor-app/components/shared/page-header.tsx`
- `client/doctor-app/components/shared/empty-state.tsx`
- `client/doctor-app/components/shared/status-badge.tsx`
- `client/doctor-app/components/shared/data-table.tsx`
- `client/doctor-app/components/ui/` (button, card, input, badge, label, tabs, dialog, select, toast, toaster, table, form)

## Implementation Steps

### 1. Types (`lib/types/`)

Copy `appointment.ts`, `patient.ts`, `medical-record.ts`, `prescription.ts`, `api-response.ts` verbatim from patient-app.
Add `provider.ts` (same as patient-app).
Create `index.ts` re-exporting all.

### 2. Utility functions (`lib/utils/`)

Copy `cn.ts`, `date-utils.ts`, `format-utils.ts` verbatim from patient-app.

### 3. Validators (`lib/validators/`)

Copy `appointment-schema.ts` from patient-app (doctor can update appointment status).
Add `medical-record-schema.ts`:
```ts
// Zod schema for creating/updating a medical record
export const createMedicalRecordSchema = z.object({
  patientId: z.string().uuid(),
  diagnosis: z.string().min(1).max(500),
  notes: z.string().max(2000).optional(),
  labResults: z.array(z.object({ name: z.string(), value: z.string() })).optional(),
});
```
Add `prescription-schema.ts`:
```ts
export const createPrescriptionSchema = z.object({
  patientId: z.string().uuid(),
  appointmentId: z.string().uuid().optional(),
  drugId: z.string(),
  dosage: z.string().min(1),
  instructions: z.string().max(500).optional(),
  durationDays: z.number().int().positive(),
});
```

### 4. API Client (`lib/api-client.ts`)

Copy verbatim from patient-app — same `callGatewayAPI<T>()` pattern.

### 5. Proxy Route (`app/api/proxy/[...path]/route.ts`)

Copy from patient-app but with **doctor-scoped allowlist**:
```ts
const ALLOWED_PATHS = [
  /^appointments(\/[^/]+)?(\/complete|\/cancel)?$/,
  /^appointments$/, // list
  /^patients(\/[^/]+)?$/,
  /^medical-records(\/[^/]+)?$/,
  /^prescriptions(\/[^/]+)?$/,
  /^providers\/[^/]+$/,
];
```

### 6. Layout Components

**`components/layout/sidebar.tsx`** — Doctor nav links:
- Dashboard (`/`)
- My Schedule (`/schedule`)
- Appointments (`/appointments`)
- Patients (`/patients`)
- Medical Records (`/medical-records`)
- Prescriptions (`/prescriptions`)

**`components/layout/topbar.tsx`** — Copy from patient-app, update branding to "Doctor Portal".

### 7. Dashboard Layout (`app/(dashboard)/layout.tsx`)

Copy from patient-app — auth guard, sidebar + topbar layout.

### 8. Shared UI Components

Copy all `components/ui/` primitives and `components/shared/` components verbatim from patient-app.

## Todo

- [ ] Create lib/types/* (copy + extend from patient-app)
- [ ] Create lib/utils/* (copy from patient-app)
- [ ] Create lib/validators/* (appointment + new medical-record + prescription schemas)
- [ ] Create lib/api-client.ts (copy from patient-app)
- [ ] Create proxy route with doctor-scoped allowlist
- [ ] Create (dashboard)/layout.tsx
- [ ] Create sidebar with doctor nav links
- [ ] Create topbar (copy, update branding)
- [ ] Copy components/ui/* from patient-app
- [ ] Copy components/shared/* from patient-app

## Success Criteria

- Proxy route returns 403 for non-allowlisted paths
- `npm run typecheck` passes
- Dashboard layout redirects unauthenticated users to `/login`

## Security Considerations

- Proxy allowlist must only include doctor-appropriate endpoints
- No admin/internal paths in allowlist
- Bearer token injected server-side only (never in client bundle)

## Next Steps

→ Phase 03: Dashboard page
