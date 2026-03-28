# Phase Implementation Report

## Executed Phase
- Phase: phase-03-api-layer
- Plan: plans/260318-1719-nextjs-patient-client-app/
- Status: completed

## Files Modified

| File | Lines | Action |
|------|-------|--------|
| `client/patient-app/lib/types/api-response.ts` | 20 | created |
| `client/patient-app/lib/types/patient.ts` | 14 | created |
| `client/patient-app/lib/types/appointment.ts` | 16 | created |
| `client/patient-app/lib/types/medical-record.ts` | 26 | created |
| `client/patient-app/lib/types/prescription.ts` | 14 | created |
| `client/patient-app/lib/types/provider.ts` | 10 | created |
| `client/patient-app/lib/types/index.ts` | 8 | created |
| `client/patient-app/lib/api-client.ts` | 68 | created |
| `client/patient-app/app/api/auth/token/route.ts` | 18 | created |
| `client/patient-app/app/api/proxy/[...path]/route.ts` | 75 | created |
| `client/patient-app/lib/hooks/use-appointments.ts` | 82 | created |
| `client/patient-app/lib/hooks/use-medical-records.ts` | 36 | created |
| `client/patient-app/lib/hooks/use-prescriptions.ts` | 22 | created |
| `client/patient-app/lib/hooks/use-patient-profile.ts` | 52 | created |
| `client/patient-app/lib/hooks/use-providers.ts` | 24 | created |
| `client/patient-app/lib/validators/appointment-schema.ts` | 22 | created |
| `client/patient-app/lib/validators/patient-schema.ts` | 22 | created |
| `client/patient-app/lib/validators/index.ts` | 14 | created |
| `client/patient-app/lib/utils/date-utils.ts` | 48 | created |
| `client/patient-app/lib/utils/format-utils.ts` | 18 | created |
| `client/patient-app/package.json` | 55 | modified — removed non-existent @radix-ui/react-calendar, added @radix-ui/react-popover, removed duplicate @radix-ui/react-label |
| `client/patient-app/lib/auth-config.ts` | 1 line | modified — fixed pre-existing TS2352 cast (`as Record` → `as unknown as Record`) |

## Tasks Completed

- [x] TypeScript types: api-response, patient, appointment, medical-record, prescription, provider, index barrel
- [x] Server-side API client (callGatewayAPI with AuthError/GatewayError, Bearer injection, cache:no-store)
- [x] GET /api/auth/token route (idToken only, for Keycloak SSO logout — satisfies logout-button.tsx Phase 2 dependency)
- [x] GET/POST/PUT/DELETE /api/proxy/[...path] with ALLOWED_PATHS allowlist (AUDIT FIX F2)
- [x] TanStack Query hooks: use-appointments, use-medical-records, use-prescriptions, use-patient-profile, use-providers
- [x] Zod schemas: ScheduleAppointmentSchema, CancelAppointmentSchema, UpdatePatientSchema + index barrel
- [x] date-utils: formatDate, formatTime, formatRelative, formatDuration (ISO 8601 → human-readable)
- [x] format-utils: formatStatus (PascalCase → spaced), formatName
- [x] QueryClientProvider + app/layout.tsx already done in Phase 2 (confirmed — no action needed)
- [x] Fixed package.json: @radix-ui/react-calendar@0.0.1 does not exist in npm registry
- [x] tsc --noEmit: clean (0 errors)

## Tests Status
- Type check: PASS (tsc --noEmit — 0 errors after fixing pre-existing cast in auth-config.ts)
- Unit tests: N/A (no test runner configured in this phase)
- Integration tests: N/A

## Issues Encountered

1. **node_modules absent** — deps not installed, `npm install` blocked by `@radix-ui/react-calendar@^0.0.1` (package does not exist in npm registry). Fixed by replacing with `@radix-ui/react-popover@^1.1.4` and removing the duplicate `@radix-ui/react-label` entry.

2. **Pre-existing TS2352 in auth-config.ts** — `session as Record<string, unknown>` fails because the intersection type lacks an index signature. Fixed with `session as unknown as Record<string, unknown>`. This was a Phase 2 file; the fix is a one-liner with no behavioral change.

3. **QueryClientProvider already exists** — `components/providers/query-client-provider.tsx` and root layout wrapping were already implemented in Phase 2. No duplicate created.

4. **idToken shape in token route** — `auth()` returns a `Session` object; `idToken` lives in the JWT layer (set in jwt callback). Cast via `session as unknown as { idToken?: string }` to access it, consistent with how auth-session.ts accesses `accessToken`.

## Next Steps

- Phase 4: Core Pages can now import from `@/lib/types`, `@/lib/hooks/*`, `@/lib/validators`, `@/lib/api-client`, `@/lib/utils/*`
- Server Components: use `callGatewayAPI<T>()` directly for initial page loads
- Client Components: use TanStack Query hooks (only for interactive filters / mutations)
- GATEWAY_API_URL env var must be set in `.env.local` (server-only, no NEXT_PUBLIC_ prefix)

## Unresolved Questions

None.
