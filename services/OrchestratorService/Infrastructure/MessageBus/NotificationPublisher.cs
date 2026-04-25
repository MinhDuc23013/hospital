using System.Text;
using System.Text.Json;
using RabbitMQ.Client;

namespace OrchestratorService.Infrastructure.MessageBus;

/// <summary>
/// RabbitMQ publisher for notification commands (SMS/email).
/// Sends messages to NotificationService via RabbitMQ queue.
/// </summary>
public class NotificationPublisher : IDisposable
{
    private readonly IConnection _connection;
    private readonly IModel _channel;
    private readonly ILogger<NotificationPublisher> _logger;

    private const string ExchangeName = "hospital.notifications";
    private const string QueueName = "notification-service";

    public NotificationPublisher(IConfiguration config, ILogger<NotificationPublisher> logger)
    {
        _logger = logger;

        var factory = new ConnectionFactory
        {
            HostName = config["RabbitMQ:HostName"] ?? config["RabbitMQ:Host"] ?? "localhost",
            Port = int.TryParse(config["RabbitMQ:Port"], out var p) ? p : 5672,
            UserName = config["RabbitMQ:User"] ?? "guest",
            Password = config["RabbitMQ:Password"] ?? "guest"
        };

        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();

        _channel.ExchangeDeclare(ExchangeName, ExchangeType.Fanout, durable: true);
        _channel.QueueDeclare(QueueName, durable: true, exclusive: false, autoDelete: false);
        _channel.QueueBind(QueueName, ExchangeName, routingKey: "");

        _logger.LogInformation("RabbitMQ NotificationPublisher connected: {Host}:{Port}",
            factory.HostName, factory.Port);
    }

    /// <summary>Publish a notification command to RabbitMQ for SMS/email delivery.</summary>
    public Task SendNotificationAsync<T>(T message, CancellationToken ct = default) where T : class
    {
        var json = JsonSerializer.Serialize(message);
        var body = Encoding.UTF8.GetBytes(json);

        var properties = _channel.CreateBasicProperties();
        properties.Persistent = true;
        properties.ContentType = "application/json";
        properties.Type = typeof(T).Name;

        _channel.BasicPublish(
            exchange: ExchangeName,
            routingKey: "",
            basicProperties: properties,
            body: body);

        _logger.LogInformation("Sent {MessageType} to RabbitMQ queue {Queue}", typeof(T).Name, QueueName);
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _channel?.Dispose();
        _connection?.Dispose();
    }
}
