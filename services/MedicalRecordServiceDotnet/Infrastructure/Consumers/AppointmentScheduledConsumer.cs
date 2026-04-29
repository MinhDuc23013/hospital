using System.Text.Json;
using Confluent.Kafka;
using HospitalShared.Events;
using HospitalShared.Kafka;
using MedicalRecordServiceDotnet.Domain.Entities;
using MedicalRecordServiceDotnet.Infrastructure.Repositories;
using MongoDB.Bson;
using MongoDB.Driver;

namespace MedicalRecordServiceDotnet.Infrastructure.Consumers;

/// <summary>
/// Kafka consumer that listens for AppointmentScheduled events
/// and auto-creates an empty medical record for the appointment.
///
/// Idempotency: INSERT into processed_events (unique index on EventId + ConsumerGroup).
/// Retry: KafkaConsumerRetryHelper — 3 attempts with exponential backoff, then DLQ.
/// EnableAutoCommit=false: offset committed only after successful processing or DLQ ship.
/// </summary>
public class AppointmentScheduledConsumer : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly KafkaDlqPublisher _dlq;
    private readonly ILogger<AppointmentScheduledConsumer> _logger;
    private readonly string _bootstrapServers;
    private const string Topic = "hospital.appointment-scheduled";
    private const string GroupId = "medical-record-service";
    private const int MaxAttempts = 3;

    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    public AppointmentScheduledConsumer(
        IServiceScopeFactory scopeFactory,
        KafkaDlqPublisher dlq,
        IConfiguration config,
        ILogger<AppointmentScheduledConsumer> logger)
    {
        _scopeFactory = scopeFactory;
        _dlq = dlq;
        _logger = logger;
        _bootstrapServers = config["Kafka:BootstrapServers"] ?? "localhost:9092";
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("AppointmentScheduledConsumer starting, topic={Topic}", Topic);

        await EnsureProcessedEventsIndexAsync(stoppingToken);
        await Task.Yield();

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
            try
            {
                var result = consumer.Consume(stoppingToken);
                if (result?.Message?.Value is null) continue;

                var evt = JsonSerializer.Deserialize<AppointmentScheduledEvent>(result.Message.Value, JsonOpts);
                if (evt is null) continue;

                var handled = await KafkaConsumerRetryHelper.HandleWithDlqAsync(
                    result,
                    handler: ct => ProcessMessageAsync(evt, ct),
                    _dlq,
                    GroupId,
                    _logger,
                    stoppingToken,
                    maxAttempts: MaxAttempts);

                consumer.Commit(result);
            }
            catch (ConsumeException ex)
            {
                _logger.LogError(ex, "Kafka consume error");
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing AppointmentScheduled event");
            }
        }

        consumer.Close();
    }

    private async Task ProcessMessageAsync(AppointmentScheduledEvent evt, CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IMongoDatabase>();
        var processedEvents = db.GetCollection<ProcessedEventDoc>("processed_events");

        // Insert idempotency key — unique index on (EventId, ConsumerGroup)
        // Duplicate Kafka delivery hits the constraint → skip before touching business logic
        try
        {
            await processedEvents.InsertOneAsync(new ProcessedEventDoc
            {
                EventId = evt.AppointmentId.ToString(),
                ConsumerGroup = GroupId,
                ProcessedAt = DateTime.UtcNow
            }, cancellationToken: ct);
        }
        catch (MongoWriteException ex) when (ex.WriteError.Category == ServerErrorCategory.DuplicateKey)
        {
            _logger.LogInformation(
                "AppointmentScheduledConsumer: duplicate AppointmentId={Id} — already processed, skipping",
                evt.AppointmentId);
            return;
        }

        var repo = scope.ServiceProvider.GetRequiredService<IMedicalRecordRepository>();
        var record = new MedicalRecord
        {
            PatientId = evt.PatientId.ToString(),
            AppointmentId = evt.AppointmentId.ToString(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await repo.CreateAsync(record, ct);
        _logger.LogInformation(
            "Auto-created medical record {RecordId} for appointment {AppointmentId}",
            record.Id, evt.AppointmentId);
    }

    private async Task EnsureProcessedEventsIndexAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IMongoDatabase>();
        var collection = db.GetCollection<ProcessedEventDoc>("processed_events");
        var keys = Builders<ProcessedEventDoc>.IndexKeys
            .Ascending(e => e.EventId)
            .Ascending(e => e.ConsumerGroup);
        await collection.Indexes.CreateOneAsync(
            new CreateIndexModel<ProcessedEventDoc>(keys, new CreateIndexOptions { Unique = true }),
            cancellationToken: ct);
    }

    private sealed class ProcessedEventDoc
    {
        public ObjectId Id { get; set; }
        public string EventId { get; set; } = string.Empty;
        public string ConsumerGroup { get; set; } = string.Empty;
        public DateTime ProcessedAt { get; set; }
    }
}
