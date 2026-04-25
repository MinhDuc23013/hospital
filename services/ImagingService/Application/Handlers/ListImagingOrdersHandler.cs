using ImagingService.Application.DTOs;
using ImagingService.Application.Queries;
using ImagingService.Infrastructure.Repositories;
using MediatR;

namespace ImagingService.Application.Handlers;

/// <summary>Returns a paginated list of imaging orders, optionally filtered by patient or appointment.</summary>
public class ListImagingOrdersHandler : IRequestHandler<ListImagingOrdersQuery, (List<ImagingOrderDto> Items, int Total)>
{
    private readonly IImagingOrderRepository _repo;

    public ListImagingOrdersHandler(IImagingOrderRepository repo)
    {
        _repo = repo;
    }

    public async Task<(List<ImagingOrderDto> Items, int Total)> Handle(
        ListImagingOrdersQuery query, CancellationToken ct)
    {
        var (orders, total) = await _repo.ListAsync(
            query.PatientId, query.AppointmentId, query.Page, query.PageSize, ct);

        var dtos = orders.Select(ImagingOrderMapper.MapToDto).ToList();
        return (dtos, total);
    }
}
