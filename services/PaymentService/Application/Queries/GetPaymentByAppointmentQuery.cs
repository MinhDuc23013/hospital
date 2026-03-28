using HospitalShared.DTOs;
using MediatR;

namespace PaymentService.Application.Queries;

public record GetPaymentByAppointmentQuery(Guid AppointmentId) : IRequest<PaymentDto?>;
