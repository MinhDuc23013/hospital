using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PharmacyServiceDotnet.Domain.Entities;

namespace PharmacyServiceDotnet.Infrastructure.Persistence;

public class DispensingSagaConfiguration : IEntityTypeConfiguration<DispensingSaga>
{
    public void Configure(EntityTypeBuilder<DispensingSaga> builder)
    {
        builder.ToTable("dispensing_sagas");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.PrescriptionId).IsRequired();
        builder.Property(s => s.PatientId).IsRequired();
        builder.Property(s => s.DoctorId).IsRequired().HasMaxLength(100);
        builder.Property(s => s.PaymentAmount).IsRequired().HasColumnType("numeric");
        builder.Property(s => s.Notes).HasMaxLength(2000);
        builder.Property(s => s.CurrentStep)
            .IsRequired()
            .HasMaxLength(30)
            .HasConversion<string>();
        builder.Property(s => s.FailureReason).HasMaxLength(1000);
        builder.Property(s => s.CreatedAt).IsRequired();
        builder.Property(s => s.UpdatedAt).IsRequired();

        builder.HasIndex(s => s.PrescriptionId);
        builder.HasIndex(s => s.CurrentStep);
    }
}
