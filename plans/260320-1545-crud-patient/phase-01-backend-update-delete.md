---
title: "Phase 1 — Backend Update & Delete Handlers"
status: complete
priority: P1
effort: 2h
---

# Phase 1: Backend — Update & Delete

## Context Links

- Domain entity: `services/PatientService/Domain/Entities/Patient.cs` — has `Update()` and `Deactivate()`
- Existing handler pattern: `services/PatientService/Application/Handlers/CreatePatientHandler.cs`
- Existing validator: `services/PatientService/Application/Validators/CreatePatientValidator.cs`
- Repository: `services/PatientService/Infrastructure/Repositories/PatientRepository.cs`
- Interface: `services/PatientService/Domain/Interfaces/IPatientRepository.cs`
- Controller: `services/PatientService/API/Controllers/PatientsController.cs`
- Existing command: `services/PatientService/Application/Commands/UpdatePatientCommand.cs`

## Overview

Complete the backend CRUD by implementing UpdatePatientHandler, DeletePatientHandler, wiring controller endpoints, and adding FluentValidation for update.

## Key Insights

- EF Core change tracking means no explicit `UpdateAsync` is strictly needed — load entity, mutate, SaveChangesAsync. However, adding `UpdateAsync` to the repo interface keeps the pattern consistent.
- `Patient.Update(firstName, lastName, phoneNumber)` already exists on the domain entity.
- `Patient.Deactivate()` sets `IsActive = false` (soft delete). Existing queries filter by `IsActive`.
- `UpdatePatientCommand` record already exists — just needs a handler.
- DELETE endpoint exists as stub returning NoContent — needs actual logic.

## Requirements

### Functional
- PUT /api/patients/{id} — update firstName, lastName, phoneNumber; return updated PatientDto
- DELETE /api/patients/{id} — soft-delete (deactivate); return 204 NoContent
- Return 404 if patient not found for both endpoints

### Non-functional
- Follow existing CQRS/MediatR handler pattern
- Validate input with FluentValidation
- Publish domain events after mutations (follow CreatePatientHandler pattern)

## Architecture

```
Controller (PUT/DELETE) → MediatR → Handler → Repository → EF Core → DB
                                      ↓
                                EventPublisher
```

## Related Code Files

### Files to modify
- `services/PatientService/Domain/Interfaces/IPatientRepository.cs` — add UpdateAsync
- `services/PatientService/Infrastructure/Repositories/PatientRepository.cs` — implement UpdateAsync
- `services/PatientService/API/Controllers/PatientsController.cs` — wire PUT handler, fix DELETE stub

### Files to create
- `services/PatientService/Application/Handlers/UpdatePatientHandler.cs`
- `services/PatientService/Application/Validators/UpdatePatientValidator.cs`
- `services/PatientService/Application/Commands/DeletePatientCommand.cs`
- `services/PatientService/Application/Handlers/DeletePatientHandler.cs`

## Implementation Steps

### Step 1: Add UpdateAsync to repository

1. In `IPatientRepository.cs`, add:
   ```csharp
   Task UpdateAsync(Patient patient, CancellationToken ct = default);
   ```

2. In `PatientRepository.cs`, implement:
   ```csharp
   public Task UpdateAsync(Patient patient, CancellationToken ct = default)
   {
       _context.Patients.Update(patient);
       return Task.CompletedTask;
   }
   ```
   Note: This marks entity as modified for EF Core tracking. Alternatively, if entity is already tracked (loaded via GetByIdAsync), just calling SaveChangesAsync suffices. The explicit `Update()` call handles detached entities.

### Step 2: Create UpdatePatientHandler

File: `services/PatientService/Application/Handlers/UpdatePatientHandler.cs`

```csharp
public class UpdatePatientHandler : IRequestHandler<UpdatePatientCommand, PatientDto>
{
    private readonly IPatientRepository _repo;
    private readonly EventPublisher _events;

    public UpdatePatientHandler(IPatientRepository repo, EventPublisher events)
    {
        _repo = repo;
        _events = events;
    }

    public async Task<PatientDto> Handle(UpdatePatientCommand request, CancellationToken ct)
    {
        var patient = await _repo.GetByIdAsync(request.Id, ct)
            ?? throw new NotFoundException($"Patient {request.Id} not found");

        patient.Update(request.FirstName, request.LastName, request.PhoneNumber);
        await _repo.SaveChangesAsync(ct);

        // Publish event if pattern exists in CreatePatientHandler
        // await _events.PublishAsync(new PatientUpdatedEvent(patient.Id), ct);

        return MapToDto(patient);
    }

    private static PatientDto MapToDto(Patient patient) => new(
        patient.Id, patient.Email, patient.FirstName, patient.LastName,
        patient.DateOfBirth, patient.PhoneNumber, patient.CreatedAt, patient.UpdatedAt
    );
}
```

