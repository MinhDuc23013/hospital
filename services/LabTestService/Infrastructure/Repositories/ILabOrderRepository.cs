using LabTestService.Domain.Entities;

namespace LabTestService.Infrastructure.Repositories;

/// <summary>Repository contract for LabOrder persistence operations.</summary>
public interface ILabOrderRepository
{
    /// <summary>Gets a lab order by ID with all items eagerly loaded.</summary>
    Task<LabOrder?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Returns paginated lab orders filtered by optional patientId / appointmentId.</summary>
    Task<(List<LabOrder> Items, int Total)> ListAsync(
        Guid? patientId, Guid? appointmentId, int page, int pageSize, CancellationToken ct = default);

    Task AddAsync(LabOrder order, CancellationToken ct = default);

    Task SaveChangesAsync(CancellationToken ct = default);
}
