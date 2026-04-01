---
phase: 1
title: "Domain Entities"
status: pending
priority: P1
effort: 2h
---

# Phase 1: Domain Entities

## Context Links

- [Current Drug entity](../../services/PharmacyServiceDotnet/Domain/Entities/Drug.cs)
- [Current Prescription entity](../../services/PharmacyServiceDotnet/Domain/Entities/Prescription.cs)
- [BookingSaga pattern](../../services/AppointmentService/Domain/Entities/BookingSaga.cs)
- [PaymentAuditLog pattern](../../services/PaymentService/Domain/Entities/PaymentAuditLog.cs)

## Overview

Create three new domain entities (`DrugBatch`, `StockReservation`, `InventoryAuditLog`) and two new enums (`ReservationStatus`, `DispensingSagaStep`). Modify `Drug` entity to support batch-based stock.

## Key Insights

- Follow existing DDD pattern: private constructor, static `Create()` factory, private setters
- `InventoryAuditLog` must be immutable (no update/delete methods)
- `Drug.Quantity` will become a read convenience -- real stock lives in batches
- Match `PaymentAuditLog` structure for audit; match `BookingSaga` for saga entity

## Requirements

**Functional:**
- DrugBatch tracks batch number, expiry date, quantity per batch
- StockReservation links prescription items to specific batches
- InventoryAuditLog records every stock movement with before/after quantities
- DispensingSaga tracks multi-step dispensing flow state

**Non-functional:**
- All entities use `Guid` primary keys (consistent with codebase)
- `DateTime.Now` for timestamps (matches existing pattern)

## Architecture

```
Drug (modified)
 └── has many → DrugBatch (new)
                  └── has many → StockReservation (new)

Prescription
 └── has one → DispensingSaga (new)
                 └── logged by → DispensingSagaLog (new)

InventoryAuditLog (new, standalone, immutable)
```

## Related Code Files

**Create:**
- `services/PharmacyServiceDotnet/Domain/Entities/DrugBatch.cs`
- `services/PharmacyServiceDotnet/Domain/Entities/StockReservation.cs`
- `services/PharmacyServiceDotnet/Domain/Entities/InventoryAuditLog.cs`
- `services/PharmacyServiceDotnet/Domain/Entities/DispensingSaga.cs`
- `services/PharmacyServiceDotnet/Domain/Entities/DispensingSagaLog.cs`
- `services/PharmacyServiceDotnet/Domain/Enums/ReservationStatus.cs`
- `services/PharmacyServiceDotnet/Domain/Enums/DispensingSagaStep.cs`
- `services/PharmacyServiceDotnet/Domain/Enums/AuditAction.cs`

**Modify:**
- `services/PharmacyServiceDotnet/Domain/Entities/Drug.cs` -- add navigation property, deprecate direct Quantity setter

## Implementation Steps

### 1. Create `Domain/Enums/ReservationStatus.cs`

```csharp
namespace PharmacyServiceDotnet.Domain.Enums;

public enum ReservationStatus
{
    Reserved = 0,
    Committed = 1,
    Released = 2
}
```

### 2. Create `Domain/Enums/DispensingSagaStep.cs`

```csharp
namespace PharmacyServiceDotnet.Domain.Enums;

public enum DispensingSagaStep
{
    Started = 0,
    StockReserved = 1,
    AwaitingPayment = 2,
    PaymentConfirmed = 3,
    StockCommitted = 4,
    Dispensed = 5,
    Failed = 10,
    Compensating = 11,
    Compensated = 12
}
```

### 3. Create `Domain/Enums/AuditAction.cs`

```csharp
namespace PharmacyServiceDotnet.Domain.Enums;

public enum AuditAction
{
    Receive,
    Reserve,
    Commit,
    Release,
    Adjust
}
```

### 4. Create `Domain/Entities/DrugBatch.cs`

```csharp
namespace PharmacyServiceDotnet.Domain.Entities;

public class DrugBatch
{
    public Guid Id { get; private set; }
    public Guid DrugId { get; private set; }
    public string BatchNumber { get; private set; } = string.Empty;
    public DateTime ExpiryDate { get; private set; }
    public int Quantity { get; private set; }
    public DateTime ReceivedDate { get; private set; }
    public uint RowVersion { get; private set; } // xmin concurrency token
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    // Navigation
    public Drug Drug { get; private set; } = null!;

    private DrugBatch() { }

    public static DrugBatch Create(
        Guid drugId, string batchNumber, DateTime expiryDate,
        int quantity, DateTime? receivedDate = null)
    {
        return new DrugBatch
        {
            Id = Guid.NewGuid(),
            DrugId = drugId,
            BatchNumber = batchNumber,
            ExpiryDate = expiryDate,
            Quantity = quantity,
            ReceivedDate = receivedDate ?? DateTime.Now,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };
    }

    /// <summary>Deduct quantity. Returns remaining.</summary>
    public int Deduct(int amount)
    {
        if (amount > Quantity)
            throw new Domain.Exceptions.DomainException(
                $"Cannot deduct {amount} from batch {BatchNumber} (available: {Quantity}).");
        Quantity -= amount;
        UpdatedAt = DateTime.Now;
        return Quantity;
    }

    /// <summary>Restore quantity (compensation/release).</summary>
    public void Restore(int amount)
    {
        Quantity += amount;
        UpdatedAt = DateTime.Now;
    }
}
```

