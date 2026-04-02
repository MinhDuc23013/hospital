using MailKit.Net.Smtp;
using MimeKit;

namespace NotificationServiceDotnet.Infrastructure.Services;

/// <summary>
/// Sends emails via SMTP using MailKit.
/// Falls back to logging when SMTP host is not configured.
/// </summary>
public class SmtpEmailService : IEmailService
{
    private readonly IConfiguration _config;
    private readonly ILogger<SmtpEmailService> _logger;

    public SmtpEmailService(IConfiguration config, ILogger<SmtpEmailService> logger)
    {
        _config = config;
        _logger = logger;
    }

    public async Task<bool> SendAsync(string to, string subject, string body, CancellationToken ct = default)
    {
        var host = _config["Smtp:Host"];
        if (string.IsNullOrWhiteSpace(host))
        {
            _logger.LogWarning("SMTP not configured — email to {To} logged only. Subject: {Subject}", to, subject);
            return true;
        }

        try
        {
            var message = new MimeMessage();
            message.From.Add(MailboxAddress.Parse(_config["Smtp:From"] ?? "noreply@hospital.local"));
            message.To.Add(MailboxAddress.Parse(to));
            message.Subject = subject;
            message.Body = new TextPart("html") { Text = body };

            using var client = new SmtpClient();
            var port = int.TryParse(_config["Smtp:Port"], out var p) ? p : 587;
            var useSsl = bool.TryParse(_config["Smtp:EnableSsl"], out var ssl) && ssl;

            await client.ConnectAsync(host, port, useSsl, ct);

            var user = _config["Smtp:User"];
            if (!string.IsNullOrWhiteSpace(user))
                await client.AuthenticateAsync(user, _config["Smtp:Password"], ct);

            await client.SendAsync(message, ct);
            await client.DisconnectAsync(true, ct);

            _logger.LogInformation("Email sent to {To}, subject: {Subject}", to, subject);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {To}", to);
            return false;
        }
    }
}
