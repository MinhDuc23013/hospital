# Phase 06 — Medical Records & Prescriptions

**Status:** Complete
**Priority:** High
**Effort:** Medium
**Blocked by:** Phase 02, Phase 05

## Context Links

- Reference: `client/patient-app/app/(dashboard)/medical-records/`
- Reference: `client/patient-app/components/medical-records/`
- Reference: `client/patient-app/components/prescriptions/`
- Reference: `client/patient-app/lib/types/medical-record.ts`
- Reference: `client/patient-app/lib/types/prescription.ts`

## Overview

Doctors can view and **create** medical records for their patients, and issue prescriptions.
This is the primary write surface of the doctor-app — absent from patient-app.

## Related Code Files

**Create:**
- `client/doctor-app/app/(dashboard)/medical-records/page.tsx`
- `client/doctor-app/app/(dashboard)/medical-records/loading.tsx`
- `client/doctor-app/app/(dashboard)/medical-records/error.tsx`
- `client/doctor-app/app/(dashboard)/medical-records/new/page.tsx`
- `client/doctor-app/app/(dashboard)/medical-records/[id]/page.tsx`
- `client/doctor-app/app/(dashboard)/prescriptions/page.tsx`
- `client/doctor-app/app/(dashboard)/prescriptions/loading.tsx`
- `client/doctor-app/app/(dashboard)/prescriptions/error.tsx`
- `client/doctor-app/app/(dashboard)/prescriptions/new/page.tsx`
- `client/doctor-app/components/medical-records/record-list.tsx`
- `client/doctor-app/components/medical-records/record-card.tsx`
- `client/doctor-app/components/medical-records/record-detail.tsx`
- `client/doctor-app/components/medical-records/create-record-form.tsx`
- `client/doctor-app/components/prescriptions/prescription-list.tsx`
- `client/doctor-app/components/prescriptions/prescription-card.tsx`
- `client/doctor-app/components/prescriptions/create-prescription-form.tsx`
- `client/doctor-app/lib/hooks/use-medical-records.ts`
- `client/doctor-app/lib/hooks/use-prescriptions.ts`

## Implementation Steps

### 1. Medical Records — List (`app/(dashboard)/medical-records/page.tsx`)

Server Component. Query params: `patientId` (optional, from nav link).
```
GET /api/medical-records?providerId={doctorId}&patientId={patientId?}
```
Renders `<RecordList />` with "New Record" button → `/medical-records/new`.

### 2. Medical Records — Detail (`app/(dashboard)/medical-records/[id]/page.tsx`)

Server Component:
```
GET /api/medical-records/{id}
```
Renders `<RecordDetail />` with diagnosis, notes, lab results table.

### 3. Create Medical Record (`app/(dashboard)/medical-records/new/page.tsx`)

Client page with `<CreateRecordForm />` using React Hook Form + Zod (`createMedicalRecordSchema`):
```
Fields: patientId (select from doctor's patients), diagnosis, notes, labResults[]
POST /api/proxy/medical-records
```
On success → redirect to `/medical-records/[newId]`.

### 4. `components/medical-records/create-record-form.tsx`

```tsx
// Client Component
// Fields: patient selector, diagnosis (textarea), notes (textarea),
//         dynamic lab results (add/remove rows: name + value)
// Submits via useMutation → POST /api/proxy/medical-records
```

### 5. Prescriptions — List (`app/(dashboard)/prescriptions/page.tsx`)

Server Component:
```
GET /api/prescriptions?providerId={doctorId}&status={status?}
```
Status tabs: All / Pending / Dispensed. "New Prescription" button → `/prescriptions/new`.

### 6. Create Prescription (`app/(dashboard)/prescriptions/new/page.tsx`)

Client page with `<CreatePrescriptionForm />` (`createPrescriptionSchema`):
```
Fields: patientId (select), drugId (search/select from pharmacy), dosage,
        instructions, durationDays, appointmentId (optional)
POST /api/proxy/prescriptions
```
On success → redirect to `/prescriptions`.

### 7. Hooks

**`lib/hooks/use-medical-records.ts`**
```ts
export function useCreateMedicalRecord() {
  return useMutation({
    mutationFn: (data) => fetch("/api/proxy/medical-records", { method: "POST", ... }),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ["medical-records"] }),
  });
}
```

**`lib/hooks/use-prescriptions.ts`**
```ts
export function useCreatePrescription() {
  return useMutation({
    mutationFn: (data) => fetch("/api/proxy/prescriptions", { method: "POST", ... }),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ["prescriptions"] }),
  });
}
```

## Proxy Allowlist Additions (Phase 02 update)

Ensure proxy route allows:
- `POST /api/proxy/medical-records`
- `GET /api/proxy/medical-records/:id`
- `POST /api/proxy/prescriptions`
- `GET /api/proxy/prescriptions`

## Todo

- [ ] Create lib/hooks/use-medical-records.ts
- [ ] Create lib/hooks/use-prescriptions.ts
- [ ] Create medical-records/page.tsx + loading.tsx + error.tsx
- [ ] Create medical-records/[id]/page.tsx
- [ ] Create medical-records/new/page.tsx
- [ ] Create components/medical-records/record-list.tsx
- [ ] Create components/medical-records/record-card.tsx
- [ ] Create components/medical-records/record-detail.tsx
- [ ] Create components/medical-records/create-record-form.tsx
- [ ] Create prescriptions/page.tsx + loading.tsx + error.tsx
- [ ] Create prescriptions/new/page.tsx
- [ ] Create components/prescriptions/prescription-list.tsx
- [ ] Create components/prescriptions/prescription-card.tsx
- [ ] Create components/prescriptions/create-prescription-form.tsx
- [ ] Update proxy allowlist if needed

## Success Criteria

- Doctor can create a medical record for a patient (POST succeeds, redirects to detail)
- Lab results rows can be added/removed dynamically in form
- Doctor can issue a prescription with drug lookup
- Record and prescription lists filter by patient when navigated from patient profile
- `npm run typecheck` passes with no errors on form schemas

## Security Considerations

- `providerId` set server-side from JWT, never from form input
- Drug lookup only returns from pharmacy service (no free-text drug IDs)
- Form inputs validated with Zod before submission

## Unresolved Questions

- Does the Medical Record Service accept `providerId` on creation? Verify endpoint contract.
- Does the Pharmacy Service expose a drug search endpoint usable from the doctor-app proxy?
- Is `POST /api/prescriptions` on Pharmacy Service or a separate endpoint?

## Next Steps

→ Phase 07: Docker + Tests
