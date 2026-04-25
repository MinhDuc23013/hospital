using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrchestratorService.Domain.Entities;

namespace OrchestratorService.Infrastructure.Persistence;

public class PaymentSagaConfiguration : IEntityTypeConfiguration<PaymentSaga>
{
    public void Configure(EntityTypeBuilder<PaymentSaga> builder)
    {
        builder.ToTable("payment_sagas");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Method).IsRequired().HasMaxLength(50);
        builder.Property(s => s.Currency).IsRequired().HasMaxLength(10);
        builder.Property(s => s.CurrentStep).HasConversion<string>().HasMaxLength(20);
        builder.Property(s => s.FailureReason).HasMaxLength(1000);
        builder.Property(s => s.CheckoutUrl).HasMaxLength(2000);
        builder.Property(s => s.CreatedAt).HasDefaultValueSql("NOW()");
        builder.HasIndex(s => s.AppointmentId);
        builder.HasIndex(s => s.PaymentId);
        builder.HasIndex(s => s.CurrentStep);
    }
}
