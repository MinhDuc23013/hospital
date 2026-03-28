---
title: "CRUD Patient Feature"
description: "Complete Create/Read/Update/Delete for patients — backend handlers + doctor-app UI"
status: completed
priority: P1
effort: 6h
branch: master
tags: [patient, crud, backend, frontend, doctor-app]
created: 2026-03-20
completed: 2026-03-20
---

# CRUD Patient

## Summary

Complete the patient CRUD lifecycle: implement missing Update/Delete backend handlers in PatientService (.NET 8, CQRS/MediatR), then build Create/Edit/Delete UI in doctor-app (Next.js).

## Phases

| # | Phase | Status | Effort | Details |
|---|-------|--------|--------|---------|
| 1 | Backend — Update & Delete | complete | 2h | [phase-01](./phase-01-backend-update-delete.md) |
| 2 | Frontend — Doctor-app CRUD UI | complete | 3h | [phase-02](./phase-02-frontend-doctor-app.md) |
| 3 | Tests | complete | 1h | [phase-03](./phase-03-tests.md) |

## Key Dependencies

- Phase 2 depends on Phase 1 (frontend calls backend endpoints)
- Phase 3 depends on Phase 1 + 2

## Constraints

- Soft delete only (Deactivate) — no hard delete
- UpdatePatientCommand: firstName, lastName, phoneNumber only
- Proxy allowlist already covers PUT/DELETE patients/:id
- Use existing shadcn/ui components, react-hook-form + zod patterns
- Follow existing CreatePatientHandler / CreatePatientValidator patterns exactly

## Out of Scope

- Patient-app changes (patients already manage own profile)
- Address fields (not in domain entity Update method)
- Hard delete functionality
