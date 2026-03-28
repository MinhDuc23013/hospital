using HospitalShared.DTOs;
using MediatR;
using PaymentService.Application.Queries;
using PaymentService.Infrastructure.Repositories;

namespace PaymentService.Application.Handlers;

public class GetPaymentHandler : IRequestHandler<GetPaymentQuery, PaymentDto?>
{
    private readonly IPaymentRepository _repo;
    public GetPaymentHandler(IPaymentRepository repo) => _repo = repo;

    public async Task<PaymentDto?> Handle(GetPaymentQuery query, CancellationToken ct)
    {
        var payment = await _repo.GetByIdAsync(query.Id, ct);
        return payment is null ? null : PaymentMapper.ToDto(payment);
    }
}
