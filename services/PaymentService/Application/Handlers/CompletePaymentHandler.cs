using HospitalShared.DTOs;
using HospitalShared.Events;
using MediatR;
using PaymentService.Application.Commands;
using PaymentService.Domain.Entities;
using PaymentService.Domain.Exceptions;
using PaymentService.Infrastructure.MessageBus;
using PaymentService.Infrastructure.Repositories;

namespace PaymentService.Application.Handlers;

/// <summary>Completes a payment after external provider confirmation (webhook/callback).</summary>
public class CompletePaymentHandler : IRequestHandler<CompletePaymentCommand, PaymentDto>
{
    private readonly IPaymentRepository _repo;
    private readonly IPaymentAuditLogRepository _auditRepo;
    private readonly EventPublisher _events;

    public CompletePaymentHandler(IPaymentRepository repo, IPaymentAuditLogRepository auditRepo, EventPublisher events)
    {
        _repo = repo;
        _auditRepo = auditRepo;
        _events = events;
    }

    public async Task<PaymentDto> Handle(CompletePaymentCommand cmd, CancellationToken ct)
    {
        var payment = await _repo.GetByIdAsync(cmd.PaymentId, ct)
            ?? throw new NotFoundException("Payment", cmd.PaymentId);

        payment.Complete(cmd.TransactionId);

        var log = PaymentAuditLog.Create(payment.Id, "Completed", oldStatus: "Processing", newStatus: "Completed",
            message: $"External transaction: {cmd.TransactionId}");
        await _auditRepo.AddAsync(log, ct);

        await _repo.SaveChangesAsync(ct);
        await _auditRepo.SaveChangesAsync(ct);

        await _events.PublishAsync(new PaymentCompletedEvent
        {
            PaymentId = payment.Id,
            AppointmentId = payment.AppointmentId,
            PatientId = payment.PatientId,
            Amount = payment.Amount,
            Method = payment.Method.ToString(),
            Timestamp = DateTime.Now
        }, ct);

        return PaymentMapper.ToDto(payment);
    }
}
