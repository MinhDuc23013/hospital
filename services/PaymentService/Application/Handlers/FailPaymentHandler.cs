using HospitalShared.DTOs;
using HospitalShared.Events;
using MediatR;
using PaymentService.Application.Commands;
using PaymentService.Domain.Entities;
using PaymentService.Domain.Exceptions;
using PaymentService.Infrastructure.MessageBus;
using PaymentService.Infrastructure.Repositories;

namespace PaymentService.Application.Handlers;

/// <summary>Marks a payment as failed and publishes PaymentFailedEvent.</summary>
public class FailPaymentHandler : IRequestHandler<FailPaymentCommand, PaymentDto>
{
    private readonly IPaymentRepository _repo;
    private readonly IPaymentAuditLogRepository _auditRepo;
    private readonly EventPublisher _events;

    public FailPaymentHandler(IPaymentRepository repo, IPaymentAuditLogRepository auditRepo, EventPublisher events)
    {
        _repo = repo;
        _auditRepo = auditRepo;
        _events = events;
    }

    public async Task<PaymentDto> Handle(FailPaymentCommand cmd, CancellationToken ct)
    {
        var payment = await _repo.GetByIdAsync(cmd.PaymentId, ct)
            ?? throw new NotFoundException("Payment", cmd.PaymentId);

        // Idempotent — don't re-publish if already failed
        if (payment.Status == Domain.Enums.PaymentStatus.Failed)
            return PaymentMapper.ToDto(payment);

        var oldStatus = payment.Status.ToString();
        payment.Fail();

        var log = PaymentAuditLog.Create(payment.Id, "Failed",
            oldStatus: oldStatus, newStatus: "Failed",
            message: "Payment failed via provider webhook");
        await _auditRepo.AddAsync(log, ct);

        await _repo.SaveChangesAsync(ct);
        await _auditRepo.SaveChangesAsync(ct);

        await _events.PublishAsync(new PaymentFailedEvent
        {
            PaymentId = payment.Id,
            AppointmentId = payment.AppointmentId,
            PatientId = payment.PatientId,
            Amount = payment.Amount,
            Reason = "Provider declined",
            Timestamp = DateTime.Now
        }, ct);

        return PaymentMapper.ToDto(payment);
    }
}
