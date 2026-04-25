using LabTestService.Application.DTOs;
using LabTestService.Application.Queries;
using LabTestService.Infrastructure.Repositories;
using MediatR;

namespace LabTestService.Application.Handlers;

/// <summary>Returns paginated list of lab orders, optionally filtered by patient or appointment.</summary>
public class ListLabOrdersHandler : IRequestHandler<ListLabOrdersQuery, (List<LabOrderDto> Items, int Total)>
{
    private readonly ILabOrderRepository _repo;

    public ListLabOrdersHandler(ILabOrderRepository repo) => _repo = repo;

    public async Task<(List<LabOrderDto> Items, int Total)> Handle(ListLabOrdersQuery query, CancellationToken ct)
    {
        var (orders, total) = await _repo.ListAsync(
            query.PatientId, query.AppointmentId, query.Page, query.PageSize, ct);
        return (orders.Select(LabOrderMapper.MapToDto).ToList(), total);
    }
}
