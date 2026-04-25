using HospitalShared.Events;
using LabTestService.Application.Commands;
using LabTestService.Application.DTOs;
using LabTestService.Infrastructure.MessageBus;
using LabTestService.Infrastructure.Repositories;
using MediatR;

namespace LabTestService.Application.Handlers;

/// <summary>Records results for individual test items; marks order completed when all items have results.</summary>
public class SubmitLabResultsHandler : IRequestHandler<SubmitLabResultsCommand, LabOrderDto>
{
    private readonly ILabOrderRepository _repo;
    private readonly EventPublisher _events;

    public SubmitLabResultsHandler(ILabOrderRepository repo, EventPublisher events)
    {
        _repo = repo;
        _events = events;
    }

    public async Task<LabOrderDto> Handle(SubmitLabResultsCommand cmd, CancellationToken ct)
    {
        var order = await _repo.GetByIdAsync(cmd.OrderId, ct)
            ?? throw new KeyNotFoundException($"LabOrder {cmd.OrderId} not found.");

        foreach (var input in cmd.Results)
        {
            var item = order.Items.FirstOrDefault(i => i.Id == input.ItemId)
                ?? throw new KeyNotFoundException($"LabOrderItem {input.ItemId} not found in order {cmd.OrderId}.");
            item.SubmitResult(input.Result, input.NormalRange, input.Unit, input.ResultStatus);
        }

        // All items have results → mark completed; otherwise in-progress
        var allDone = order.Items.All(i => i.ResultedAt.HasValue);
        if (allDone)
        {
            order.MarkCompleted();
            await _repo.SaveChangesAsync(ct);

            await _events.PublishAsync(new LabResultReadyEvent
            {
                OrderId = order.Id,
                PatientId = order.PatientId,
                AppointmentId = order.AppointmentId,
                DoctorId = order.DoctorId,
                Timestamp = DateTime.UtcNow
            }, ct);
        }
        else
        {
            order.MarkInProgress();
            await _repo.SaveChangesAsync(ct);
        }

        return LabOrderMapper.MapToDto(order);
    }
}
