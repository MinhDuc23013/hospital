using System.Text.Json;
using Confluent.Kafka;
using Elastic.Clients.Elasticsearch;
using HospitalShared.Kafka;
using SearchServiceDotnet.Application.Models;

namespace SearchServiceDotnet.Infrastructure.Kafka;

/// <summary>
/// BackgroundService that consumes hospital.patient-created and hospital.patient-updated
/// Kafka topics and indexes patient documents into Elasticsearch.
/// Consumer group: search-service. Poison messages ship to {topic}.dlq.
/// </summary>
public class PatientEventConsumer : BackgroundService
{
    private readonly IConfiguration _configuration;
    private readonly ElasticsearchClient _esClient;
    private readonly KafkaDlqPublisher _dlq;
    private readonly ILogger<PatientEventConsumer> _logger;

    private const string PatientIndex      = "hospital-patients";
    private const string ConsumerGroup     = "search-service";
    private const string TopicCreated      = "hospital.patient-created";
    private const string TopicUpdated      = "hospital.patient-updated";
    private const int MaxAttempts          = 3;

    // Accept both camelCase and PascalCase JSON property names
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public PatientEventConsumer(
        IConfiguration configuration,
        ElasticsearchClient esClient,
        KafkaDlqPublisher dlq,
        ILogger<PatientEventConsumer> logger)
    {
        _configuration = configuration;
        _esClient      = esClient;
        _dlq           = dlq;
        _logger        = logger;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Run blocking Kafka poll loop on a dedicated thread so it doesn't block the host.
        return Task.Run(() => ConsumeLoop(stoppingToken), stoppingToken);
    }

    private async Task ConsumeLoop(CancellationToken ct)
    {
        var bootstrap = _configuration["Kafka:BootstrapServers"] ?? "kafka:9092";

        var config = new ConsumerConfig
        {
            BootstrapServers       = bootstrap,
            GroupId                = ConsumerGroup,
            AutoOffsetReset        = AutoOffsetReset.Earliest,
            EnableAutoCommit       = false,
            SessionTimeoutMs       = 10000,
            MaxPollIntervalMs      = 300000
        };

        _logger.LogInformation("PatientEventConsumer starting — bootstrap={Bootstrap}", bootstrap);

        using var consumer = new ConsumerBuilder<string, string>(config).Build();
        consumer.Subscribe(new[] { TopicCreated, TopicUpdated });

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
            _logger.LogInformation("PatientEventConsumer stopped");
        }
        finally
        {
            consumer.Close();
        }
    }

    private async Task HandleMessageAsync(string topic, string json, CancellationToken ct)
    {
        var payload = JsonSerializer.Deserialize<PatientPayload>(json, JsonOpts)
            ?? throw new InvalidOperationException($"Null payload on topic {topic}");

        var doc = new PatientDocument
        {
            PatientId = payload.PatientId ?? payload.Id ?? string.Empty,
            FirstName = payload.FirstName ?? string.Empty,
            LastName  = payload.LastName  ?? string.Empty,
            Email     = payload.Email     ?? string.Empty,
            CreatedAt = payload.CreatedAt == default ? DateTime.UtcNow : payload.CreatedAt
        };

        if (string.IsNullOrWhiteSpace(doc.PatientId))
        {
            _logger.LogWarning("PatientId missing on topic {Topic} — skipping (not DLQ-worthy)", topic);
            return;
        }

        var response = await _esClient.IndexAsync(doc, idx => idx
            .Index(PatientIndex)
            .Id(doc.PatientId), ct);

        if (!response.IsValidResponse)
            throw new InvalidOperationException(
                $"Elasticsearch index failed for patient {doc.PatientId}: {response.DebugInformation}");

        _logger.LogInformation("Indexed patient {PatientId} from topic {Topic}", doc.PatientId, topic);
    }

    /// <summary>
    /// Accepts both camelCase and PascalCase, and both 'Id' and 'PatientId' field names
    /// as produced by different outbox event formats across services.
    /// </summary>
    private sealed class PatientPayload
    {
        public string?   Id        { get; set; }
        public string?   PatientId { get; set; }
        public string?   FirstName { get; set; }
        public string?   LastName  { get; set; }
        public string?   Email     { get; set; }
        public DateTime  CreatedAt { get; set; }
    }
}
