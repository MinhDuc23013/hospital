using ImagingService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ImagingService.Infrastructure.Persistence;

/// <summary>EF Core table/column configuration for ImagingOrder and ImagingResult entities.</summary>
internal class ImagingOrderConfiguration : IEntityTypeConfiguration<ImagingOrder>
{
    public void Configure(EntityTypeBuilder<ImagingOrder> builder)
    {
        builder.ToTable("imaging_orders");
        builder.HasKey(o => o.Id);

        builder.HasIndex(o => o.PatientId);
        builder.HasIndex(o => o.AppointmentId);
        builder.HasIndex(o => o.DoctorId);

        builder.Property(o => o.Status)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(o => o.Type)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(o => o.BodyPart)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(o => o.Price).HasColumnType("numeric(18,2)");

        builder.Property(o => o.ClinicalHistory)
            .HasMaxLength(1000);

        builder.Property(o => o.DoctorId)
            .IsRequired()
            .HasMaxLength(200);

        // One-to-one: ImagingOrder HasOne(Result)
        builder.HasOne(o => o.Result)
            .WithOne()
            .HasForeignKey<ImagingResult>(r => r.ImagingOrderId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

internal class ImagingResultConfiguration : IEntityTypeConfiguration<ImagingResult>
{
    public void Configure(EntityTypeBuilder<ImagingResult> builder)
    {
        builder.ToTable("imaging_results");
        builder.HasKey(r => r.Id);

        builder.HasIndex(r => r.ImagingOrderId).IsUnique();

        builder.Property(r => r.Findings)
            .IsRequired()
            .HasMaxLength(2000);

        builder.Property(r => r.Impression)
            .IsRequired()
            .HasMaxLength(2000);

        builder.Property(r => r.ImageUrl)
            .HasMaxLength(500);

        builder.Property(r => r.ReportedBy)
            .IsRequired()
            .HasMaxLength(200);
    }
}
