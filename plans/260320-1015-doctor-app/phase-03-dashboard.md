# Phase 03 — Dashboard

**Status:** Complete
**Priority:** High
**Effort:** Small
**Blocked by:** Phase 02

## Context Links

- Reference: `client/patient-app/app/(dashboard)/page.tsx`
- Reference: `client/patient-app/components/dashboard/`

## Overview

Doctor dashboard home page — shows today's workload at a glance:
today's appointment count, patients seen today, pending prescriptions, and upcoming appointments list.

## Related Code Files

**Create:**
- `client/doctor-app/app/(dashboard)/page.tsx`
- `client/doctor-app/app/(dashboard)/loading.tsx`
- `client/doctor-app/components/dashboard/stats-cards.tsx`
- `client/doctor-app/components/dashboard/todays-appointments.tsx`
- `client/doctor-app/components/dashboard/recent-patients.tsx`

## Implementation Steps

### `app/(dashboard)/page.tsx` (Server Component)

```tsx
export default async function DashboardPage() {
  const session = await getAuthSession();
  const doctorId = session?.user?.id ?? "";
  const today = new Date().toISOString().split("T")[0];

  const [appointments, prescriptions] = await Promise.all([
    callGatewayAPI<Appointment[]>(
      `/api/appointments?providerId=${doctorId}&from=${today}&to=${today}`
    ).catch(() => [] as Appointment[]),
    callGatewayAPI<Prescription[]>(
      `/api/prescriptions?providerId=${doctorId}&status=Pending`
    ).catch(() => [] as Prescription[]),
  ]);

  return (
    <div className="space-y-6">
      <PageHeader title="Dashboard" description="Your workload for today." />
      <StatsCards
        todayCount={appointments.length}
        completedCount={appointments.filter(a => a.status === "Completed").length}
        pendingPrescriptions={prescriptions.length}
      />
      <TodaysAppointments appointments={appointments} />
    </div>
  );
}
```

### `components/dashboard/stats-cards.tsx`

Three stat cards:
- **Today's Appointments** — total scheduled for today
- **Completed** — appointments marked Completed today
- **Pending Prescriptions** — prescriptions awaiting fulfillment

### `components/dashboard/todays-appointments.tsx`

List of today's appointments sorted by `scheduledTime`:
- Patient name (fetched or displayed by patientId)
- Scheduled time
- Status badge
- Link to `/appointments/[id]`

## Todo

- [ ] Create app/(dashboard)/page.tsx
- [ ] Create app/(dashboard)/loading.tsx
- [ ] Create components/dashboard/stats-cards.tsx
- [ ] Create components/dashboard/todays-appointments.tsx

## Success Criteria

- Dashboard renders with real data from gateway
- Stats show correct today-scoped counts
- Empty state shown when no appointments

## Next Steps

→ Phase 04: Schedule & Appointments
