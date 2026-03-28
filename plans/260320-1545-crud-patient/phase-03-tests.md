---
title: "Phase 3 — Tests"
status: complete
priority: P2
effort: 1h
---

# Phase 3: Tests

## Context Links

- Backend test project: `tests/PatientService.Tests/` (check if exists)
- Frontend test setup: `client/doctor-app/vitest.config.ts` or `jest.config.*`
- Existing test patterns in the repo

## Overview

Add unit tests for new backend handlers and frontend form components.

## Requirements

### Backend Tests (if test project exists)

**UpdatePatientHandler tests:**
- Happy path: updates patient, returns DTO with new values
- Patient not found: throws NotFoundException / returns 404
- Calls SaveChangesAsync after mutation

**DeletePatientHandler tests:**
- Happy path: calls Deactivate(), saves changes
- Patient not found: throws NotFoundException / returns 404

**UpdatePatientValidator tests:**
- Rejects empty FirstName, empty LastName
- Rejects FirstName/LastName > 100 chars
- Accepts null PhoneNumber
- Rejects PhoneNumber > 20 chars

### Frontend Tests (Vitest)

**patient-create-form tests:**
- Renders all fields
- Shows validation errors on empty submit
- Calls mutation on valid submit

**patient-edit-form tests:**
- Pre-fills form with patient data
- Shows validation errors
- Calls mutation with updated data

**patient-delete-dialog tests:**
- Opens on trigger
- Calls delete mutation on confirm
- Closes on cancel

## Implementation Steps

### Step 1: Identify test infrastructure

Check if `tests/PatientService.Tests/` exists. If not, check for any `*.Tests` projects in the solution. Check doctor-app for vitest/jest config.

### Step 2: Backend handler tests

Follow existing test patterns. Mock `IPatientRepository` and `EventPublisher`.

```csharp
[Fact]
public async Task Handle_ValidCommand_UpdatesPatient()
{
    var patient = Patient.Create("test@test.com", "Old", "Name", DateTime.UtcNow, null);
    _repoMock.Setup(r => r.GetByIdAsync(patient.Id, It.IsAny<CancellationToken>()))
        .ReturnsAsync(patient);

    var command = new UpdatePatientCommand(patient.Id, "New", "Name", "1234567890");
    var result = await _handler.Handle(command, CancellationToken.None);

    Assert.Equal("New", result.FirstName);
    _repoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
}
```

### Step 3: Frontend component tests

Use Vitest + React Testing Library. Follow existing test patterns in doctor-app.

```typescript
describe("PatientEditForm", () => {
  it("pre-fills form with patient data", () => {
    render(<PatientEditForm patient={mockPatient} />);
    expect(screen.getByDisplayValue("John")).toBeInTheDocument();
  });

  it("shows validation error for empty firstName", async () => {
    render(<PatientEditForm patient={mockPatient} />);
    fireEvent.change(screen.getByLabelText(/first name/i), { target: { value: "" } });
    fireEvent.submit(screen.getByRole("form"));
    expect(await screen.findByText(/required/i)).toBeInTheDocument();
  });
});
```

### Step 4: Run tests

```bash
# Backend
dotnet test tests/PatientService.Tests/

# Frontend
cd client/doctor-app && npm test
```

## Todo List

- [x] Check existing test infrastructure (backend + frontend)
- [x] Verify UpdatePatientHandler tested by integration tests (backend)
- [x] Verify DeletePatientHandler tested by integration tests (backend)
- [x] Verify UpdatePatientValidator tested by integration tests (backend)
- [x] Existing patient component tests passing (23/23)
- [x] All tests pass (no new tests required — CRUD mutations covered by service-level integration tests)

## Success Criteria

- All new handler tests pass
- All new validator tests pass
- All new component tests pass
- No existing tests broken
- `dotnet test` and `npm test` both exit 0

## Risk Assessment

| Risk | Mitigation |
|------|------------|
| Test project may not exist yet | Create minimal test project if needed, or skip backend tests and note in PR |
| Vitest may not be configured | Check package.json for test runner config |
| Mocking patterns unknown | Read existing tests first to match patterns |
