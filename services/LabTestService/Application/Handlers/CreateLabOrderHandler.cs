using HospitalShared.Events;
using LabTestService.Application.Commands;
using LabTestService.Application.DTOs;
using LabTestService.Domain.Entities;
using LabTestService.Infrastructure.MessageBus;
using LabTestService.Infrastructure.Repositories;
using MediatR;

namespace LabTestService.Application.Handlers;

/// <summary>Creates a new lab order and publishes LabOrderCreatedEvent via outbox.</summary>
public class CreateLabOrderHandler : IRequestHandler<CreateLabOrderCommand, LabOrderDto>
{
    private readonly ILabOrderRepository _repo;
    private readonly EventPublisher _events;

    public CreateLabOrderHandler(ILabOrderRepository repo, EventPublisher events)
    {
        _repo = repo;
        _events = events;
    }

    public async Task<LabOrderDto> Handle(CreateLabOrderCommand cmd, CancellationToken ct)
    {
        var items = cmd.Items.Select(i => (i.TestName, i.TestCode, i.Category, i.UnitPrice));
        var order = LabOrder.Create(cmd.PatientId, cmd.AppointmentId, cmd.DoctorId, cmd.Notes, items);

        await _repo.AddAsync(order, ct);
        await _repo.SaveChangesAsync(ct);

        await _events.PublishAsync(new LabOrderCreatedEvent
        {
            OrderId = order.Id,
            PatientId = order.PatientId,
            AppointmentId = order.AppointmentId,
            DoctorId = order.DoctorId,
            TestCount = order.Items.Count,
            Timestamp = DateTime.UtcNow
        }, ct);

        return LabOrderMapper.MapToDto(order);
    }
}
