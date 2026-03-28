using HospitalShared.DTOs;
using HospitalShared.Events;
using MediatR;
using PaymentService.Application.Commands;
using PaymentService.Domain.Entities;
using PaymentService.Domain.Exceptions;
using PaymentService.Infrastructure.MessageBus;
using PaymentService.Infrastructure.Repositories;

namespace PaymentService.Application.Handlers;

public class ProcessPaymentHandler : IRequestHandler<ProcessPaymentCommand, PaymentDto>
{
    private readonly IPaymentRepository _repo;
    private readonly IPaymentAuditLogRepository _auditRepo;
    private readonly EventPublisher _events;

    public ProcessPaymentHandler(IPaymentRepository repo, IPaymentAuditLogRepository auditRepo, EventPublisher events)
    {
        _repo = repo;
        _auditRepo = auditRepo;
        _events = events;
    }

    public async Task<PaymentDto> Handle(ProcessPaymentCommand cmd, CancellationToken ct)
    {
        var payment = await _repo.GetByIdAsync(cmd.PaymentId, ct)
            ?? throw new NotFoundException("Payment", cmd.PaymentId);

        // Transition to Processing then simulate auto-complete
        payment.Process();

        var processingLog = PaymentAuditLog.Create(payment.Id, "Processing", oldStatus: "Pending", newStatus: "Processing");
        await _auditRepo.AddAsync(processingLog, ct);

        // Simulate successful payment processing — generate external transaction ref
        var transactionId = Guid.NewGuid().ToString();
        payment.Complete(transactionId);

        var completedLog = PaymentAuditLog.Create(payment.Id, "Completed", oldStatus: "Processing", newStatus: "Completed");
        await _auditRepo.AddAsync(completedLog, ct);

        await _repo.SaveChangesAsync(ct);
        await _auditRepo.SaveChangesAsync(ct);

        await _events.PublishAsync(new PaymentCompletedEvent
        {
            PaymentId = payment.Id,
            AppointmentId = payment.AppointmentId,
            PatientId = payment.PatientId,
            Amount = payment.Amount,
            Method = payment.Method.ToString(),
            Timestamp = DateTime.UtcNow
        }, ct);

        return PaymentMapper.ToDto(payment);
    }
}
