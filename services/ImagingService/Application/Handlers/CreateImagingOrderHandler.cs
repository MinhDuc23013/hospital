using HospitalShared.Events;
using ImagingService.Application.Commands;
using ImagingService.Application.DTOs;
using ImagingService.Domain.Entities;
using ImagingService.Infrastructure.MessageBus;
using ImagingService.Infrastructure.Repositories;
using MediatR;

namespace ImagingService.Application.Handlers;

/// <summary>Creates a new imaging order and publishes ImagingOrderCreatedEvent to outbox.</summary>
public class CreateImagingOrderHandler : IRequestHandler<CreateImagingOrderCommand, ImagingOrderDto>
{
    private readonly IImagingOrderRepository _repo;
    private readonly EventPublisher _events;

    public CreateImagingOrderHandler(IImagingOrderRepository repo, EventPublisher events)
    {
        _repo = repo;
        _events = events;
    }

    public async Task<ImagingOrderDto> Handle(CreateImagingOrderCommand cmd, CancellationToken ct)
    {
        var order = ImagingOrder.Create(
            cmd.PatientId, cmd.AppointmentId, cmd.DoctorId,
            cmd.Type, cmd.BodyPart, cmd.ClinicalHistory, cmd.Price);

        await _repo.AddAsync(order, ct);
        await _repo.SaveChangesAsync(ct);

        var @event = new ImagingOrderCreatedEvent
        {
            OrderId = order.Id,
            PatientId = order.PatientId,
            AppointmentId = order.AppointmentId,
            DoctorId = order.DoctorId,
            Type = order.Type.ToString(),
            BodyPart = order.BodyPart,
            Timestamp = DateTime.UtcNow
        };

        await _events.PublishAsync(@event, ct);

        return ImagingOrderMapper.MapToDto(order);
    }
}
