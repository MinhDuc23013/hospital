using System.Text.Json;
using Confluent.Kafka;
using Elastic.Clients.Elasticsearch;
using HospitalShared.Kafka;
using SearchServiceDotnet.Application.Models;

namespace SearchServiceDotnet.Infrastructure.Kafka;

/// <summary>
/// BackgroundService that consumes hospital.payment-completed Kafka topic
/// and indexes payment documents into Elasticsearch.
/// Consumer group: search-service. Poison messages ship to hospital.payment-completed.dlq.
/// </summary>
public class PaymentEventConsumer : BackgroundService
{
    private readonly IConfiguration _configuration;
    private readonly ElasticsearchClient _esClient;
    private readonly KafkaDlqPublisher _dlq;
    private readonly ILogger<PaymentEventConsumer> _logger;

    private const string PaymentIndex    = "hospital-payments";
    private const string ConsumerGroup   = "search-service";
    private const string TopicCompleted  = "hospital.payment-completed";
    private const int MaxAttempts        = 3;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public PaymentEventConsumer(
        IConfiguration configuration,
        ElasticsearchClient esClient,
        KafkaDlqPublisher dlq,
        ILogger<PaymentEventConsumer> logger)
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

        _logger.LogInformation("PaymentEventConsumer starting — bootstrap={Bootstrap}", bootstrap);

        using var consumer = new ConsumerBuilder<string, string>(config).Build();
        consumer.Subscribe(new[] { TopicCompleted });

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
            _logger.LogInformation("PaymentEventConsumer stopped");
        }
        finally
        {
            consumer.Close();
        }
    }

    private async Task HandleMessageAsync(string topic, string json, CancellationToken ct)
    {
        var payload = JsonSerializer.Deserialize<PaymentPayload>(json, JsonOpts)
            ?? throw new InvalidOperationException($"Null payload on topic {topic}");

        var doc = new PaymentDocument
        {
            PaymentId     = payload.PaymentId     ?? string.Empty,
            AppointmentId = payload.AppointmentId ?? string.Empty,
            PatientId     = payload.PatientId     ?? string.Empty,
            Amount        = payload.Amount,
            Currency      = payload.Currency      ?? "VND",
            Method        = payload.Method        ?? string.Empty,
            Status        = payload.Status        ?? string.Empty,
            CreatedAt     = payload.CreatedAt
        };

        if (string.IsNullOrWhiteSpace(doc.PaymentId))
        {
            _logger.LogWarning("PaymentId missing on topic {Topic} — skipping (not DLQ-worthy)", topic);
            return;
        }

        var response = await _esClient.IndexAsync(doc, idx => idx
            .Index(PaymentIndex)
            .Id(doc.PaymentId), ct);

        if (!response.IsValidResponse)
            throw new InvalidOperationException(
                $"Elasticsearch index failed for payment {doc.PaymentId}: {response.DebugInformation}");

        _logger.LogInformation("Indexed payment {PaymentId} from topic {Topic}", doc.PaymentId, topic);
    }

    private sealed class PaymentPayload
    {
        public string?  PaymentId     { get; set; }
        public string?  AppointmentId { get; set; }
        public string?  PatientId     { get; set; }
        public decimal  Amount        { get; set; }
        public string?  Currency      { get; set; }
        public string?  Method        { get; set; }
        public string?  Status        { get; set; }
        public DateTime CreatedAt     { get; set; }
    }
}