### 5. Create `Domain/Entities/StockReservation.cs`

```csharp
using PharmacyServiceDotnet.Domain.Enums;

namespace PharmacyServiceDotnet.Domain.Entities;

public class StockReservation
{
    public Guid Id { get; private set; }
    public Guid PrescriptionId { get; private set; }
    public Guid DrugBatchId { get; private set; }
    public Guid DrugId { get; private set; }
    public int Quantity { get; private set; }
    public ReservationStatus Status { get; private set; }
    public DateTime ExpiresAt { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private StockReservation() { }

    public static StockReservation Create(
        Guid prescriptionId, Guid drugBatchId, Guid drugId,
        int quantity, TimeSpan? ttl = null)
    {
        return new StockReservation
        {
            Id = Guid.NewGuid(),
            PrescriptionId = prescriptionId,
            DrugBatchId = drugBatchId,
            DrugId = drugId,
            Quantity = quantity,
            Status = ReservationStatus.Reserved,
            ExpiresAt = DateTime.Now.Add(ttl ?? TimeSpan.FromMinutes(30)),
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };
    }

    public void Commit()
    {
        if (Status != ReservationStatus.Reserved)
            throw new Domain.Exceptions.DomainException(
                $"Cannot commit reservation in status '{Status}'.");
        Status = ReservationStatus.Committed;
        UpdatedAt = DateTime.Now;
    }

    public void Release()
    {
        if (Status != ReservationStatus.Reserved)
            throw new Domain.Exceptions.DomainException(
                $"Cannot release reservation in status '{Status}'.");
        Status = ReservationStatus.Released;
        UpdatedAt = DateTime.Now;
    }

    public bool IsExpired => Status == ReservationStatus.Reserved
                             && DateTime.Now > ExpiresAt;
}
```

### 6. Create `Domain/Entities/InventoryAuditLog.cs`

Follow `PaymentAuditLog` pattern. No mutation methods -- immutable after creation.

```csharp
using PharmacyServiceDotnet.Domain.Enums;

namespace PharmacyServiceDotnet.Domain.Entities;

public class InventoryAuditLog
{
    public Guid Id { get; private set; }
    public AuditAction Action { get; private set; }
    public Guid DrugId { get; private set; }
    public Guid? DrugBatchId { get; private set; }
    public Guid? PrescriptionId { get; private set; }
    public Guid? PatientId { get; private set; }
    public string? UserId { get; private set; }
    public int Quantity { get; private set; }
    public int OldQuantity { get; private set; }
    public int NewQuantity { get; private set; }
    public string? BatchNumber { get; private set; }
    public string? Details { get; private set; }
    public DateTime Timestamp { get; private set; }

    private InventoryAuditLog() { }

    public static InventoryAuditLog Create(
        AuditAction action, Guid drugId, int quantity,
        int oldQuantity, int newQuantity,
        Guid? drugBatchId = null, Guid? prescriptionId = null,
        Guid? patientId = null, string? userId = null,
        string? batchNumber = null, string? details = null)
    {
        return new InventoryAuditLog
        {
            Id = Guid.NewGuid(),
            Action = action,
            DrugId = drugId,
            DrugBatchId = drugBatchId,
            PrescriptionId = prescriptionId,
            PatientId = patientId,
            UserId = userId,
            Quantity = quantity,
            OldQuantity = oldQuantity,
            NewQuantity = newQuantity,
            BatchNumber = batchNumber,
            Details = details,
            Timestamp = DateTime.Now
        };
    }
}
```

### 7. Create `Domain/Entities/DispensingSaga.cs`

Follow `BookingSaga` pattern.

