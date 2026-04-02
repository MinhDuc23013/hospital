namespace NotificationServiceDotnet.Infrastructure.Services;

public interface ISmsService
{
    Task<bool> SendAsync(string phoneNumber, string message, CancellationToken ct = default);
}
