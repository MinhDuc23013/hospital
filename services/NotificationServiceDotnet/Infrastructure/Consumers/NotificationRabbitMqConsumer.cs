using System.Text;
using System.Text.Json;
using HospitalShared.Events;
using MediatR;
using NotificationServiceDotnet.Application.Commands;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace NotificationServiceDotnet.Infrastructure.Consumers;

/// <summary>
/// Consumes notification commands from RabbitMQ exchange "hospital.notifications".
/// Dispatches email/SMS via MediatR handlers.
/// </summary>
public class NotificationRabbitMqConsumer : BackgroundService
{
    private readonly IServiceProvider _sp;
    private readonly ILogger<NotificationRabbitMqConsumer> _logger;
    private readonly IConfiguration _config;

    private IConnection? _connection;
    private IModel? _channel;

    private const string ExchangeName = "hospital.notifications";
    private const string QueueName = "notification-service";

    public NotificationRabbitMqConsumer(
        IServiceProvider sp, IConfiguration config, ILogger<NotificationRabbitMqConsumer> logger)
    {
        _sp = sp;
        _config = config;
        _logger = logger;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            var factory = new ConnectionFactory
            {
                HostName = _config["RabbitMQ:HostName"] ?? "localhost",
                Port = int.TryParse(_config["RabbitMQ:Port"], out var p) ? p : 5672,
                UserName = _config["RabbitMQ:User"] ?? "guest",
                Password = _config["RabbitMQ:Password"] ?? "guest",
                DispatchConsumersAsync = true
            };

            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();

            _channel.ExchangeDeclare(ExchangeName, ExchangeType.Fanout, durable: true);
            _channel.QueueDeclare(QueueName, durable: true, exclusive: false, autoDelete: false);
            _channel.QueueBind(QueueName, ExchangeName, routingKey: "");
            _channel.BasicQos(0, 10, false);

            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.Received += OnMessageReceived;
            _channel.BasicConsume(QueueName, autoAck: false, consumer);

            _logger.LogInformation("RabbitMQ consumer started on queue {Queue}", QueueName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect to RabbitMQ — notifications will not be consumed");
        }

        return Task.CompletedTask;
    }

    private async Task OnMessageReceived(object sender, BasicDeliverEventArgs ea)
    {
        try
        {
            var json = Encoding.UTF8.GetString(ea.Body.Span);
            var messageType = ea.BasicProperties.Type ?? "Unknown";

            _logger.LogInformation("Received {MessageType} from RabbitMQ", messageType);

            using var scope = _sp.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

            await DispatchNotification(mediator, messageType, json);

            _channel?.BasicAck(ea.DeliveryTag, false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing notification message");
            _channel?.BasicNack(ea.DeliveryTag, false, requeue: true);
        }
    }

    /// <summary>Route incoming RabbitMQ messages to appropriate email/SMS commands.</summary>
    private static async Task DispatchNotification(IMediator mediator, string messageType, string json)
    {
        switch (messageType)
        {
            case nameof(AppointmentScheduledEvent):
                var evt = JsonSerializer.Deserialize<AppointmentScheduledEvent>(json);
                if (evt is null) return;

                // Send email notification for appointment
                await mediator.Send(new SendEmailCommand(
                    To: "", // resolved from patient lookup in production
                    Subject: $"Appointment Confirmation - {evt.ScheduledTime:yyyy-MM-dd HH:mm}",
                    Body: $"Your appointment has been scheduled on {evt.ScheduledTime:yyyy-MM-dd} at {evt.ScheduledTime:HH:mm} for {evt.DurationMinutes} minutes.",
                    ReferenceId: evt.AppointmentId.ToString(),
                    ReferenceType: "AppointmentScheduled"
                ));
                break;

            default:
                // Generic notification — try to send as email
                await mediator.Send(new SendEmailCommand(
                    To: "",
                    Subject: $"Hospital Notification - {messageType}",
                    Body: json,
                    ReferenceType: messageType
                ));
                break;
        }
    }

    public override void Dispose()
    {
        _channel?.Dispose();
        _connection?.Dispose();
        base.Dispose();
    }
}