**Important**: Check CreatePatientHandler for exact MapToDto signature and event publishing pattern. Mirror it.

### Step 3: Create UpdatePatientValidator

File: `services/PatientService/Application/Validators/UpdatePatientValidator.cs`

```csharp
public class UpdatePatientValidator : AbstractValidator<UpdatePatientCommand>
{
    public UpdatePatientValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.PhoneNumber).MaximumLength(20).When(x => x.PhoneNumber != null);
    }
}
```

### Step 4: Create DeletePatientCommand

File: `services/PatientService/Application/Commands/DeletePatientCommand.cs`

```csharp
public record DeletePatientCommand(Guid Id) : IRequest;
```

### Step 5: Create DeletePatientHandler

File: `services/PatientService/Application/Handlers/DeletePatientHandler.cs`

```csharp
public class DeletePatientHandler : IRequestHandler<DeletePatientCommand>
{
    private readonly IPatientRepository _repo;
    private readonly EventPublisher _events;

    public DeletePatientHandler(IPatientRepository repo, EventPublisher events)
    {
        _repo = repo;
        _events = events;
    }

    public async Task Handle(DeletePatientCommand request, CancellationToken ct)
    {
        var patient = await _repo.GetByIdAsync(request.Id, ct)
            ?? throw new NotFoundException($"Patient {request.Id} not found");

        patient.Deactivate();
        await _repo.SaveChangesAsync(ct);

        // Publish event if pattern exists
        // await _events.PublishAsync(new PatientDeactivatedEvent(patient.Id), ct);
    }
}
```

### Step 6: Wire controller endpoints

In `PatientsController.cs`:

**PUT endpoint** — add or update:
```csharp
[HttpPut("{id:guid}")]
public async Task<ActionResult<PatientDto>> Update(Guid id, UpdatePatientCommand command)
{
    if (id != command.Id)
        return BadRequest("Route ID and body ID mismatch");

    var result = await _mediator.Send(command);
    return Ok(result);
}
```

**DELETE endpoint** — replace stub:
```csharp
[HttpDelete("{id:guid}")]
public async Task<IActionResult> Delete(Guid id)
{
    await _mediator.Send(new DeletePatientCommand(id));
    return NoContent();
}
```

### Step 7: Verify compilation

```bash
dotnet build services/PatientService/
```

## Todo List

- [x] Add `UpdateAsync` to `IPatientRepository`
- [x] Implement `UpdateAsync` in `PatientRepository`
- [x] Create `UpdatePatientHandler.cs`
- [x] Create `UpdatePatientValidator.cs`
- [x] Create `DeletePatientCommand.cs`
- [x] Create `DeletePatientHandler.cs`
- [x] Wire PUT endpoint in PatientsController
- [x] Fix DELETE stub in PatientsController
- [x] Verify `dotnet build` passes

## Success Criteria

- PUT /api/patients/{id} returns updated PatientDto with 200
- PUT /api/patients/{id} returns 404 for nonexistent patient
- PUT /api/patients/{id} returns 400 for validation failures
- DELETE /api/patients/{id} returns 204, patient becomes inactive
- DELETE /api/patients/{id} returns 404 for nonexistent patient
- Deactivated patients no longer appear in GET list queries
- Solution compiles without errors

## Risk Assessment

| Risk | Mitigation |
|------|------------|
| NotFoundException class may not exist | Check existing handlers for 404 pattern — may use custom exception or return null-check with NotFound() |
| Event publishing may differ from Create | Read CreatePatientHandler event pattern exactly before implementing |
| UpdatePatientCommand may need route ID binding | Controller merges route ID with body — ensure command record accepts ID |

## Security Considerations

- Validate all input via FluentValidation before processing
- Route ID must match body ID to prevent IDOR
- Soft delete preserves audit trail
- Authorization handled at gateway/middleware level (existing pattern)
