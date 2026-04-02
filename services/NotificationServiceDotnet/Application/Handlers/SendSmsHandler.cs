using MediatR;
using NotificationServiceDotnet.Application.Commands;
using NotificationServiceDotnet.Infrastructure.Services;

namespace NotificationServiceDotnet.Application.Handlers;

public class SendSmsHandler : IRequestHandler<SendSmsCommand, bool>
{
    private readonly ISmsService _smsService;
    private readonly ILogger<SendSmsHandler> _logger;

    public SendSmsHandler(ISmsService smsService, ILogger<SendSmsHandler> logger)
    {
        _smsService = smsService;
        _logger = logger;
    }

    public async Task<bool> Handle(SendSmsCommand request, CancellationToken ct)
    {
        _logger.LogInformation("Sending SMS to {Phone}", request.PhoneNumber);
        return await _smsService.SendAsync(request.PhoneNumber, request.Message, ct);
    }
}
