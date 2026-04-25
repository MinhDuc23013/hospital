using HospitalShared.Outbox;
using ImagingService.Infrastructure.Persistence;

namespace ImagingService.Infrastructure.MessageBus;

/// <summary>Service-specific EventPublisher — delegates to OutboxEventPublisher for reliable Kafka delivery.</summary>
public class EventPublisher : OutboxEventPublisher
{
    public EventPublisher(ImagingDbContext dbContext, ILogger<OutboxEventPublisher> logger)
        : base(dbContext, logger) { }
}
