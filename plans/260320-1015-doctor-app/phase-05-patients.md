# Phase 05 — Patient Management

**Status:** Complete
**Priority:** High
**Effort:** Small
**Blocked by:** Phase 02

## Context Links

- Reference: `client/patient-app/lib/types/patient.ts`
- Reference: `client/patient-app/components/profile/profile-view.tsx`

## Overview

Read-only patient list and patient profile view. Doctor sees patients they have (or have had)
appointments with. No create/edit — doctors don't manage patient demographics.

## Related Code Files

**Create:**
- `client/doctor-app/app/(dashboard)/patients/page.tsx`
- `client/doctor-app/app/(dashboard)/patients/loading.tsx`
- `client/doctor-app/app/(dashboard)/patients/error.tsx`
- `client/doctor-app/app/(dashboard)/patients/[id]/page.tsx`
- `client/doctor-app/app/(dashboard)/patients/[id]/loading.tsx`
- `client/doctor-app/components/patients/patient-card.tsx`
- `client/doctor-app/components/patients/patient-list.tsx`
- `client/doctor-app/components/patients/patient-profile-view.tsx`
- `client/doctor-app/lib/hooks/use-patients.ts`

## Implementation Steps

### 1. Patient list page (`app/(dashboard)/patients/page.tsx`)

Server Component — fetches patients associated with doctor:
```
GET /api/patients?providerId={doctorId}
```
Renders `<PatientList />` with search input (client-side filter on loaded data).

### 2. Patient profile page (`app/(dashboard)/patients/[id]/page.tsx`)

Server Component:
```
GET /api/patients/{id}
```
Renders `<PatientProfileView />` with:
- Demographics (name, DOB, email, phone)
- Quick links: "View Appointments →" `/appointments?patientId={id}`, "View Medical Records →" `/medical-records?patientId={id}`

### 3. Hook (`lib/hooks/use-patients.ts`)

```ts
export function usePatients(providerId: string) {
  return useQuery({
    queryKey: ["patients", providerId],
    queryFn: () => fetch(`/api/proxy/patients?providerId=${providerId}`).then(r => r.json()),
  });
}
```
Used for client-side search/filter without page reload.

### 4. `components/patients/patient-card.tsx`

Card showing: full name, email, DOB (formatted), phone. Links to `/patients/[id]`.

### 5. `components/patients/patient-profile-view.tsx`

Read-only profile display (adapt from patient-app `profile-view.tsx`). Remove edit button.

## Todo

- [ ] Create lib/hooks/use-patients.ts
- [ ] Create patients/page.tsx + loading.tsx + error.tsx
- [ ] Create patients/[id]/page.tsx + loading.tsx
- [ ] Create components/patients/patient-card.tsx
- [ ] Create components/patients/patient-list.tsx
- [ ] Create components/patients/patient-profile-view.tsx

## Success Criteria

- Patient list shows only patients linked to logged-in doctor
- Search filters list client-side without refetch
- Patient profile shows demographics with quick links
- No edit/delete controls visible

## Security Considerations

- `providerId` always from server-side JWT, never from client input
- Patient data read-only in this portal

## Next Steps

→ Phase 06: Medical Records & Prescriptions
