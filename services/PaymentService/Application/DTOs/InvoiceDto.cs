namespace PaymentService.Application.DTOs;

public record InvoiceLineItem(
    string Category,
    string Description,
    int Quantity,
    decimal UnitPrice,
    decimal Total);

public record InvoiceDto(
    Guid AppointmentId,
    Guid PatientId,
    List<InvoiceLineItem> Items,
    decimal Subtotal,
    decimal Total,
    string Currency,
    DateTime GeneratedAt);
