using HospitalShared.DTOs;
using MediatR;

namespace PaymentService.Application.Queries;

public record GetPaymentQuery(Guid Id) : IRequest<PaymentDto?>;
