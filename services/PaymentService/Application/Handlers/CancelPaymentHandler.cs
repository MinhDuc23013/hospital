using HospitalShared.DTOs;
using MediatR;
using PaymentService.Application.Commands;
using PaymentService.Domain.Entities;
using PaymentService.Domain.Exceptions;
using PaymentService.Infrastructure.Repositories;

namespace PaymentService.Application.Handlers;

/// <summary>Cancels a pending/processing payment — no money was charged.</summary>
public class CancelPaymentHandler : IRequestHandler<CancelPaymentCommand, PaymentDto>
{
    private readonly IPaymentRepository _repo;
    private readonly IPaymentAuditLogRepository _auditRepo;

    public CancelPaymentHandler(IPaymentRepository repo, IPaymentAuditLogRepository auditRepo)
    {
        _repo = repo;
        _auditRepo = auditRepo;
    }

    public async Task<PaymentDto> Handle(CancelPaymentCommand cmd, CancellationToken ct)
    {
        var payment = await _repo.GetByIdAsync(cmd.PaymentId, ct)
            ?? throw new NotFoundException("Payment", cmd.PaymentId);

        var oldStatus = payment.Status.ToString();
        payment.Cancel();

        var log = PaymentAuditLog.Create(payment.Id, "Cancelled", oldStatus: oldStatus, newStatus: "Failed",
            message: "Payment cancelled due to booking cancellation");
        await _auditRepo.AddAsync(log, ct);

        await _repo.SaveChangesAsync(ct);
        await _auditRepo.SaveChangesAsync(ct);

        return PaymentMapper.ToDto(payment);
    }
}
