---
title: "Phase 2 — Frontend Doctor-app CRUD UI"
status: complete
priority: P1
effort: 3h
---

# Phase 2: Frontend — Doctor-app CRUD UI

## Context Links

- Patient list page: `client/doctor-app/app/(dashboard)/patients/page.tsx`
- Patient detail page: `client/doctor-app/app/(dashboard)/patients/[id]/page.tsx`
- PatientList component: `client/doctor-app/components/patients/patient-list.tsx`
- PatientProfileView: `client/doctor-app/components/patients/patient-profile-view.tsx`
- PatientCard: `client/doctor-app/components/patients/patient-card.tsx`
- Pattern reference (patient-app): `client/patient-app/app/(main)/profile/edit/page.tsx`
- Pattern reference (form): `client/patient-app/components/profile/profile-edit-form.tsx`
- Proxy config: `client/doctor-app/app/api/proxy/[...path]/route.ts`
- Existing hooks pattern: `client/doctor-app/lib/hooks/`

## Overview

Build Create/Edit/Delete UI in doctor-app so doctors can fully manage patients. Follow existing patient-app form patterns (react-hook-form + zodResolver + mutation + toast).

## Key Insights

- Proxy allowlist already permits PUT/DELETE on `patients/:id` — no proxy changes needed
- patient-app's profile-edit-form.tsx is the exact pattern to follow for form structure
- Use existing shadcn/ui components (Card, Button, Input, Label, Dialog for confirm)
- Keep forms simple: firstName, lastName, phoneNumber only (matching backend UpdatePatientCommand)
- Create form adds email + dateOfBirth (matching CreatePatientCommand)

## Requirements

### Functional
- "New Patient" button on patient list page → create form
- "Edit" button on patient profile → edit form (pre-filled)
- "Deactivate" button on patient profile → confirmation dialog → DELETE call
- Success/error toasts on all mutations
- Redirect to list after create, redirect to profile after edit, redirect to list after delete

### Non-functional
- Client components for forms (`"use client"`)
- Server components for page wrappers where possible
- Zod validation matching backend rules
- Loading/disabled states during mutations
- Mobile-responsive forms using existing layout patterns

## Architecture

```
pages (server) → form components (client) → hooks (mutations) → proxy API → PatientService
```

## Related Code Files

### Files to create
- `client/doctor-app/lib/hooks/use-patients.ts` — mutation hooks
- `client/doctor-app/lib/validators/patient-validators.ts` — zod schemas
- `client/doctor-app/components/patients/patient-create-form.tsx`
- `client/doctor-app/components/patients/patient-edit-form.tsx`
- `client/doctor-app/components/patients/patient-delete-dialog.tsx`
- `client/doctor-app/app/(dashboard)/patients/new/page.tsx`
- `client/doctor-app/app/(dashboard)/patients/[id]/edit/page.tsx`

### Files to modify
- `client/doctor-app/components/patients/patient-profile-view.tsx` — add Edit + Deactivate buttons
- `client/doctor-app/components/patients/patient-list.tsx` — add "New Patient" button

## Implementation Steps

### Step 1: Create Zod validators

File: `client/doctor-app/lib/validators/patient-validators.ts`

```typescript
import { z } from "zod";

export const createPatientSchema = z.object({
  email: z.string().email("Invalid email"),
  firstName: z.string().min(1, "Required").max(100),
  lastName: z.string().min(1, "Required").max(100),
  dateOfBirth: z.string().min(1, "Required"),  // ISO date string
  phoneNumber: z.string().max(20).optional().or(z.literal("")),
});

export const updatePatientSchema = z.object({
  firstName: z.string().min(1, "Required").max(100),
  lastName: z.string().min(1, "Required").max(100),
  phoneNumber: z.string().max(20).optional().or(z.literal("")),
});

export type CreatePatientInput = z.infer<typeof createPatientSchema>;
export type UpdatePatientInput = z.infer<typeof updatePatientSchema>;
```

### Step 2: Create mutation hooks

File: `client/doctor-app/lib/hooks/use-patients.ts`

Follow existing hook patterns in the project. Likely uses fetch or a wrapper.

