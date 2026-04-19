using MediatR;
using PaymentService.Domain.Entities;

namespace PaymentService.Application.Commands;

public record OpenCashSessionCommand(
    string CashierId,
    string CashierName,
    string CounterId,
    decimal OpeningBalance
) : IRequest<CashSession>;

public record CloseCashSessionCommand(
    Guid SessionId,
    decimal ActualCash,
    string? Notes
) : IRequest<CashSession>;

public record CompleteCashPaymentCommand(
    Guid PaymentId,
    decimal AmountReceived,
    string CashierId,
    Guid CashSessionId
) : IRequest<HospitalShared.DTOs.PaymentDto>;
