using System.Text.Json;
using Confluent.Kafka;
using HospitalShared.Events;
using HospitalShared.Kafka;
using OrchestratorService.Application.Saga;

namespace OrchestratorService.Infrastructure.Consumers;

/// <summary>
/// Kafka consumer for payment outcome events.
/// Handles Step 4-5 of the payment saga: listen for success/failure and update saga state.
///
/// Topics:  hospital.payment-completed  →  mark saga Completed
///          hospital.payment-failed     →  mark saga Failed (allows retry)
/// GroupId: orchestrator-payment
/// </summary>
public class PaymentEventConsumer : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly KafkaDlqPublisher _dlq;
    private readonly ILogger<PaymentEventConsumer> _logger;
    private readonly string _bootstrapServers;

    private const string TopicCompleted = "hospital.payment-completed";
    private const string TopicFailed = "hospital.payment-failed";
    private const string GroupId = "orchestrator-payment";
    private const int MaxAttempts = 3;

    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    public PaymentEventConsumer(
        IServiceScopeFactory scopeFactory,
        KafkaDlqPublisher dlq,
        IConfiguration config,
        ILogger<PaymentEventConsumer> logger)
    {
        _scopeFactory = scopeFactory;
        _dlq = dlq;
        _logger = logger;
        _bootstrapServers = config["Kafka:BootstrapServers"] ?? "localhost:9092";
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("PaymentEventConsumer starting, topics={Completed},{Failed}",
            TopicCompleted, TopicFailed);

        await Task.Yield();

        var config = new ConsumerConfig
        {
            BootstrapServers = _bootstrapServers,
            GroupId = GroupId,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false
        };

        using var consumer = new ConsumerBuilder<string, string>(config).Build();
        consumer.Subscribe(new[] { TopicCompleted, TopicFailed });

        while (!stoppingToken.IsCancellationRequested)
        {
            ConsumeResult<string, string>? result = null;
            try
            {
                result = consumer.Consume(stoppingToken);
                if (result?.Message?.Value is null) continue;

                var handled = await KafkaConsumerRetryHelper.HandleWithDlqAsync(
                    result,
                    handler: ct => ProcessMessageAsync(result, ct),
                    _dlq,
                    GroupId,
                    _logger,
                    stoppingToken,
                    maxAttempts: MaxAttempts);

                consumer.Commit(result);
            }
            catch (ConsumeException ex)
            {
                _logger.LogError(ex, "Kafka consume error in PaymentEventConsumer");
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in PaymentEventConsumer loop");
            }
        }

        consumer.Close();
    }

    private async Task ProcessMessageAsync(ConsumeResult<string, string> result, CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var orchestrator = scope.ServiceProvider.GetRequiredService<PaymentSagaOrchestrator>();

        if (result.Topic == TopicCompleted)
        {
            var evt = JsonSerializer.Deserialize<PaymentCompletedEvent>(result.Message.Value, JsonOpts)
                ?? throw new InvalidOperationException("PaymentCompletedEvent deserialized to null");

            _logger.LogInformation("PaymentEventConsumer: completed PaymentId={PaymentId}", evt.PaymentId);
            await orchestrator.HandleCompletedAsync(evt.PaymentId, ct);
        }
        else
        {
            var evt = JsonSerializer.Deserialize<PaymentFailedEvent>(result.Message.Value, JsonOpts)
                ?? throw new InvalidOperationException("PaymentFailedEvent deserialized to null");

            _logger.LogInformation("PaymentEventConsumer: failed PaymentId={PaymentId}", evt.PaymentId);
            await orchestrator.HandleFailedAsync(evt.PaymentId, evt.Reason, ct);
        }
    }
}
