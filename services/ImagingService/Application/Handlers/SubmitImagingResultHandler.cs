using HospitalShared.Events;
using ImagingService.Application.Commands;
using ImagingService.Application.DTOs;
using ImagingService.Infrastructure.MessageBus;
using ImagingService.Infrastructure.Repositories;
using MediatR;

namespace ImagingService.Application.Handlers;

/// <summary>Submits radiology result for an order and publishes ImagingResultReadyEvent to outbox.</summary>
public class SubmitImagingResultHandler : IRequestHandler<SubmitImagingResultCommand, ImagingOrderDto>
{
    private readonly IImagingOrderRepository _repo;
    private readonly EventPublisher _events;

    public SubmitImagingResultHandler(IImagingOrderRepository repo, EventPublisher events)
    {
        _repo = repo;
        _events = events;
    }

    public async Task<ImagingOrderDto> Handle(SubmitImagingResultCommand cmd, CancellationToken ct)
    {
        var order = await _repo.GetByIdAsync(cmd.OrderId, ct)
            ?? throw new KeyNotFoundException($"Imaging order {cmd.OrderId} not found.");

        order.SubmitResult(cmd.Findings, cmd.Impression, cmd.ImageUrl, cmd.ReportedBy);
        await _repo.SaveChangesAsync(ct);

        var @event = new ImagingResultReadyEvent
        {
            OrderId = order.Id,
            PatientId = order.PatientId,
            AppointmentId = order.AppointmentId,
            DoctorId = order.DoctorId,
            Timestamp = DateTime.UtcNow
        };

        await _events.PublishAsync(@event, ct);

        return ImagingOrderMapper.MapToDto(order);
    }
}
