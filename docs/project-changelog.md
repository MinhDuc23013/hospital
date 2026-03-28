# Hospital HRM Microservices — Project Changelog

**Format:** Semantic Versioning (MAJOR.MINOR.PATCH)
**Last Updated:** 2026-03-20

---

## [1.1.1] — 2026-03-20

### Added

#### CRUD Patient Management Feature (Doctor Portal)
- **Backend (PatientService)**
  - Added `UpdateAsync` to `IPatientRepository` interface for consistent repository pattern
  - Implemented `UpdatePatientHandler` with CQRS/MediatR for atomic updates
  - Implemented `UpdatePatientValidator` using FluentValidation (firstName, lastName, phoneNumber constraints)
  - Implemented `DeletePatientCommand` and `DeletePatientHandler` for soft-delete operations
  - Added PUT `/api/patients/{id}` endpoint for updating patient records (returns 200 with PatientDto)
  - Added complete DELETE `/api/patients/{id}` endpoint implementation (returns 204 NoContent)
  - Both endpoints validate patient existence (return 404 if not found)
  - Route ID validation prevents IDOR attacks

- **Frontend (Doctor Portal — doctor-app)**
  - Added `patient-validators.ts` with createPatientSchema and updatePatientSchema (Zod validation)
  - Added `use-patients.ts` hooks: useCreatePatient, useUpdatePatient, useDeletePatient
  - Added `patient-create-form.tsx` component for creating new patients
  - Added `patient-edit-form.tsx` component for editing patient details
  - Added `patient-delete-dialog.tsx` confirmation dialog component
  - Added `/patients/new` page for patient creation
  - Added `/patients/[id]/edit` page for patient editing
  - Added "Edit" and "Deactivate" buttons to patient profile view
  - Added "New Patient" button to patient list view
  - All mutations include success/error toast notifications
  - Smart redirects after mutations (create → list, edit → profile, delete → list)

### Changed

- **PatientService Controller:** Replaced DELETE endpoint stub with fully functional soft-delete handler
- **Doctor-app Patient Profile:** Enhanced with CRUD management capabilities (Edit, Deactivate actions)
- **Doctor-app Patient List:** Added "New Patient" button for creating patients directly from list view

### Technical Details

- **Backend Architecture:** CQRS/MediatR pattern with FluentValidation and domain-driven design
- **Soft Delete:** Uses IsActive flag for compliance with audit requirements
- **Validation:** Dual-layer validation (client-side Zod schemas, server-side FluentValidation)
- **API Security:** Route ID matching, proxy allowlist validation, 404 for missing resources
- **Testing:** All existing 23 frontend unit tests passing; backend changes covered by service-level integration tests

### Quality Metrics

- Backend compilation: ✓ 0 errors, 0 warnings
- Frontend build: ✓ 0 errors, 0 warnings
- Frontend tests: ✓ 23/23 passing
- Code coverage: ✓ Maintained (no regressions)

### Breaking Changes

None — Feature is additive to existing CRUD functionality.

### Dependencies

- PatientService (.NET 8, EF Core, FluentValidation)
- Doctor-app (Next.js 14, React Hook Form, Zod, shadcn/ui)
- API Gateway (YARP proxy with allowlist)

---

## [1.1.0] — 2026-03-19

### Added

#### Doctor Portal (Phase 8)
- Full Next.js 14 doctor portal with responsive UI
- Appointment management (list, detail, mark complete, cancel)
- Medical record creation and viewing
- Prescription issuance and tracking
- Patient directory (read-only list view)
- NextAuth v5 with Keycloak OIDC integration
- Secure API proxy with allowlist pattern
- Token refresh with mutex pattern
- 23 unit tests with >80% coverage

### Changed

- Unified portal architecture (patient-app + doctor-app share authentication patterns)
- Enhanced patient list UI in both portals

### Technical Details

- Port: 3200
- Framework: Next.js 14 App Router
- Auth: NextAuth v5 + Keycloak (`doctor-app` client)
- Testing: Vitest + React Testing Library + JSDOM

---

## [1.0.0] — 2026-03-18

### Infrastructure & Services (Phase 1-6)

- **Infrastructure (Phase 1):** Docker Compose setup with PostgreSQL, MongoDB, SQL Server, RabbitMQ, Redis, Elasticsearch, Keycloak, Prometheus, Grafana, Seq
- **Gateway (Phase 2):** YARP API Gateway with JWT validation and rate limiting
- **.NET Services (Phase 3):** PatientService and AppointmentService with CQRS/MediatR
- **Node.js Services (Phase 4):** MedicalRecordService, PharmacyService, NotificationService, SearchService
- **Observability (Phase 5-6):** Prometheus metrics, Grafana dashboards, Elasticsearch logging, Seq log aggregation

### Patient Portal (Phase 7)

- Next.js 14 patient web portal
- Appointment management (book, view, cancel)
- Medical record access
- Prescription tracking
- Profile management
- NextAuth v5 + Keycloak OIDC
- 40 unit tests with comprehensive coverage

---

## Version History Summary

| Version | Date | Focus | Status |
|---------|------|-------|--------|
| 1.1.1 | 2026-03-20 | CRUD Patient Management | Complete |
| 1.1.0 | 2026-03-19 | Doctor Portal | Complete |
| 1.0.0 | 2026-03-18 | Infrastructure + Core Services + Patient Portal | Complete |

---

## Migration Guide

### Upgrading from 1.1.0 to 1.1.1

**Backend:**
1. Run database migrations (no schema changes)
2. Redeploy PatientService with new handlers
3. No configuration changes required

**Frontend:**
1. Update doctor-app with new components and validators
2. Clear Next.js cache if needed
3. Test patient CRUD workflow end-to-end

**No breaking changes** — Feature is fully backward compatible.

---

## Known Issues & Limitations

- None currently documented

---

## Future Roadmap

- **Phase 9:** Admin Portal (user management, system configuration)
- **Phase 10:** Mobile App (patient and doctor mobile clients)
- **Phase 11:** Advanced Scheduling (recurring appointments, slot optimization)
- **Phase 12:** Telemedicine Integration (video consultation support)

---

## Contributors

- Hospital HRM Project Team
- Architecture & Development Team
- QA & Testing Team

---

## License

Internal Project — Hospital HRM Microservices

