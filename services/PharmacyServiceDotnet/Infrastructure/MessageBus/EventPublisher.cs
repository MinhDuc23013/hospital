using HospitalShared.Outbox;
using PharmacyServiceDotnet.Infrastructure.Persistence;

namespace PharmacyServiceDotnet.Infrastructure.MessageBus;

/// <summary>Service-specific EventPublisher — delegates to OutboxEventPublisher for reliable delivery.</summary>
public class EventPublisher : OutboxEventPublisher
{
    public EventPublisher(PharmacyDbContext dbContext, ILogger<OutboxEventPublisher> logger)
        : base(dbContext, logger) { }
}
