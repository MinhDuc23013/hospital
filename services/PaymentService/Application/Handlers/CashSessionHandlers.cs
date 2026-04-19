using HospitalShared.DTOs;
using HospitalShared.Events;
using MediatR;
using PaymentService.Application.Commands;
using PaymentService.Domain.Entities;
using PaymentService.Domain.Exceptions;
using PaymentService.Infrastructure.MessageBus;
using PaymentService.Infrastructure.Repositories;

namespace PaymentService.Application.Handlers;

/// <summary>Open a new cash session (cashier starts their shift).</summary>
public class OpenCashSessionHandler : IRequestHandler<OpenCashSessionCommand, CashSession>
{
    private readonly ICashSessionRepository _repo;

    public OpenCashSessionHandler(ICashSessionRepository repo) => _repo = repo;

    public async Task<CashSession> Handle(OpenCashSessionCommand cmd, CancellationToken ct)
    {
        // Only one open session per cashier
        var existing = await _repo.GetOpenSessionByCashierAsync(cmd.CashierId, ct);
        if (existing is not null)
            throw new DomainException($"Cashier {cmd.CashierId} already has an open session ({existing.Id}).");

        var session = CashSession.Open(cmd.CashierId, cmd.CashierName, cmd.CounterId, cmd.OpeningBalance);
        await _repo.AddAsync(session, ct);
        await _repo.SaveChangesAsync(ct);
        return session;
    }
}

/// <summary>Close cash session — cashier reports actual counted cash; system calculates variance.</summary>
public class CloseCashSessionHandler : IRequestHandler<CloseCashSessionCommand, CashSession>
{
    private readonly ICashSessionRepository _repo;

    public CloseCashSessionHandler(ICashSessionRepository repo) => _repo = repo;

    public async Task<CashSession> Handle(CloseCashSessionCommand cmd, CancellationToken ct)
    {
        var session = await _repo.GetByIdAsync(cmd.SessionId, ct)
            ?? throw new NotFoundException("CashSession", cmd.SessionId);

        session.Close(cmd.ActualCash, cmd.Notes);
        await _repo.SaveChangesAsync(ct);
        return session;
    }
}

/// <summary>
/// One-step cash payment completion at cashier counter.
/// Validates amount received ≥ invoice total, calculates change, generates receipt number,
/// updates cash session expected cash, publishes PaymentCompletedEvent.
/// </summary>
public class CompleteCashPaymentHandler : IRequestHandler<CompleteCashPaymentCommand, PaymentDto>
{
    private readonly IPaymentRepository _paymentRepo;
    private readonly IPaymentAuditLogRepository _auditRepo;
    private readonly ICashSessionRepository _sessionRepo;
    private readonly EventPublisher _events;

    public CompleteCashPaymentHandler(
        IPaymentRepository paymentRepo,
        IPaymentAuditLogRepository auditRepo,
        ICashSessionRepository sessionRepo,
        EventPublisher events)
    {
        _paymentRepo = paymentRepo;
        _auditRepo = auditRepo;
        _sessionRepo = sessionRepo;
        _events = events;
    }

    public async Task<PaymentDto> Handle(CompleteCashPaymentCommand cmd, CancellationToken ct)
    {
        var payment = await _paymentRepo.GetByIdAsync(cmd.PaymentId, ct)
            ?? throw new NotFoundException("Payment", cmd.PaymentId);

        var session = await _sessionRepo.GetByIdAsync(cmd.CashSessionId, ct)
            ?? throw new NotFoundException("CashSession", cmd.CashSessionId);

        if (session.Status != CashSessionStatus.Open)
            throw new DomainException("Cash session is not open.");

        var receiptNumber = await _sessionRepo.GenerateReceiptNumberAsync(ct);

        // Domain method validates Method=Cash, Status=Pending, amount ≥ total
        payment.CompleteCash(cmd.AmountReceived, cmd.CashierId, cmd.CashSessionId, receiptNumber);
        session.AddCashIn(payment.Amount);

        var log = PaymentAuditLog.Create(payment.Id, "CashCompleted",
            oldStatus: "Pending", newStatus: "Completed",
            message: $"Cash payment — receipt {receiptNumber}, received {cmd.AmountReceived}, change {payment.ChangeReturned}");
        await _auditRepo.AddAsync(log, ct);

        await _paymentRepo.SaveChangesAsync(ct);

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
