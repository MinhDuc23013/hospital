---
phase: 2
title: "Repositories & Migration"
status: pending
priority: P1
effort: 2h
depends_on: [1]
---

# Phase 2: Repositories & Migration

## Context Links

- [DrugConfiguration](../../services/PharmacyServiceDotnet/Infrastructure/Persistence/DrugConfiguration.cs)
- [PrescriptionConfiguration](../../services/PharmacyServiceDotnet/Infrastructure/Persistence/PrescriptionConfiguration.cs)
- [PharmacyDbContext](../../services/PharmacyServiceDotnet/Infrastructure/Persistence/PharmacyDbContext.cs)
- [DrugRepository](../../services/PharmacyServiceDotnet/Infrastructure/Repositories/DrugRepository.cs)
- [PaymentAuditLogConfiguration](../../services/PaymentService/Infrastructure/Persistence/PaymentAuditLogConfiguration.cs)

## Overview

Create EF Core configurations, repository interfaces/implementations, and DB migration for all Phase 1 entities. Register in DI.

## Key Insights

- Use PostgreSQL `xmin` system column as concurrency token for `DrugBatch` (cheap, no schema change)
- `InventoryAuditLog` config: no update/delete at EF level -- use `builder.Metadata` to discourage but enforcement is at domain level
- Follow existing repo pattern: interface in `Infrastructure/Repositories/`, impl alongside
- Single migration covering all new tables

## Requirements

**Functional:**
- EF configurations for DrugBatch, StockReservation, InventoryAuditLog, DispensingSaga, DispensingSagaLog
- Repository interfaces + implementations for each
- PharmacyDbContext updated with new DbSets
- Program.cs updated with DI registrations

**Non-functional:**
- Indexes on foreign keys and frequently queried columns
- Concurrency token on `DrugBatch`
- Composite index on `(DrugId, ExpiryDate)` for FEFO queries

## Related Code Files

**Create:**
- `Infrastructure/Persistence/DrugBatchConfiguration.cs`
- `Infrastructure/Persistence/StockReservationConfiguration.cs`
- `Infrastructure/Persistence/InventoryAuditLogConfiguration.cs`
- `Infrastructure/Persistence/DispensingSagaConfiguration.cs`
- `Infrastructure/Persistence/DispensingSagaLogConfiguration.cs`
- `Infrastructure/Repositories/IDrugBatchRepository.cs`
- `Infrastructure/Repositories/DrugBatchRepository.cs`
- `Infrastructure/Repositories/IStockReservationRepository.cs`
- `Infrastructure/Repositories/StockReservationRepository.cs`
- `Infrastructure/Repositories/IInventoryAuditLogRepository.cs`
- `Infrastructure/Repositories/InventoryAuditLogRepository.cs`
- `Infrastructure/Repositories/IDispensingSagaRepository.cs`
- `Infrastructure/Repositories/DispensingSagaRepository.cs`
- `Infrastructure/Repositories/IDispensingSagaLogRepository.cs`
- `Infrastructure/Repositories/DispensingSagaLogRepository.cs`

**Modify:**
- `Infrastructure/Persistence/PharmacyDbContext.cs` -- add DbSets
- `Infrastructure/Persistence/DrugConfiguration.cs` -- add batch navigation
- `Program.cs` -- register new repos

## Implementation Steps

### 1. `DrugBatchConfiguration.cs`

```csharp
public class DrugBatchConfiguration : IEntityTypeConfiguration<DrugBatch>
{
    public void Configure(EntityTypeBuilder<DrugBatch> builder)
    {
        builder.ToTable("drug_batches");
        builder.HasKey(b => b.Id);
        builder.Property(b => b.DrugId).IsRequired();
        builder.Property(b => b.BatchNumber).IsRequired().HasMaxLength(100);
        builder.Property(b => b.ExpiryDate).IsRequired();
        builder.Property(b => b.Quantity).IsRequired().HasDefaultValue(0);
        builder.Property(b => b.ReceivedDate).IsRequired();
        builder.Property(b => b.CreatedAt).IsRequired();
        builder.Property(b => b.UpdatedAt).IsRequired();

        // Optimistic concurrency via PostgreSQL xmin
        builder.UseXminAsConcurrencyToken();

        // FEFO index: query batches by drug ordered by expiry
        builder.HasIndex(b => new { b.DrugId, b.ExpiryDate });
        builder.HasIndex(b => b.BatchNumber);

        // FK to Drug
        builder.HasOne(b => b.Drug)
            .WithMany(d => d.Batches)
            .HasForeignKey(b => b.DrugId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
```

### 2. `StockReservationConfiguration.cs`

```csharp
public class StockReservationConfiguration : IEntityTypeConfiguration<StockReservation>
{
    public void Configure(EntityTypeBuilder<StockReservation> builder)
    {
        builder.ToTable("stock_reservations");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.PrescriptionId).IsRequired();
        builder.Property(r => r.DrugBatchId).IsRequired();
        builder.Property(r => r.DrugId).IsRequired();
        builder.Property(r => r.Quantity).IsRequired();
        builder.Property(r => r.Status).IsRequired().HasMaxLength(20).HasConversion<string>();
        builder.Property(r => r.ExpiresAt).IsRequired();
        builder.Property(r => r.CreatedAt).IsRequired();
        builder.Property(r => r.UpdatedAt).IsRequired();

        builder.HasIndex(r => r.PrescriptionId);
        builder.HasIndex(r => new { r.Status, r.ExpiresAt }); // expiry worker query
        builder.HasIndex(r => r.DrugBatchId);
    }
}
```

