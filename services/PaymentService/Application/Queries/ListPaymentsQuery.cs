using HospitalShared.DTOs;
using MediatR;
using PaymentService.Domain.Enums;

namespace PaymentService.Application.Queries;

public record ListPaymentsQuery(
    Guid? AppointmentId,
    Guid? PatientId,
    PaymentStatus? Status,
    int Page = 1,
    int PageSize = 50
) : IRequest<(List<PaymentDto> Items, int Total)>;
