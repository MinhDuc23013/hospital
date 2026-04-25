using LabTestService.Application.DTOs;
using LabTestService.Application.Queries;
using LabTestService.Infrastructure.Repositories;
using MediatR;

namespace LabTestService.Application.Handlers;

/// <summary>Fetches a single lab order by ID, including all test items.</summary>
public class GetLabOrderHandler : IRequestHandler<GetLabOrderQuery, LabOrderDto?>
{
    private readonly ILabOrderRepository _repo;

    public GetLabOrderHandler(ILabOrderRepository repo) => _repo = repo;

    public async Task<LabOrderDto?> Handle(GetLabOrderQuery query, CancellationToken ct)
    {
        var order = await _repo.GetByIdAsync(query.Id, ct);
        return order is null ? null : LabOrderMapper.MapToDto(order);
    }
}
