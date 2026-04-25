using MediatR;

namespace ImagingService.Application.Commands;

public record CancelImagingOrderCommand(Guid OrderId) : IRequest<bool>;
