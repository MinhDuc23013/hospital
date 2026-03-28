using HospitalShared.DTOs;
using MediatR;

namespace PaymentService.Application.Commands;

public record ProcessPaymentCommand(Guid PaymentId) : IRequest<PaymentDto>;
