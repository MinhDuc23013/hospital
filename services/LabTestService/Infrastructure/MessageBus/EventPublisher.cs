using HospitalShared.Outbox;
using LabTestService.Infrastructure.Persistence;

namespace LabTestService.Infrastructure.MessageBus;

/// <summary>Service-specific EventPublisher — delegates to OutboxEventPublisher for reliable delivery.</summary>
public class EventPublisher : OutboxEventPublisher
{
    public EventPublisher(LabTestDbContext dbContext, ILogger<OutboxEventPublisher> logger)
        : base(dbContext, logger) { }
}
