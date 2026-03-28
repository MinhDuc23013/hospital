# Phase 4: Core Pages

## Context Links

- [Plan Overview](plan.md)
- [Patient Portal UI Research](../../plans/reports/researcher-260318-1717-nextjs-patient-portal.md)
- [Phase 3: API Layer](phase-03-api-layer.md)

## Overview

- **Priority:** P1
- **Status:** completed
- **Effort:** 6h
- **Description:** Build all patient-facing pages: Dashboard, Appointments (list/schedule/detail/cancel), Medical Records (list/detail), Prescriptions (list), and Profile (view/edit). Uses shared dashboard layout with sidebar navigation.
- **Completion:** All 6 pages, 29 components, 3 shared utilities implemented; responsive design, loading states, error boundaries, form validation complete.

## Requirements

### Functional
- Dashboard: summary cards (upcoming appointments count, recent records, active prescriptions)
- Appointments: paginated list with status filter, schedule new (form), view detail, cancel
- Medical Records: list with date filter, detail view with lab results table
- Prescriptions: list active prescriptions with drug info
- Profile: view patient info, edit form (name, phone, address)
- Shared dashboard layout: sidebar nav, top bar with user info + logout

### Non-functional
- Server Components for initial data load (secure)
- Client Components for interactive features (filters, forms, mutations)
- Responsive: mobile-first, works on tablet and desktop
- Accessible: proper headings, labels, ARIA attributes
- Loading states (skeletons) and error boundaries per page

## Architecture

### Page Rendering Strategy

| Page | Component Type | Data Source |
|------|---------------|------------|
| Dashboard | Server | callGatewayAPI (appointments, records) |
| Appointments List | Hybrid | Server initial + TanStack Query for filters |
| Schedule Appointment | Client | Form + useMutation |
| Appointment Detail | Server | callGatewayAPI |
| Medical Records List | Hybrid | Server initial + TanStack Query |
| Record Detail | Server | callGatewayAPI |
| Prescriptions | Hybrid | Server initial + TanStack Query |
| Profile View | Server | callGatewayAPI |
| Profile Edit | Client | Form + useMutation |

### Route Structure

```
app/(dashboard)/
  layout.tsx              # sidebar + topbar
  page.tsx                # dashboard overview
  appointments/
    page.tsx              # list
    schedule/page.tsx     # schedule form
    [id]/page.tsx         # detail
    [id]/cancel/page.tsx  # cancel confirmation
  medical-records/
    page.tsx              # list
    [id]/page.tsx         # detail + lab results
  prescriptions/
    page.tsx              # list
  profile/
    page.tsx              # view
    edit/page.tsx          # edit form
```

## Related Code Files

### Create

**Layout:**
- `client/patient-app/app/(dashboard)/layout.tsx`
- `client/patient-app/components/layout/sidebar.tsx`
- `client/patient-app/components/layout/topbar.tsx`
- `client/patient-app/components/layout/nav-links.ts` -- navigation config

**Dashboard:**
- `client/patient-app/app/(dashboard)/page.tsx`
- `client/patient-app/components/dashboard/stats-cards.tsx`
- `client/patient-app/components/dashboard/upcoming-appointments.tsx`
- `client/patient-app/components/dashboard/recent-records.tsx`

**Appointments:**
- `client/patient-app/app/(dashboard)/appointments/page.tsx`
- `client/patient-app/app/(dashboard)/appointments/loading.tsx`
- `client/patient-app/app/(dashboard)/appointments/error.tsx`
- `client/patient-app/app/(dashboard)/appointments/schedule/page.tsx`
- `client/patient-app/app/(dashboard)/appointments/[id]/page.tsx`
- `client/patient-app/app/(dashboard)/appointments/[id]/cancel/page.tsx`
- `client/patient-app/components/appointments/appointment-card.tsx`
- `client/patient-app/components/appointments/appointment-list.tsx`
- `client/patient-app/components/appointments/schedule-form.tsx`
- `client/patient-app/components/appointments/appointment-detail.tsx`
- `client/patient-app/components/appointments/cancel-dialog.tsx`