```csharp
using PharmacyServiceDotnet.Domain.Enums;

namespace PharmacyServiceDotnet.Domain.Entities;

public class DispensingSaga
{
    public Guid Id { get; private set; }
    public Guid PrescriptionId { get; private set; }
    public Guid PatientId { get; private set; }
    public string DoctorId { get; private set; } = string.Empty;
    public DispensingSagaStep CurrentStep { get; private set; }
    public string? FailureReason { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private DispensingSaga() { }

    public static DispensingSaga Create(Guid prescriptionId, Guid patientId, string doctorId)
    {
        return new DispensingSaga
        {
            Id = Guid.NewGuid(),
            PrescriptionId = prescriptionId,
            PatientId = patientId,
            DoctorId = doctorId,
            CurrentStep = DispensingSagaStep.Started,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };
    }

    public void MarkStockReserved() => AdvanceTo(DispensingSagaStep.StockReserved);
    public void MarkAwaitingPayment() => AdvanceTo(DispensingSagaStep.AwaitingPayment);
    public void MarkPaymentConfirmed() => AdvanceTo(DispensingSagaStep.PaymentConfirmed);
    public void MarkStockCommitted() => AdvanceTo(DispensingSagaStep.StockCommitted);
    public void MarkDispensed() => AdvanceTo(DispensingSagaStep.Dispensed);

    public void MarkFailed(string reason)
    {
        FailureReason = reason;
        CurrentStep = DispensingSagaStep.Failed;
        UpdatedAt = DateTime.Now;
    }

    public void MarkCompensating()
    {
        CurrentStep = DispensingSagaStep.Compensating;
        UpdatedAt = DateTime.Now;
    }

    public void MarkCompensated()
    {
        CurrentStep = DispensingSagaStep.Compensated;
        UpdatedAt = DateTime.Now;
    }

    private void AdvanceTo(DispensingSagaStep step)
    {
        CurrentStep = step;
        UpdatedAt = DateTime.Now;
    }
}
```

### 8. Create `Domain/Entities/DispensingSagaLog.cs`

Follow `BookingSagaLog` pattern.

```csharp
namespace PharmacyServiceDotnet.Domain.Entities;

public class DispensingSagaLog
{
    public Guid Id { get; private set; }
    public Guid SagaId { get; private set; }
    public string FromStep { get; private set; } = string.Empty;
    public string ToStep { get; private set; } = string.Empty;
    public string? Message { get; private set; }
    public string? Details { get; private set; }
    public DateTime Timestamp { get; private set; }

    private DispensingSagaLog() { }

    public static DispensingSagaLog Create(
        Guid sagaId, string fromStep, string toStep,
        string? message = null, string? details = null)
    {
        return new DispensingSagaLog
        {
            Id = Guid.NewGuid(),
            SagaId = sagaId,
            FromStep = fromStep,
            ToStep = toStep,
            Message = message,
            Details = details,
            Timestamp = DateTime.Now
        };
    }
}
```

### 9. Modify `Domain/Entities/Drug.cs`

Add navigation property for batches. Keep `Quantity` property but note it will be superseded by batch sum in queries.

```csharp
// ADD after existing properties:
public IReadOnlyCollection<DrugBatch> Batches => _batches.AsReadOnly();
private readonly List<DrugBatch> _batches = new();

// ADD method:
/// <summary>Recalculates Quantity from batch totals.</summary>
public void RecalculateQuantity(int batchTotal)
{
    Quantity = batchTotal;
    UpdatedAt = DateTime.Now;
}
```

## Todo List

- [ ] Create `ReservationStatus` enum
- [ ] Create `DispensingSagaStep` enum
- [ ] Create `AuditAction` enum
- [ ] Create `DrugBatch` entity
- [ ] Create `StockReservation` entity
- [ ] Create `InventoryAuditLog` entity (immutable)
- [ ] Create `DispensingSaga` entity
- [ ] Create `DispensingSagaLog` entity
- [ ] Modify `Drug` entity -- add `Batches` navigation + `RecalculateQuantity()`
- [ ] Verify all entities compile

## Success Criteria

- All entity files compile without errors
- Entities follow DDD pattern: private ctor, static Create(), private setters
- `InventoryAuditLog` has no mutation methods
- `DrugBatch.Deduct()` throws on insufficient quantity
- `StockReservation.Commit()`/`Release()` enforce valid state transitions

## Risk Assessment

- **Drug.Quantity backward compat**: Existing code reads `Drug.Quantity` directly. Keep it working by recalculating from batches. Queries in Phase 3 handle this.
- **EF Core navigation loading**: `_batches` backing field needs EF config (Phase 2).

## Security Considerations

- Audit log immutability enforced at domain level (no setters, no update methods)
- DB-level immutability enforced via EF config in Phase 2 (no UPDATE/DELETE)

## Next Steps

- Phase 2: EF configurations, repositories, DB migration for all new entities
