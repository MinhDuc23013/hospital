using ImagingService.Application.DTOs;
using ImagingService.Application.Queries;
using ImagingService.Infrastructure.Repositories;
using MediatR;

namespace ImagingService.Application.Handlers;

/// <summary>Returns a single imaging order by ID, including its result if present.</summary>
public class GetImagingOrderHandler : IRequestHandler<GetImagingOrderQuery, ImagingOrderDto?>
{
    private readonly IImagingOrderRepository _repo;

    public GetImagingOrderHandler(IImagingOrderRepository repo)
    {
        _repo = repo;
    }

    public async Task<ImagingOrderDto?> Handle(GetImagingOrderQuery query, CancellationToken ct)
    {
        var order = await _repo.GetByIdAsync(query.Id, ct);
        return order is null ? null : ImagingOrderMapper.MapToDto(order);
    }
}
