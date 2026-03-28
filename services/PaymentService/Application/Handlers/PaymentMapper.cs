using HospitalShared.DTOs;
using PaymentService.Domain.Entities;

namespace PaymentService.Application.Handlers;

/// <summary>Static mapper from Payment domain entity to PaymentDto.</summary>
internal static class PaymentMapper
{
    internal static PaymentDto ToDto(Payment p) => new()
    {
        Id = p.Id,
        AppointmentId = p.AppointmentId,
        PatientId = p.PatientId,
        Amount = p.Amount,
        Currency = p.Currency,
        Method = p.Method.ToString(),
        Status = p.Status.ToString(),
        TransactionId = p.TransactionId,
        Description = p.Description,
        CreatedAt = p.CreatedAt,
        PaidAt = p.PaidAt
    };
}
