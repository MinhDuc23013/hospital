using System.Text.Json;
using AppointmentService.Application.Saga;
using AppointmentService.Infrastructure.Repositories;
using Confluent.Kafka;
using HospitalShared.Events;
using HospitalShared.Kafka;

namespace AppointmentService.Infrastructure.Consumers;

/// <summary>
/// Kafka consumer that listens for PaymentCompleted events
/// and completes the corresponding booking saga.
/// Replaces the HTTP webhook approach with reliable event-driven flow.
/// </summary>
public class PaymentCompletedConsumer : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly KafkaDlqPublisher _dlq;
    private readonly ILogger<PaymentCompletedConsumer> _logger;
    private readonly string _bootstrapServers;
    private const string Topic = "hospital.payment-completed";
    private const string GroupId = "appointment-service";
    private const int MaxAttempts = 3;

    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    public PaymentCompletedConsumer(
        IServiceScopeFactory scopeFactory,
        KafkaDlqPublisher dlq,
        IConfiguration config,
        ILogger<PaymentCompletedConsumer> logger)
    {
        _scopeFactory = scopeFactory;
        _dlq = dlq;
        _logger = logger;
        _bootstrapServers = config["Kafka:BootstrapServers"] ?? "localhost:9092";
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("PaymentCompletedConsumer starting, topic={Topic}", Topic);

        await Task.Yield(); // Release startup thread

        var config = new ConsumerConfig
        {
            BootstrapServers = _bootstrapServers,
            GroupId = GroupId,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false
        };

        using var consumer = new ConsumerBuilder<string, string>(config).Build();
        consumer.Subscribe(Topic);

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

                if (handled)
                    consumer.Commit(result);
                // else: don't commit → message redelivered next poll (DLQ publish failed).
            }
            catch (ConsumeException ex)
            {
                _logger.LogError(ex, "Kafka consume error on topic {Topic}", Topic);
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected consumer loop error on topic {Topic}", Topic);
            }
        }

        consumer.Close();
    }

    private async Task ProcessMessageAsync(ConsumeResult<string, string> result, CancellationToken ct)
    {
        var evt = JsonSerializer.Deserialize<PaymentCompletedEvent>(result.Message.Value, JsonOpts)
            ?? throw new InvalidOperationException("PaymentCompletedEvent payload deserialized to null");

        _logger.LogInformation(
            "Received PaymentCompletedEvent: PaymentId={PaymentId}, AppointmentId={AppointmentId}",
            evt.PaymentId, evt.AppointmentId);

        using var scope = _scopeFactory.CreateScope();
        var sagaRepo = scope.ServiceProvider.GetRequiredService<IBookingSagaRepository>();
        var orchestrator = scope.ServiceProvider.GetRequiredService<BookingSagaOrchestrator>();

        var saga = await sagaRepo.GetByPaymentIdAsync(evt.PaymentId, ct);
        if (saga is null)
        {
            _logger.LogWarning("No saga found for PaymentId={PaymentId}, skipping", evt.PaymentId);
            return;
        }

        // Idempotency: skip if saga already moved past AwaitingPayment
        if (saga.CurrentStep != AppointmentService.Domain.Enums.BookingSagaStep.AwaitingPayment)
        {
            _logger.LogInformation(
                "Saga {SagaId} already at step {Step}, duplicate PaymentCompletedEvent skipped",
                saga.Id, saga.CurrentStep);
            return;
        }

        await orchestrator.CompleteAfterPaymentAsync(saga.Id, ct);
        _logger.LogInformation("Saga {SagaId} completed via PaymentCompletedEvent", saga.Id);
    }
}
