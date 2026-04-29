using System.Text.Json;
using Confluent.Kafka;
using Elastic.Clients.Elasticsearch;
using HospitalShared.Kafka;
using SearchServiceDotnet.Application.Models;

namespace SearchServiceDotnet.Infrastructure.Kafka;

/// <summary>
/// BackgroundService that consumes hospital.appointment-scheduled Kafka topic
/// and indexes appointment documents into Elasticsearch.
/// Consumer group: search-service. Poison messages ship to hospital.appointment-scheduled.dlq.
/// </summary>
public class AppointmentEventConsumer : BackgroundService
{
    private readonly IConfiguration _configuration;
    private readonly ElasticsearchClient _esClient;
    private readonly KafkaDlqPublisher _dlq;
    private readonly ILogger<AppointmentEventConsumer> _logger;

    private const string AppointmentIndex = "hospital-appointments";
    private const string ConsumerGroup    = "search-service";
    private const string TopicScheduled   = "hospital.appointment-scheduled";
    private const int MaxAttempts         = 3;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public AppointmentEventConsumer(
        IConfiguration configuration,
        ElasticsearchClient esClient,
        KafkaDlqPublisher dlq,
        ILogger<AppointmentEventConsumer> logger)
    {
        _configuration = configuration;
        _esClient      = esClient;
        _dlq           = dlq;
        _logger        = logger;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return Task.Run(() => ConsumeLoop(stoppingToken), stoppingToken);
    }

    private async Task ConsumeLoop(CancellationToken ct)
    {
        var bootstrap = _configuration["Kafka:BootstrapServers"] ?? "kafka:9092";

        var config = new ConsumerConfig
        {
            BootstrapServers  = bootstrap,
            GroupId           = ConsumerGroup,
            AutoOffsetReset   = AutoOffsetReset.Earliest,
            EnableAutoCommit  = false,
            SessionTimeoutMs  = 10000,
            MaxPollIntervalMs = 300000
        };

        _logger.LogInformation("AppointmentEventConsumer starting — bootstrap={Bootstrap}", bootstrap);

        using var consumer = new ConsumerBuilder<string, string>(config).Build();
        consumer.Subscribe(new[] { TopicScheduled });

        try
        {
            while (!ct.IsCancellationRequested)
            {
                ConsumeResult<string, string>? result = null;
                try
                {
                    result = consumer.Consume(ct);
                }
                catch (ConsumeException ex)
                {
                    _logger.LogError(ex, "Kafka consume error: {Reason}", ex.Error.Reason);
                    await Task.Delay(2000, ct);
                    continue;
                }

                if (result?.Message?.Value is null) continue;

                var handled = await KafkaConsumerRetryHelper.HandleWithDlqAsync(
                    result,
                    handler: innerCt => HandleMessageAsync(result.Topic, result.Message.Value, innerCt),
                    _dlq,
                    ConsumerGroup,
                    _logger,
                    ct,
                    maxAttempts: MaxAttempts);

                consumer.Commit(result);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("AppointmentEventConsumer stopped");
        }
        finally
        {
            consumer.Close();
        }
    }

    private async Task HandleMessageAsync(string topic, string json, CancellationToken ct)
    {
        var payload = JsonSerializer.Deserialize<AppointmentPayload>(json, JsonOpts)
            ?? throw new InvalidOperationException($"Null payload on topic {topic}");

        var doc = new AppointmentDocument
        {
            AppointmentId  = payload.AppointmentId ?? string.Empty,
            PatientId      = payload.PatientId     ?? string.Empty,
            DoctorId       = payload.DoctorId      ?? string.Empty,
            ScheduledTime  = payload.ScheduledTime,
            DurationMinutes = payload.DurationMinutes,
            Status         = payload.Status        ?? string.Empty,
            CreatedAt      = payload.CreatedAt
        };

        if (string.IsNullOrWhiteSpace(doc.AppointmentId))
        {
            _logger.LogWarning("AppointmentId missing on topic {Topic} — skipping (not DLQ-worthy)", topic);
            return;
        }

        var response = await _esClient.IndexAsync(doc, idx => idx
            .Index(AppointmentIndex)
            .Id(doc.AppointmentId), ct);

        if (!response.IsValidResponse)
            throw new InvalidOperationException(
                $"Elasticsearch index failed for appointment {doc.AppointmentId}: {response.DebugInformation}");

        _logger.LogInformation("Indexed appointment {AppointmentId} from topic {Topic}", doc.AppointmentId, topic);
    }

    private sealed class AppointmentPayload
    {
        public string?   AppointmentId   { get; set; }
        public string?   PatientId       { get; set; }
        public string?   DoctorId        { get; set; }
        public DateTime  ScheduledTime   { get; set; }
        public int       DurationMinutes { get; set; }
        public string?   Status          { get; set; }
        public DateTime  CreatedAt       { get; set; }
    }
}
