# Phase 04 — Schedule & Appointments

**Status:** Complete
**Priority:** High
**Effort:** Medium
**Blocked by:** Phase 02

## Context Links

- Reference: `client/patient-app/app/(dashboard)/appointments/`
- Reference: `client/patient-app/components/appointments/`
- Reference: `client/patient-app/lib/hooks/use-appointments.ts`

## Overview

Doctor's schedule view (date-filtered list of their appointments) and appointment detail page
with status management (mark Complete, Cancel with reason).

## Related Code Files

**Create:**
- `client/doctor-app/app/(dashboard)/schedule/page.tsx`
- `client/doctor-app/app/(dashboard)/schedule/loading.tsx`
- `client/doctor-app/app/(dashboard)/appointments/page.tsx`
- `client/doctor-app/app/(dashboard)/appointments/loading.tsx`
- `client/doctor-app/app/(dashboard)/appointments/error.tsx`
- `client/doctor-app/app/(dashboard)/appointments/[id]/page.tsx`
- `client/doctor-app/app/(dashboard)/appointments/[id]/loading.tsx`
- `client/doctor-app/components/appointments/appointment-card.tsx`
- `client/doctor-app/components/appointments/appointment-list.tsx`
- `client/doctor-app/components/appointments/appointment-detail.tsx`
- `client/doctor-app/components/appointments/complete-dialog.tsx`
- `client/doctor-app/components/appointments/cancel-dialog.tsx`
- `client/doctor-app/components/schedule/date-filter.tsx`
- `client/doctor-app/lib/hooks/use-doctor-appointments.ts`

## Implementation Steps

### 1. TanStack Query hook (`lib/hooks/use-doctor-appointments.ts`)

```ts
interface DoctorAppointmentFilters {
  from?: string;      // ISO date
  to?: string;
  status?: string;
  page?: number;
}

export function useDoctorAppointments(filters?: DoctorAppointmentFilters) {
  return useQuery({
    queryKey: ["doctor-appointments", filters],
    queryFn: () => fetchAppointments(filters),
  });
}

export function useCompleteAppointment() { /* PATCH /appointments/:id/complete */ }
export function useCancelAppointment() { /* DELETE /appointments/:id */ }
```

### 2. Schedule page (`app/(dashboard)/schedule/page.tsx`)

Server Component — fetches this week's appointments for the logged-in doctor:
```
GET /api/appointments?providerId={doctorId}&from={weekStart}&to={weekEnd}
```
Renders `<DateFilter />` (client) + `<AppointmentList />`.

### 3. Appointments page (`app/(dashboard)/appointments/page.tsx`)

All appointments list with status filter tabs (All / Scheduled / Completed / Cancelled).
Uses TanStack Query for tab switching without page reload.

### 4. Appointment detail (`app/(dashboard)/appointments/[id]/page.tsx`)

Shows:
- Patient info (name, DOB)
- Scheduled time + duration
- Status badge
- Notes
- Action buttons:
  - **Mark Complete** → `complete-dialog.tsx` → PATCH `/api/appointments/:id` with `{ status: "Completed" }`
  - **Cancel** → `cancel-dialog.tsx` → DELETE `/api/appointments/:id` with `{ reason }`

### 5. Complete dialog (`components/appointments/complete-dialog.tsx`)

Simple confirmation dialog. On confirm calls `useCompleteAppointment()` mutation,
then redirects to `/appointments`.

### 6. Cancel dialog

Copy from patient-app `cancel-dialog.tsx` — same pattern.

### 7. Date filter (`components/schedule/date-filter.tsx`)

Client Component — date range picker (today / this week / custom) that updates query params,
triggering TanStack Query refetch.

## Todo

- [ ] Create lib/hooks/use-doctor-appointments.ts
- [ ] Create schedule/page.tsx + loading.tsx
- [ ] Create components/schedule/date-filter.tsx
- [ ] Create appointments/page.tsx (status tabs)
- [ ] Create appointments/loading.tsx + error.tsx
- [ ] Create appointments/[id]/page.tsx
- [ ] Create components/appointments/appointment-card.tsx
- [ ] Create components/appointments/appointment-list.tsx
- [ ] Create components/appointments/appointment-detail.tsx
- [ ] Create components/appointments/complete-dialog.tsx
- [ ] Create components/appointments/cancel-dialog.tsx

## Success Criteria

- Schedule page shows this week's appointments filtered by `providerId`
- Status tabs filter correctly without page reload
- Mark Complete and Cancel mutations update status and refresh list
- Date filter changes URL params and re-fetches data

## Security Considerations

- `providerId` sourced from server-side JWT (`session.user.id`) — never from query params
- Proxy allowlist covers `/appointments/:id/complete` and `/appointments/:id`

## Next Steps

→ Phase 05: Patient Management
