using MediatR;

namespace NotificationServiceDotnet.Application.Commands;

public record SendSmsCommand(
    string PhoneNumber,
    string Message,
    string? ReferenceId = null,
    string? ReferenceType = null
) : IRequest<bool>;
