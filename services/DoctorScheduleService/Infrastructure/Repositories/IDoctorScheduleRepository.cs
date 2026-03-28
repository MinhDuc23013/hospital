using DoctorScheduleService.Domain.Entities;

namespace DoctorScheduleService.Infrastructure.Repositories;

public interface IDoctorScheduleRepository
{
    Task<DoctorSchedule?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<(List<DoctorSchedule> Items, int Total)> ListAsync(
        string? doctorId, DateTime? date, int page, int pageSize, CancellationToken ct = default);
    Task AddAsync(DoctorSchedule schedule, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
