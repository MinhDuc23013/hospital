using MediatR;
using PaymentService.Application.DTOs;

namespace PaymentService.Application.Queries;

public record GetInvoiceQuery(Guid AppointmentId, Guid PatientId) : IRequest<InvoiceDto>;
