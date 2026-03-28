using HospitalShared.DTOs;
using MediatR;

namespace PaymentService.Application.Commands;

public record RefundPaymentCommand(Guid PaymentId) : IRequest<PaymentDto>;
