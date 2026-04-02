using MediatR;
using NotificationServiceDotnet.Application.Commands;
using NotificationServiceDotnet.Infrastructure.Services;

namespace NotificationServiceDotnet.Application.Handlers;

public class SendEmailHandler : IRequestHandler<SendEmailCommand, bool>
{
    private readonly IEmailService _emailService;
    private readonly ILogger<SendEmailHandler> _logger;

    public SendEmailHandler(IEmailService emailService, ILogger<SendEmailHandler> logger)
    {
        _emailService = emailService;
        _logger = logger;
    }

    public async Task<bool> Handle(SendEmailCommand request, CancellationToken ct)
    {
        _logger.LogInformation("Sending email to {To}, subject: {Subject}", request.To, request.Subject);
        return await _emailService.SendAsync(request.To, request.Subject, request.Body, ct);
    }
}
