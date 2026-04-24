using HospitalShared.DTOs;
using MediatR;
using PaymentService.Application.Commands;
using PaymentService.Domain.Entities;
using PaymentService.Domain.Enums;
using PaymentService.Domain.Exceptions;
using PaymentService.Infrastructure.HttpClients;
using PaymentService.Infrastructure.MessageBus;
using PaymentService.Infrastructure.Repositories;

namespace PaymentService.Application.Handlers;

public class ProcessPaymentHandler : IRequestHandler<ProcessPaymentCommand, PaymentDto>
{
    private readonly IPaymentRepository _repo;
    private readonly IPaymentAuditLogRepository _auditRepo;
    private readonly EventPublisher _events;
    private readonly IPaymentProviderClient _provider;
    private readonly IConfiguration _config;

    public ProcessPaymentHandler(IPaymentRepository repo, IPaymentAuditLogRepository auditRepo,
        EventPublisher events, IPaymentProviderClient provider, IConfiguration config)
    {
        _repo = repo;
        _auditRepo = auditRepo;
        _events = events;
        _provider = provider;
        _config = config;
    }

    public async Task<PaymentDto> Handle(ProcessPaymentCommand cmd, CancellationToken ct)
    {
        var payment = await _repo.GetByIdAsync(cmd.PaymentId, ct)
            ?? throw new NotFoundException("Payment", cmd.PaymentId);

        payment.Process();

        var log = PaymentAuditLog.Create(payment.Id, "Processing", oldStatus: "Pending", newStatus: "Processing");
        await _auditRepo.AddAsync(log, ct);

        await _repo.SaveChangesAsync(ct);
        await _auditRepo.SaveChangesAsync(ct);

        var dto = PaymentMapper.ToDto(payment);

        // For online payments, call external provider to get checkout URL
        if (payment.Method != PaymentMethod.Cash)
        {
            var selfBaseUrl = _config["Services:SelfBaseUrl"] ?? "http://localhost:5008";
            var frontendUrl = _config["Services:FrontendUrl"] ?? "http://localhost:3000";

            var webhookUrl = $"{selfBaseUrl}/api/payments/{payment.Id}/provider-webhook";
            var returnUrl = $"{frontendUrl}/payment/result";

            try
            {
                var result = await _provider.InitiateAsync(
                    payment.Id, payment.Amount, payment.Currency,
                    payment.Method.ToString(), webhookUrl, returnUrl, ct);

                dto.CheckoutUrl = result.CheckoutUrl;
            }
            catch (Exception ex)
            {
                // Log and continue — payment is still in Processing state
                // Frontend can poll or use the complete endpoint manually
                _ = ex;
            }
        }

        return dto;
    }
}
