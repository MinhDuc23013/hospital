---
title: "Pharmacy Batch Tracking, Stock Reservation & Audit"
description: "Add batch/lot tracking (FEFO), dispensing saga with stock reservation, concurrency control, and compliance audit logging to PharmacyServiceDotnet"
status: pending
priority: P1
effort: 12h
branch: dev
tags: [pharmacy, inventory, saga, audit, batch-tracking]
created: 2026-03-30
---

# Pharmacy Batch Tracking, Stock Reservation & Audit

## Goal

Upgrade PharmacyServiceDotnet from flat stock counting to batch-aware inventory with FEFO dispensing, saga-based stock reservation, optimistic concurrency, and immutable audit trail.

## Current State

- `Drug.Quantity` is a single flat int -- no batch/lot/expiry tracking
- `DispensePrescriptionHandler` directly decrements stock -- no reservation, no concurrency control, no audit
- No saga orchestration in Pharmacy (exists in AppointmentService as reference)
- Outbox pattern already wired (`EventPublisher` extends `OutboxEventPublisher`)

## Phases

| # | Phase | Priority | Effort | Status |
|---|-------|----------|--------|--------|
| 1 | [Domain Entities](phase-01-domain-entities.md) | P1 | 2h | pending |
| 2 | [Repositories & Migration](phase-02-repositories-and-migration.md) | P1 | 2h | pending |
| 3 | [Batch Stock Management](phase-03-batch-stock-management.md) | P1 | 2h | pending |
| 4 | [Dispensing Saga](phase-04-dispensing-saga.md) | P1 | 3h | pending |
| 5 | [Audit & Events](phase-05-audit-and-events.md) | P2 | 1.5h | pending |
| 6 | [Concurrency & Expiry Worker](phase-06-concurrency-and-expiry.md) | P2 | 1.5h | pending |

## Dependencies

- Phase 2 depends on Phase 1 (entities must exist before repos/config)
- Phase 3 depends on Phase 2 (repos needed for FEFO logic)
- Phase 4 depends on Phase 3 (saga uses batch stock operations)
- Phase 5 depends on Phase 1 (audit entity)
- Phase 6 depends on Phase 2 + 4 (concurrency on DrugBatch, expiry on reservations)

## Out of Scope

- Downstream consumers (MedicalRecordService, NotificationService, SearchService)
- UI/frontend changes
- Batch receiving/purchase order workflow