### 3. `InventoryAuditLogConfiguration.cs`

```csharp
public class InventoryAuditLogConfiguration : IEntityTypeConfiguration<InventoryAuditLog>
{
    public void Configure(EntityTypeBuilder<InventoryAuditLog> builder)
    {
        builder.ToTable("inventory_audit_logs");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Action).IsRequired().HasMaxLength(20).HasConversion<string>();
        builder.Property(a => a.DrugId).IsRequired();
        builder.Property(a => a.Quantity).IsRequired();
        builder.Property(a => a.OldQuantity).IsRequired();
        builder.Property(a => a.NewQuantity).IsRequired();
        builder.Property(a => a.UserId).HasMaxLength(100);
        builder.Property(a => a.BatchNumber).HasMaxLength(100);
        builder.Property(a => a.Details).HasColumnType("text");
        builder.Property(a => a.Timestamp).IsRequired();

        builder.HasIndex(a => a.DrugId);
        builder.HasIndex(a => a.PrescriptionId);
        builder.HasIndex(a => a.Timestamp);
    }
}
```

### 4. `DispensingSagaConfiguration.cs` and `DispensingSagaLogConfiguration.cs`

Follow `BookingSagaConfiguration`/`BookingSagaLogConfiguration` patterns exactly. Table names: `dispensing_sagas`, `dispensing_saga_logs`. Index on `PrescriptionId` and `CurrentStep`.

### 5. Update `DrugConfiguration.cs`

Add backing field config for `Batches` navigation:

```csharp
// ADD inside Configure():
builder.HasMany(d => d.Batches)
    .WithOne(b => b.Drug)
    .HasForeignKey(b => b.DrugId);
builder.Navigation(d => d.Batches)
    .UsePropertyAccessMode(PropertyAccessMode.Field)
    .HasField("_batches");
```

### 6. Update `PharmacyDbContext.cs`

Add 5 new DbSets:

```csharp
public DbSet<DrugBatch> DrugBatches => Set<DrugBatch>();
public DbSet<StockReservation> StockReservations => Set<StockReservation>();
public DbSet<InventoryAuditLog> InventoryAuditLogs => Set<InventoryAuditLog>();
public DbSet<DispensingSaga> DispensingSagas => Set<DispensingSaga>();
public DbSet<DispensingSagaLog> DispensingSagaLogs => Set<DispensingSagaLog>();
```

Apply 5 new configurations in `OnModelCreating`.

### 7. Repository Interfaces & Implementations

Each repo follows existing pattern (`DrugRepository`). Key methods:

**IDrugBatchRepository:**
- `GetByIdAsync(Guid id)`
- `GetByDrugIdFefoAsync(Guid drugId)` -- `WHERE Quantity > 0 AND ExpiryDate > NOW() ORDER BY ExpiryDate ASC`
- `AddAsync`, `SaveChangesAsync`

**IStockReservationRepository:**
- `GetByPrescriptionIdAsync(Guid prescriptionId)`
- `GetExpiredReservationsAsync()` -- `WHERE Status = 'Reserved' AND ExpiresAt < NOW()`
- `AddAsync`, `SaveChangesAsync`

**IInventoryAuditLogRepository:**
- `AddAsync`, `GetByDrugIdAsync(Guid drugId, int page, int pageSize)`

**IDispensingSagaRepository / IDispensingSagaLogRepository:**
- Follow `IBookingSagaRepository` / `IBookingSagaLogRepository` patterns

### 8. Update `Program.cs` DI

```csharp
builder.Services.AddScoped<IDrugBatchRepository, DrugBatchRepository>();
builder.Services.AddScoped<IStockReservationRepository, StockReservationRepository>();
builder.Services.AddScoped<IInventoryAuditLogRepository, InventoryAuditLogRepository>();
builder.Services.AddScoped<IDispensingSagaRepository, DispensingSagaRepository>();
builder.Services.AddScoped<IDispensingSagaLogRepository, DispensingSagaLogRepository>();
```

### 9. Create EF Migration

```bash
cd services/PharmacyServiceDotnet
dotnet ef migrations add AddBatchReservationAudit
dotnet ef database update
```

## Todo List

- [ ] Create DrugBatchConfiguration
- [ ] Create StockReservationConfiguration
- [ ] Create InventoryAuditLogConfiguration
- [ ] Create DispensingSagaConfiguration
- [ ] Create DispensingSagaLogConfiguration
- [ ] Update DrugConfiguration (navigation)
- [ ] Update PharmacyDbContext (DbSets + configs)
- [ ] Create 5 repository interfaces
- [ ] Create 5 repository implementations
- [ ] Update Program.cs DI registrations
- [ ] Run EF migration, verify tables created
- [ ] Verify compile

## Success Criteria

- Migration runs clean, creates 5 new tables
- `drug_batches` has `xmin` concurrency token
- FEFO index exists on `(drug_id, expiry_date)`
- All repositories resolve from DI correctly

## Risk Assessment

- **Migration conflicts**: If other devs add migrations in parallel, may need to regenerate. Mitigate by coordinating merge timing.
- **xmin concurrency**: PostgreSQL-specific. Fine since project already uses Npgsql.

## Security Considerations

- `inventory_audit_logs` table should have restricted DELETE/UPDATE permissions at DB level (ops concern, not EF)

## Next Steps

- Phase 3: Implement FEFO stock deduction logic using `IDrugBatchRepository.GetByDrugIdFefoAsync()`
