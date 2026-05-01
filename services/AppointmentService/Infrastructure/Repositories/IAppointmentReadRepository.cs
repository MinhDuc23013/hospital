using AppointmentService.Domain.Entities;

namespace AppointmentService.Infrastructure.Repositories;

public interface IAppointmentReadRepository
{
    Task<Appointment?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<(List<Appointment> Items, int Total)> ListAsync(Guid? patientId, string? providerId, int page, int pageSize, CancellationToken ct = default);
}
