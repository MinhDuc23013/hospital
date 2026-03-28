using HospitalShared.DTOs;
using MediatR;
using PaymentService.Application.Queries;
using PaymentService.Infrastructure.Repositories;

namespace PaymentService.Application.Handlers;

public class ListPaymentsHandler : IRequestHandler<ListPaymentsQuery, (List<PaymentDto> Items, int Total)>
{
    private readonly IPaymentRepository _repo;
    public ListPaymentsHandler(IPaymentRepository repo) => _repo = repo;

    public async Task<(List<PaymentDto> Items, int Total)> Handle(ListPaymentsQuery query, CancellationToken ct)
    {
        var (payments, total) = await _repo.ListAsync(
            query.AppointmentId, query.PatientId, query.Status, query.Page, query.PageSize, ct);
        return (payments.Select(PaymentMapper.ToDto).ToList(), total);
    }
}
