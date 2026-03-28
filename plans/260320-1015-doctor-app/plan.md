# Plan: Doctor App (Frontend)

**Created:** 2026-03-20
**Status:** Complete
**Plan Dir:** `plans/260320-1015-doctor-app/`

## Overview

Doctor-facing Next.js 14 web app at `client/doctor-app/`. Mirrors patient-app patterns:
NextAuth v5 + Keycloak, TanStack Query, shadcn/ui (Radix UI + Tailwind), React Hook Form + Zod, Vitest.

Port: **3200** (patient-app is 3100).

## Key Differences from patient-app

| Aspect | patient-app | doctor-app |
|---|---|---|
| User role | patient | doctor/provider |
| Primary data | own appointments, records | all assigned patients, schedule |
| Mutations | schedule/cancel appointments | complete appointments, write records, issue prescriptions |
| Dashboard | health summary | today's workload |
| Proxy allowlist | patient-scoped paths | doctor-scoped paths |

## Phases

| # | Phase | Status |
|---|---|---|
| 1 | [Bootstrap & Project Setup](phase-01-bootstrap.md) | Complete |
| 2 | [Shared Infrastructure](phase-02-shared-infrastructure.md) | Complete |
| 3 | [Dashboard](phase-03-dashboard.md) | Complete |
| 4 | [Schedule & Appointments](phase-04-schedule-appointments.md) | Complete |
| 5 | [Patient Management](phase-05-patients.md) | Complete |
| 6 | [Medical Records & Prescriptions](phase-06-medical-records-prescriptions.md) | Complete |
| 7 | [Docker + Tests](phase-07-docker-tests.md) | Complete |

## Dependencies

- Gateway running at `GATEWAY_API_URL` (same as patient-app)
- Keycloak realm `hospital` with `doctor` role
- Appointment Service, Patient Service, Medical Record Service, Pharmacy Service endpoints

## Key Context Links

- Patient-app reference: `client/patient-app/`
- Gateway: `gateway/HospitalGateway/`
- Code standards: `docs/code-standards.md`
- System architecture: `docs/system-architecture.md`
