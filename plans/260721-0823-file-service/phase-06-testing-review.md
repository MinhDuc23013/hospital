# Phase 06 — Testing & Review

## Context Links
- Overview: [plan.md](plan.md); depends on [phase-05-infra-deployment.md](phase-05-infra-deployment.md)

## Overview
- Priority: P2
- Status: pending
- Validate upload→store→download round-trip, validation rules, auth, then code review.

## Key Insights
- Repo uses `EnsureCreated` + shared Postgres — integration tests should hit a real DB (no mocking DB per project convention). Unit-test `FileValidationService` in isolation.

## Requirements
- Unit: `FileValidationService` (oversize reject, disallowed type/extension reject, filename sanitize, happy path).
- Integration: upload valid file → 201; download → bytes+filename match (SHA-256 equality); list excludes content; delete → soft-deleted, subsequent GET 404; unauthorized role → 403; oversize → 400/413.

## Related Code Files
Create:
- `services/FileService/Tests/...` OR follow existing repo test layout (detect sibling test project convention first).
- `services/FileService/FileService.http` (manual smoke, mirror PatientService.http)

## Implementation Steps
1. Detect existing test project pattern in repo; if none, add minimal xUnit project or rely on `.http` smoke + manual (confirm with lead).
2. Write `FileValidationService` unit tests.
3. Write round-trip integration test (upload/download SHA match).
4. Delegate to `tester` agent; fix failures; re-run until green.
5. Delegate to `code-reviewer` agent on final code.

## Todo List
- [ ] FileValidationService unit tests
- [ ] Upload/download round-trip integration test
- [ ] Auth (403) + oversize (400) tests
- [ ] FileService.http smoke file
- [ ] tester agent green
- [ ] code-reviewer pass

## Success Criteria
- All tests pass; round-trip byte-identical; validation + auth enforced; reviewer approves.

## Risk Assessment
- No existing test infra → scope creep. Mitigate: confirm test approach with lead before building test project.

## Security Considerations
- Include negative tests: path-traversal filename, disallowed executable type, unauth access.

## Next Steps
- Merge; update roadmap/changelog status to completed.
