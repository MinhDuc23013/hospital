using HospitalShared.DTOs;
using MediatR;
using PaymentService.Application.Commands;
using PaymentService.Domain.Entities;
using PaymentService.Domain.Exceptions;
using PaymentService.Infrastructure.Repositories;

namespace PaymentService.Application.Handlers;

public class RefundPaymentHandler : IRequestHandler<RefundPaymentCommand, PaymentDto>
{
    private readonly IPaymentRepository _repo;
    private readonly IPaymentAuditLogRepository _auditRepo;

    public RefundPaymentHandler(IPaymentRepository repo, IPaymentAuditLogRepository auditRepo)
    {
        _repo = repo;
        _auditRepo = auditRepo;
    }

    public async Task<PaymentDto> Handle(RefundPaymentCommand cmd, CancellationToken ct)
    {
        var payment = await _repo.GetByIdAsync(cmd.PaymentId, ct)
            ?? throw new NotFoundException("Payment", cmd.PaymentId);

        payment.Refund();

        var auditLog = PaymentAuditLog.Create(payment.Id, "Refunded", oldStatus: "Completed", newStatus: "Refunded");
        await _auditRepo.AddAsync(auditLog, ct);

        await _repo.SaveChangesAsync(ct);
        await _auditRepo.SaveChangesAsync(ct);

        return PaymentMapper.ToDto(payment);
    }
}
