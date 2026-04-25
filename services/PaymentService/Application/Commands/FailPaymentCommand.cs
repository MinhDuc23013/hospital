using HospitalShared.DTOs;
using MediatR;

namespace PaymentService.Application.Commands;

/// <summary>Mark a payment as failed and publish PaymentFailedEvent.</summary>
public record FailPaymentCommand(Guid PaymentId) : IRequest<PaymentDto>;
