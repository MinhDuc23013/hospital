using HospitalShared.DTOs;
using MediatR;
using PaymentService.Application.Queries;
using PaymentService.Infrastructure.Repositories;

namespace PaymentService.Application.Handlers;

public class GetPaymentByAppointmentHandler : IRequestHandler<GetPaymentByAppointmentQuery, PaymentDto?>
{
    private readonly IPaymentRepository _repo;
    public GetPaymentByAppointmentHandler(IPaymentRepository repo) => _repo = repo;

    public async Task<PaymentDto?> Handle(GetPaymentByAppointmentQuery query, CancellationToken ct)
    {
        var payment = await _repo.GetByAppointmentIdAsync(query.AppointmentId, ct);
        return payment is null ? null : PaymentMapper.ToDto(payment);
    }
}
