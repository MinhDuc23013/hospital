using System.Text.Json;
using Confluent.Kafka;
using Elastic.Clients.Elasticsearch;
using SearchServiceDotnet.Application.Models;

namespace SearchServiceDotnet.Infrastructure.Kafka;

/// <summary>
/// BackgroundService that consumes hospital.appointment-scheduled Kafka topic
/// and indexes appointment documents into Elasticsearch.
/// Consumer group: search-service
/// </summary>
public class AppointmentEventConsumer : BackgroundService
{
    private readonly IConfiguration _configuration;
    private readonly ElasticsearchClient _esClient;
    private readonly ILogger<AppointmentEventConsumer> _logger;

    private const string AppointmentIndex = "hospital-appointments";
    private const string ConsumerGroup    = "search-service";
    private const string TopicScheduled   = "hospital.appointment-scheduled";

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public AppointmentEventConsumer(
        IConfiguration configuration,
        ElasticsearchClient esClient,
        ILogger<AppointmentEventConsumer> logger)
    {
        _configuration = configuration;
        _esClient      = esClient;
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

                await HandleMessageAsync(result.Topic, result.Message.Value, ct);
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
        try
        {
            var payload = JsonSerializer.Deserialize<AppointmentPayload>(json, JsonOpts);
            if (payload is null)
            {
                _logger.LogWarning("Received null payload on topic {Topic}", topic);
                return;
            }

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
                _logger.LogWarning("AppointmentId missing in message from topic {Topic} — skipping", topic);
                return;
            }

            var response = await _esClient.IndexAsync(doc, idx => idx
                .Index(AppointmentIndex)
                .Id(doc.AppointmentId), ct);

            if (response.IsValidResponse)
                _logger.LogInformation("Indexed appointment {AppointmentId} from topic {Topic}", doc.AppointmentId, topic);
            else
                _logger.LogWarning("Failed to index appointment {AppointmentId}: {Debug}", doc.AppointmentId, response.DebugInformation);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "JSON deserialization failed for message on topic {Topic}", topic);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error handling message on topic {Topic}", topic);
        }
    }

    // ── Internal DTO ─────────────────────────────────────────────────────────

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
