using DoctorScheduleService.Domain.Entities;

namespace DoctorScheduleService.Infrastructure.Repositories;

public interface IDoctorRepository
{
    Task<Doctor?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<(List<Doctor> Items, int Total)> ListAsync(string? searchName, string? specialty, bool? isActive, int page, int pageSize, CancellationToken ct = default);
    Task AddAsync(Doctor doctor, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
