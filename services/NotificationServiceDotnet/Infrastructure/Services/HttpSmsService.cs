namespace NotificationServiceDotnet.Infrastructure.Services;

/// <summary>
/// SMS service placeholder — logs SMS when provider is not configured.
/// Replace with Twilio/Vonage/AWS SNS implementation when ready.
/// </summary>
public class HttpSmsService : ISmsService
{
    private readonly IConfiguration _config;
    private readonly ILogger<HttpSmsService> _logger;

    public HttpSmsService(IConfiguration config, ILogger<HttpSmsService> logger)
    {
        _config = config;
        _logger = logger;
    }

    public Task<bool> SendAsync(string phoneNumber, string message, CancellationToken ct = default)
    {
        var accountSid = _config["Sms:AccountSid"];
        if (string.IsNullOrWhiteSpace(accountSid))
        {
            _logger.LogWarning("SMS provider not configured — SMS to {Phone} logged only. Message: {Message}",
                phoneNumber, message);
            return Task.FromResult(true);
        }

        // TODO: Implement actual SMS provider (Twilio, Vonage, AWS SNS)
        _logger.LogInformation("SMS sent to {Phone}: {Message}", phoneNumber, message);
        return Task.FromResult(true);
    }
}
