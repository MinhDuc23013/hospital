using HospitalShared.Outbox;
using OrchestratorService.Infrastructure.Persistence;

namespace OrchestratorService.Infrastructure.MessageBus;

/// <summary>Service-specific EventPublisher — delegates to OutboxEventPublisher for reliable delivery.</summary>
public class EventPublisher : OutboxEventPublisher
{
    public EventPublisher(OrchestratorDbContext dbContext, ILogger<OutboxEventPublisher> logger)
        : base(dbContext, logger) { }
}
