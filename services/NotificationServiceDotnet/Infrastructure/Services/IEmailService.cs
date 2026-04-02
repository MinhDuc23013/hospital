namespace NotificationServiceDotnet.Infrastructure.Services;

public interface IEmailService
{
    Task<bool> SendAsync(string to, string subject, string body, CancellationToken ct = default);
}
