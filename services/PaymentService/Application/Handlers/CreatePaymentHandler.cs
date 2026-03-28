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

    public CreatePaymentHandler(
        IPaymentRepository repo,
        IPaymentAuditLogRepository auditRepo,
        AppointmentServiceClient appointmentClient)
    {
        _repo = repo;
        _auditRepo = auditRepo;
        _appointmentClient = appointmentClient;
    }

    public async Task<PaymentDto> Handle(CreatePaymentCommand cmd, CancellationToken ct)
    {
        // Validate appointment exists (graceful degradation if AppointmentService unavailable)
        var appointment = await _appointmentClient.GetAppointmentAsync(cmd.AppointmentId, ct);
        if (appointment is null)
            throw new DomainException($"Appointment {cmd.AppointmentId} not found or AppointmentService unavailable.");

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
