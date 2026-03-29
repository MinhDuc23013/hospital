using HospitalShared.DTOs;
using MediatR;

namespace PaymentService.Application.Commands;

/// <summary>Complete a payment after external confirmation (webhook/callback from payment provider).</summary>
public record CompletePaymentCommand(Guid PaymentId, string TransactionId) : IRequest<PaymentDto>;
