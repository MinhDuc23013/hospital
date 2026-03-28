using HospitalShared.DTOs;
using MediatR;
using PaymentService.Domain.Enums;

namespace PaymentService.Application.Commands;

public record CreatePaymentCommand(
    Guid AppointmentId,
    Guid PatientId,
    decimal Amount,
    string Currency,
    PaymentMethod Method,
    string? Description
) : IRequest<PaymentDto>;
