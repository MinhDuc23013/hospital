using System.Text.Json;
using Confluent.Kafka;
using Elastic.Clients.Elasticsearch;
using HospitalShared.Kafka;
using SearchServiceDotnet.Application.Models;

namespace SearchServiceDotnet.Infrastructure.Kafka;

/// <summary>
/// BackgroundService that consumes hospital.slot-reserved Kafka topic
/// and indexes slot documents into Elasticsearch.
/// Consumer group: search-service. Poison messages ship to hospital.slot-reserved.dlq.
/// </summary>
public class SlotEventConsumer : BackgroundService
{
    private readonly IConfiguration _configuration;
    private readonly ElasticsearchClient _esClient;
    private readonly KafkaDlqPublisher _dlq;
    private readonly ILogger<SlotEventConsumer> _logger;

    private const string SlotIndex      = "hospital-slots";
    private const string ConsumerGroup  = "search-service";
    private const string TopicReserved  = "hospital.slot-reserved";
    private const int MaxAttempts       = 3;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public SlotEventConsumer(
        IConfiguration configuration,
        ElasticsearchClient esClient,
        KafkaDlqPublisher dlq,
        ILogger<SlotEventConsumer> logger)
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

        _logger.LogInformation("SlotEventConsumer starting — bootstrap={Bootstrap}", bootstrap);

        using var consumer = new ConsumerBuilder<string, string>(config).Build();
        consumer.Subscribe(new[] { TopicReserved });

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

                if (handled)
                    consumer.Commit(result);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("SlotEventConsumer stopped");
        }
        finally
        {
            consumer.Close();
        }
    }

    private async Task HandleMessageAsync(string topic, string json, CancellationToken ct)
    {
        var payload = JsonSerializer.Deserialize<SlotPayload>(json, JsonOpts)
            ?? throw new InvalidOperationException($"Null payload on topic {topic}");

        var doc = new SlotDocument
        {
            SlotId          = payload.SlotId       ?? string.Empty,
            ScheduleId      = payload.ScheduleId   ?? string.Empty,
            DoctorId        = payload.DoctorId     ?? string.Empty,
            PatientId       = payload.PatientId    ?? string.Empty,
            ScheduledTime   = payload.ScheduledTime,
            DurationMinutes = payload.DurationMinutes,
            Status          = payload.Status       ?? string.Empty,
            ReservedUntil   = payload.ReservedUntil
        };

        if (string.IsNullOrWhiteSpace(doc.SlotId))
        {
            _logger.LogWarning("SlotId missing on topic {Topic} — skipping (not DLQ-worthy)", topic);
            return;
        }

        var response = await _esClient.IndexAsync(doc, idx => idx
            .Index(SlotIndex)
            .Id(doc.SlotId), ct);

        if (!response.IsValidResponse)
            throw new InvalidOperationException(
                $"Elasticsearch index failed for slot {doc.SlotId}: {response.DebugInformation}");

        _logger.LogInformation("Indexed slot {SlotId} from topic {Topic}", doc.SlotId, topic);
    }

    private sealed class SlotPayload
    {
        public string?   SlotId          { get; set; }
        public string?   ScheduleId      { get; set; }
        public string?   DoctorId        { get; set; }
        public string?   PatientId       { get; set; }
        public DateTime  ScheduledTime   { get; set; }
        public int       DurationMinutes { get; set; }
        public string?   Status          { get; set; }
        public DateTime? ReservedUntil   { get; set; }
    }
}
