using ImagingService.Domain.Entities;

namespace ImagingService.Infrastructure.Repositories;

/// <summary>Repository contract for ImagingOrder persistence.</summary>
public interface IImagingOrderRepository
{
    /// <summary>Get single order by ID with Result included.</summary>
    Task<ImagingOrder?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Paginated list filtered optionally by patient or appointment.</summary>
    Task<(List<ImagingOrder> Items, int Total)> ListAsync(
        Guid? patientId, Guid? appointmentId,
        int page, int pageSize,
        CancellationToken ct = default);

    Task AddAsync(ImagingOrder order, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