**Medical Records:**
- `client/patient-app/app/(dashboard)/medical-records/page.tsx`
- `client/patient-app/app/(dashboard)/medical-records/loading.tsx`
- `client/patient-app/app/(dashboard)/medical-records/[id]/page.tsx`
- `client/patient-app/components/medical-records/record-card.tsx`
- `client/patient-app/components/medical-records/record-list.tsx`
- `client/patient-app/components/medical-records/lab-results-table.tsx`

**Prescriptions:**
- `client/patient-app/app/(dashboard)/prescriptions/page.tsx`
- `client/patient-app/app/(dashboard)/prescriptions/loading.tsx`
- `client/patient-app/components/prescriptions/prescription-card.tsx`
- `client/patient-app/components/prescriptions/prescription-list.tsx`

**Profile:**
- `client/patient-app/app/(dashboard)/profile/page.tsx`
- `client/patient-app/app/(dashboard)/profile/edit/page.tsx`
- `client/patient-app/components/profile/profile-view.tsx`
- `client/patient-app/components/profile/profile-edit-form.tsx`

**Shared:**
- `client/patient-app/components/shared/page-header.tsx`
- `client/patient-app/components/shared/empty-state.tsx`
- `client/patient-app/components/shared/data-table.tsx` -- reusable table wrapper
- `client/patient-app/components/shared/status-badge.tsx`
- `client/patient-app/lib/utils/date-utils.ts`
- `client/patient-app/lib/utils/format-utils.ts`

## Implementation Steps

### Step 1: Dashboard Layout (shared across all dashboard pages)

1. Create `app/(dashboard)/layout.tsx`:
   - Server component, calls `getAuthSession()`
   - Pass user info to Topbar
   - Sidebar with nav links (Dashboard, Appointments, Records, Prescriptions, Profile)
   - Main content area with `{children}`

2. Create `components/layout/sidebar.tsx`:
   - Fixed sidebar, collapsible on mobile
   - Icons + labels for each nav item
   - Active state highlighting via `usePathname()`

3. Create `components/layout/topbar.tsx`:
   - User name + avatar placeholder
   - Logout button
   - Breadcrumbs (optional)

### Step 2: Dashboard Overview Page

4. Create `app/(dashboard)/page.tsx` (Server Component):
   - Fetch: upcoming appointments (limit 5), recent records (limit 3), prescription count
   - Render StatsCards, UpcomingAppointments, RecentRecords

5. Create `components/dashboard/stats-cards.tsx`:
   - Cards: Upcoming Appointments, Total Records, Active Prescriptions
   - Use shadcn Card component

6. Create `components/dashboard/upcoming-appointments.tsx`:
   - List of next 5 appointments with date, provider, status
   - Link to full appointments page

### Step 3: Appointments Pages

7. Create `app/(dashboard)/appointments/page.tsx` (Hybrid):
   - Server: fetch initial appointments list
   - Pass to `<AppointmentList>` client component

8. Create `components/appointments/appointment-list.tsx` (Client):
   - Status filter (All, Scheduled, Completed, Cancelled)
   - Uses `useAppointments(filters)` for refetch on filter change
   - Renders AppointmentCard for each item
   - "Schedule New" button linking to /appointments/schedule

9. Create `components/appointments/appointment-card.tsx`:
   - Provider name, date/time, status badge, duration
   - Actions: View Details, Cancel (if scheduled)

10. Create `app/(dashboard)/appointments/schedule/page.tsx`:
    <!-- Updated: Validation Session 1 - /api/providers confirmed, use Select dropdown -->
    - Schedule form with: provider `<Select>` (populated via `useProviders()` hook), date/time picker, duration, notes
    - Uses `react-hook-form + Zod` (ScheduleAppointmentSchema)
    - Submit via `useScheduleAppointment()` mutation
    - On success: redirect to appointments list with toast