```typescript
"use client";

import { useMutation } from "..."; // match existing pattern (SWR mutation, react-query, or custom)
import { useRouter } from "next/navigation";
import { toast } from "..."; // match existing toast import

export function useCreatePatient() {
  const router = useRouter();
  return useMutation(async (data: CreatePatientInput) => {
    const res = await fetch("/api/proxy/patients", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(data),
    });
    if (!res.ok) throw new Error("Failed to create patient");
    return res.json();
  }, {
    onSuccess: () => {
      toast.success("Patient created");
      router.push("/patients");
    },
    onError: () => toast.error("Failed to create patient"),
  });
}

export function useUpdatePatient(id: string) {
  const router = useRouter();
  return useMutation(async (data: UpdatePatientInput) => {
    const res = await fetch(`/api/proxy/patients/${id}`, {
      method: "PUT",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ id, ...data }),
    });
    if (!res.ok) throw new Error("Failed to update patient");
    return res.json();
  }, {
    onSuccess: () => {
      toast.success("Patient updated");
      router.push(`/patients/${id}`);
    },
    onError: () => toast.error("Failed to update patient"),
  });
}

export function useDeletePatient(id: string) {
  const router = useRouter();
  return useMutation(async () => {
    const res = await fetch(`/api/proxy/patients/${id}`, { method: "DELETE" });
    if (!res.ok) throw new Error("Failed to deactivate patient");
  }, {
    onSuccess: () => {
      toast.success("Patient deactivated");
      router.push("/patients");
    },
    onError: () => toast.error("Failed to deactivate patient"),
  });
}
```

**Important**: Before implementing, check existing hooks in `lib/hooks/` to match the exact mutation/fetch pattern used in the project (could be custom fetch wrapper, SWR, react-query, etc.).

### Step 3: Create patient-create-form component

File: `client/doctor-app/components/patients/patient-create-form.tsx`

- `"use client"` directive
- react-hook-form with zodResolver(createPatientSchema)
- Fields: email, firstName, lastName, dateOfBirth (date input), phoneNumber
- Submit button with loading state
- Cancel button → router.back()
- Follow profile-edit-form.tsx structure from patient-app

### Step 4: Create patient-edit-form component

File: `client/doctor-app/components/patients/patient-edit-form.tsx`

- `"use client"` directive
- Props: `patient: PatientDto` (pre-fill form)
- react-hook-form with zodResolver(updatePatientSchema)
- Fields: firstName, lastName, phoneNumber (email + DOB read-only display)
- Submit button with loading state
- Cancel button → router.back()

### Step 5: Create patient-delete-dialog component

File: `client/doctor-app/components/patients/patient-delete-dialog.tsx`

- Confirmation dialog using shadcn AlertDialog or Dialog
- Shows patient name, warns action is irreversible
- Confirm triggers useDeletePatient mutation
- Cancel closes dialog

### Step 6: Create new patient page

File: `client/doctor-app/app/(dashboard)/patients/new/page.tsx`

```tsx
import { PatientCreateForm } from "@/components/patients/patient-create-form";

export default function NewPatientPage() {
  return (
    <div className="container max-w-2xl py-6">
      <h1 className="text-2xl font-bold mb-6">New Patient</h1>
      <PatientCreateForm />
    </div>
  );
}
```

### Step 7: Create edit patient page

File: `client/doctor-app/app/(dashboard)/patients/[id]/edit/page.tsx`

- Server component that fetches patient data (same pattern as existing [id]/page.tsx)
- Passes patient data to PatientEditForm client component
- Shows loading/error states

### Step 8: Update PatientProfileView

Add to `patient-profile-view.tsx`:
- "Edit" button (Link to `/patients/${id}/edit`)
- "Deactivate" button (opens PatientDeleteDialog)
- Import and render PatientDeleteDialog with state management

### Step 9: Update PatientList

Add to `patient-list.tsx`:
- "New Patient" button at top of list (Link to `/patients/new`)
- Use Button component with Plus icon

### Step 10: Verify build

```bash
cd client/doctor-app && npm run build
```

## Todo List

- [x] Create patient-validators.ts (zod schemas)
- [x] Create use-patients.ts (mutation hooks)
- [x] Create patient-create-form.tsx
- [x] Create patient-edit-form.tsx
- [x] Create patient-delete-dialog.tsx
- [x] Create /patients/new page
- [x] Create /patients/[id]/edit page
- [x] Update PatientProfileView with Edit + Deactivate buttons
- [x] Update PatientList with "New Patient" button
- [x] Verify `npm run build` passes

## Success Criteria

- Doctor can create a new patient from /patients/new
- Doctor can edit patient details from /patients/:id/edit
- Doctor can deactivate patient from profile view with confirmation
- Forms validate input client-side matching backend rules
- Toast notifications on success/error
- Proper redirects after each action
- No build errors

## Risk Assessment

| Risk | Mitigation |
|------|------------|
| Hook pattern mismatch | Read existing hooks before implementing — adapt to project's fetch/mutation pattern |
| Toast import differs | Check existing toast usage in doctor-app or patient-app |
| PatientDto type may differ | Check existing TypeScript types/interfaces in lib/types |
| Date input format mismatch | Use ISO string format, verify backend expectation |

## Security Considerations

- All mutations go through proxy (auth token forwarded)
- Client-side validation is UX only — backend validates authoritatively
- Confirmation dialog prevents accidental deactivation
- No sensitive data exposed in client components beyond what's already shown
