using DoctorScheduleService.Infrastructure.Persistence;
using HospitalShared.Outbox;

namespace DoctorScheduleService.Infrastructure.MessageBus;

/// <summary>Service-specific EventPublisher — delegates to OutboxEventPublisher for reliable delivery.</summary>
public class EventPublisher : OutboxEventPublisher
{
    public EventPublisher(DoctorScheduleDbContext dbContext, ILogger<OutboxEventPublisher> logger)
        : base(dbContext, logger) { }
}