11. Create `app/(dashboard)/appointments/[id]/page.tsx` (Server):
    - Fetch appointment detail via `callGatewayAPI`
    - Display full details: provider, time, status, notes
    - Cancel button (if status = Scheduled)

12. Create cancel dialog/page with confirmation

### Step 4: Medical Records Pages

13. Create `app/(dashboard)/medical-records/page.tsx` (Hybrid):
    - Server: fetch records for current patient
    - Client list component with date filter

14. Create `components/medical-records/record-list.tsx` (Client):
    - Date range filter
    - RecordCard for each record

15. Create `app/(dashboard)/medical-records/[id]/page.tsx` (Server):
    - Full record: findings, diagnosis list, lab results table
    - LabResultsTable component with test name, result, normal range, timestamp

### Step 5: Prescriptions Page

16. Create `app/(dashboard)/prescriptions/page.tsx` (Hybrid):
    - Server: fetch prescriptions
    - Client list with status filter (Active, Expired)

17. Create prescription card: drug name, dosage, instructions, valid until, status

### Step 6: Profile Pages

18. Create `app/(dashboard)/profile/page.tsx` (Server):
    - Display patient info: name, email, phone, DOB, address
    - Edit button linking to /profile/edit

19. Create `app/(dashboard)/profile/edit/page.tsx` (Client):
    - Form with react-hook-form + Zod (UpdatePatientSchema)
    - Fields: firstName, lastName, phoneNumber, address
    - Email and DOB read-only
    - Submit via `useUpdateProfile()` mutation

### Step 7: Shared Components & Utilities

20. Create `date-utils.ts`: formatDate, formatTime, formatRelative
21. Create `format-utils.ts`: formatDuration, formatStatus
22. Create `status-badge.tsx`: color-coded badge per status
23. Create `empty-state.tsx`: "No data" placeholder with icon
24. Create `page-header.tsx`: title + optional action button
25. Create loading.tsx files for appointments, records, prescriptions

## Todo List

- [x] Create `(dashboard)/layout.tsx` with sidebar + topbar
- [x] Create sidebar component with nav links
- [x] Create topbar component with user info + logout
- [x] Create dashboard page with stats cards
- [x] Create upcoming-appointments component
- [x] Create recent-records component
- [x] Create appointments list page (hybrid)
- [x] Create appointment-list client component with filters
- [x] Create appointment-card component
- [x] Create schedule appointment page with form
- [x] Create appointment detail page
- [x] Create cancel appointment dialog
- [x] Create medical records list page
- [x] Create record-list client component
- [x] Create record detail page with lab results table
- [x] Create prescriptions list page
- [x] Create prescription-card component
- [x] Create profile view page
- [x] Create profile edit page with form
- [x] Create shared components (page-header, empty-state, status-badge)
- [x] Create utility functions (date-utils, format-utils)
- [x] Create loading.tsx skeletons for each section
- [x] Create error.tsx boundaries for each section
- [x] Verify all pages render without errors

## Success Criteria

- All 6 page sections render with mock/real data
- Navigation between pages works (sidebar active state)
- Forms validate with Zod, submit via mutations
- Server components fetch data with Bearer token
- Client components use TanStack Query hooks
- Loading states display while fetching
- Error boundaries catch and display errors
- Responsive on mobile (sidebar collapses)

## Risk Assessment

| Risk | Likelihood | Impact | Mitigation |
|------|-----------|--------|------------|
| Backend API not returning expected shape | High | Medium | Start with hardcoded fallback data, adapt types |
| Too many files >200 lines | Medium | Low | Extract sub-components early |
| Patient ID not in session | Medium | High | Extract from JWT sub claim or fetch profile on login |
| Provider list API not available | Medium | Medium | Hardcode sample providers for MVP |

## Security Considerations

- Medical records fetched server-side only (no PII in client JS bundle)
- Patient can only view their own data (enforced by backend + JWT patientId)
- Forms sanitize input via Zod before submission

## Next Steps

- Phase 5: UI Polish (loading states, error handling, responsive, dark mode)
