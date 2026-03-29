using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PharmacyServiceDotnet.Domain.Entities;

namespace PharmacyServiceDotnet.Infrastructure.Persistence;

public class PrescriptionConfiguration : IEntityTypeConfiguration<Prescription>
{
    public void Configure(EntityTypeBuilder<Prescription> builder)
    {
        builder.ToTable("prescriptions");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.PatientId).IsRequired();
        builder.Property(p => p.DoctorId).IsRequired().HasMaxLength(100);
        builder.Property(p => p.AppointmentId);
        builder.Property(p => p.Status)
            .IsRequired()
            .HasMaxLength(20)
            .HasConversion<string>();
        builder.Property(p => p.Notes).HasMaxLength(2000);
        builder.Property(p => p.Items).IsRequired().HasColumnType("text");
        builder.Property(p => p.CreatedAt).IsRequired();
        builder.Property(p => p.UpdatedAt).IsRequired();
        builder.HasIndex(p => p.PatientId);
        builder.HasIndex(p => p.Status);
    }
}
