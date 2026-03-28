using PaymentService.Domain.Entities;
using PaymentService.Domain.Enums;

namespace PaymentService.Infrastructure.Repositories;

public interface IPaymentRepository
{
    Task<Payment?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Payment?> GetByAppointmentIdAsync(Guid appointmentId, CancellationToken ct = default);
    Task<(List<Payment> Items, int Total)> ListAsync(
        Guid? appointmentId, Guid? patientId, PaymentStatus? status,
        int page, int pageSize, CancellationToken ct = default);
    Task AddAsync(Payment payment, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
