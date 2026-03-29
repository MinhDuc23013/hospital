using DoctorScheduleService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DoctorScheduleService.Infrastructure.Persistence;

public class DoctorConfiguration : IEntityTypeConfiguration<Doctor>
{
    public void Configure(EntityTypeBuilder<Doctor> builder)
    {
        builder.ToTable("doctors");
        builder.HasKey(d => d.Id);
        builder.Property(d => d.FullName).IsRequired().HasMaxLength(200);
        builder.Property(d => d.Specialty).IsRequired().HasMaxLength(100);
        builder.Property(d => d.Phone).HasMaxLength(20);
        builder.Property(d => d.Email).HasMaxLength(200);
        builder.Property(d => d.CreatedAt).HasDefaultValueSql("NOW()");
        builder.HasIndex(d => d.Specialty);
        builder.HasIndex(d => d.IsActive);
    }
}
