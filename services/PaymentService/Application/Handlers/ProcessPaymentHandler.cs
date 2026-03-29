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

        // Transition to Processing — actual payment completion comes from external callback
        payment.Process();

        var processingLog = PaymentAuditLog.Create(payment.Id, "Processing", oldStatus: "Pending", newStatus: "Processing");
        await _auditRepo.AddAsync(processingLog, ct);

        await _repo.SaveChangesAsync(ct);
        await _auditRepo.SaveChangesAsync(ct);

        return PaymentMapper.ToDto(payment);
    }
}
