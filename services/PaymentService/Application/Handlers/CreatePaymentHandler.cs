using HospitalShared.DTOs;
using MediatR;
using PaymentService.Application.Commands;
using PaymentService.Domain.Entities;
using PaymentService.Domain.Exceptions;
using PaymentService.Infrastructure.HttpClients;
using PaymentService.Infrastructure.Repositories;

namespace PaymentService.Application.Handlers;

public class CreatePaymentHandler : IRequestHandler<CreatePaymentCommand, PaymentDto>
{
    private readonly IPaymentRepository _repo;
    private readonly IPaymentAuditLogRepository _auditRepo;
    private readonly AppointmentServiceClient _appointmentClient;
    private readonly ILogger<CreatePaymentHandler> _logger;

    public CreatePaymentHandler(
        IPaymentRepository repo,
        IPaymentAuditLogRepository auditRepo,
        AppointmentServiceClient appointmentClient,
        ILogger<CreatePaymentHandler> logger)
    {
        _repo = repo;
        _auditRepo = auditRepo;
        _appointmentClient = appointmentClient;
        _logger = logger;
    }

    public async Task<PaymentDto> Handle(CreatePaymentCommand cmd, CancellationToken ct)
    {
        // Validate appointment exists if possible (graceful degradation — payment can also be for prescriptions)
        var appointment = await _appointmentClient.GetAppointmentAsync(cmd.AppointmentId, ct);
        if (appointment is not null)
            _logger.LogInformation("Payment linked to appointment {AppointmentId}", cmd.AppointmentId);
        else
            _logger.LogInformation("Payment created with reference {ReferenceId} (no appointment found — may be prescription)", cmd.AppointmentId);

        var payment = Payment.Create(
            cmd.AppointmentId, cmd.PatientId, cmd.Amount, cmd.Currency, cmd.Method, cmd.Description);

        await _repo.AddAsync(payment, ct);
        await _repo.SaveChangesAsync(ct);

        var auditLog = PaymentAuditLog.Create(payment.Id, "Created", oldStatus: null, newStatus: "Pending");
        await _auditRepo.AddAsync(auditLog, ct);
        await _auditRepo.SaveChangesAsync(ct);

        return PaymentMapper.ToDto(payment);
    }
}
