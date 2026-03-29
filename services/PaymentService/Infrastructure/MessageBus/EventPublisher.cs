using HospitalShared.Outbox;
using PaymentService.Infrastructure.Persistence;

namespace PaymentService.Infrastructure.MessageBus;

/// <summary>Service-specific EventPublisher — delegates to OutboxEventPublisher for reliable delivery.</summary>
public class EventPublisher : OutboxEventPublisher
{
    public EventPublisher(PaymentDbContext dbContext, ILogger<OutboxEventPublisher> logger)
        : base(dbContext, logger) { }
}
