using LabTestService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LabTestService.Infrastructure.Persistence;

/// <summary>EF Core configuration for LabOrder and LabOrderItem entities.</summary>
public class LabOrderConfiguration : IEntityTypeConfiguration<LabOrder>
{
    public void Configure(EntityTypeBuilder<LabOrder> builder)
    {
        builder.ToTable("lab_orders");
        builder.HasKey(o => o.Id);

        builder.Property(o => o.DoctorId).IsRequired().HasMaxLength(100);
        builder.Property(o => o.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(o => o.Notes).HasMaxLength(500);

        builder.HasIndex(o => o.PatientId);
        builder.HasIndex(o => o.AppointmentId);
        builder.HasIndex(o => o.DoctorId);

        builder.HasMany(o => o.Items)
            .WithOne()
            .HasForeignKey(i => i.LabOrderId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class LabOrderItemConfiguration : IEntityTypeConfiguration<LabOrderItem>
{
    public void Configure(EntityTypeBuilder<LabOrderItem> builder)
    {
        builder.ToTable("lab_order_items");
        builder.HasKey(i => i.Id);

        builder.Property(i => i.TestName).IsRequired().HasMaxLength(200);
        builder.Property(i => i.TestCode).IsRequired().HasMaxLength(200);
        builder.Property(i => i.Category).IsRequired().HasMaxLength(200);
        builder.Property(i => i.ResultStatus).HasConversion<string>().HasMaxLength(20);
        builder.Property(i => i.UnitPrice).HasColumnType("numeric(18,2)");
        builder.Property(i => i.Result).HasMaxLength(1000);
        builder.Property(i => i.NormalRange).HasMaxLength(100);
        builder.Property(i => i.Unit).HasMaxLength(100);
    }
}
