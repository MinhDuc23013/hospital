using MediatR;

namespace AppointmentService.Application.Commands;

/// <summary>Triggered when external payment is confirmed (via webhook or Kafka consumer).</summary>
public record ConfirmPaymentCommand(Guid SagaId) : IRequest;
