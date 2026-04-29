using System.Text.Json;
using Confluent.Kafka;
using HospitalShared.Events;
using HospitalShared.Kafka;
using Microsoft.EntityFrameworkCore;
using OrchestratorService.Application.Saga;
using OrchestratorService.Domain.Entities;
using OrchestratorService.Infrastructure.Persistence;

namespace OrchestratorService.Infrastructure.Consumers;

/// <summary>
/// Kafka consumer for BookingSlotLockedEvent.
/// Triggers the async booking phase: ConfirmSlot → ConfirmAppointment → SendNotification → IndexSearch.
///
/// Topic:   hospital.booking-slot-locked
/// GroupId: orchestrator-service
/// </summary>
public class BookingAsyncPhaseConsumer : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly KafkaDlqPublisher _dlq;
    private readonly ILogger<BookingAsyncPhaseConsumer> _logger;
    private readonly string _bootstrapServers;

    private const string Topic = "hospital.booking-slot-locked";
    private const string GroupId = "orchestrator-service";
    private const int MaxAttempts = 3;

    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    public BookingAsyncPhaseConsumer(
        IServiceScopeFactory scopeFactory,
        KafkaDlqPublisher dlq,
        IConfiguration config,
        ILogger<BookingAsyncPhaseConsumer> logger)
    {
        _scopeFactory = scopeFactory;
        _dlq = dlq;
        _logger = logger;
        _bootstrapServers = config["Kafka:BootstrapServers"] ?? "localhost:9092";
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("BookingAsyncPhaseConsumer starting, topic={Topic}", Topic);

        await Task.Yield(); // release startup thread

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
            }
            catch (ConsumeException ex)
            {
                _logger.LogError(ex, "Kafka consume error on topic {Topic}", Topic);
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in BookingAsyncPhaseConsumer loop");
            }
        }

        consumer.Close();
    }

    private async Task ProcessMessageAsync(ConsumeResult<string, string> result, CancellationToken ct)
    {
        var evt = JsonSerializer.Deserialize<BookingSlotLockedEvent>(result.Message.Value, JsonOpts)
            ?? throw new InvalidOperationException("BookingSlotLockedEvent deserialized to null");

        _logger.LogInformation(
            "BookingAsyncPhaseConsumer: received SagaId={SagaId}, AppointmentId={AppointmentId}",
            evt.SagaId, evt.AppointmentId);

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OrchestratorDbContext>();
        var orchestrator = scope.ServiceProvider.GetRequiredService<BookingSagaOrchestrator>();

        await using var tx = await db.Database.BeginTransactionAsync(ct);

        // Insert idempotency key — unique constraint on (EventId, ConsumerGroup)
        // Duplicate Kafka delivery hits the constraint and rolls back before touching business logic
        var idempotencyKey = ProcessedEvent.Create(evt.SagaId, GroupId);
        db.ProcessedEvents.Add(idempotencyKey);
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("duplicate") == true
                                        || ex.InnerException?.Message.Contains("unique") == true)
        {
            await tx.RollbackAsync(ct);
            _logger.LogInformation(
                "BookingAsyncPhaseConsumer: duplicate event SagaId={SagaId} — already processed, skipping",
                evt.SagaId);
            return;
        }

        try
        {
            await orchestrator.ExecuteAsyncPhaseAsync(evt.SagaId, ct);
            await tx.CommitAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            await tx.RollbackAsync(ct);
            _logger.LogWarning(
                "BookingAsyncPhaseConsumer: concurrency conflict on SagaId={SagaId} — another instance processed first, skipping",
                evt.SagaId);
        }
    }
}
