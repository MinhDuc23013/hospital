using MediatR;

namespace LabTestService.Application.Commands;

public record CancelLabOrderCommand(Guid OrderId) : IRequest<bool>;
