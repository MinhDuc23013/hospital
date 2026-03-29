using HospitalShared.DTOs;
using MediatR;

namespace PaymentService.Application.Commands;

/// <summary>Cancel a pending/processing payment (booking cancelled before payment completed).</summary>
public record CancelPaymentCommand(Guid PaymentId) : IRequest<PaymentDto>;
