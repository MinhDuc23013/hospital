using MediatR;

namespace NotificationServiceDotnet.Application.Commands;

public record SendEmailCommand(
    string To,
    string Subject,
    string Body,
    string? ReferenceId = null,
    string? ReferenceType = null
) : IRequest<bool>;
