using System.Text;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;

namespace HospitalShared.Kafka;

/// <summary>
/// Publishes poison messages to a dead-letter topic (<c>{topic}.dlq</c>) with rich failure metadata.
/// Consumers call this after exceeding max retries on a message to unblock the partition while
/// preserving the message for manual inspection / replay.
/// </summary>
public class KafkaDlqPublisher
{
    private readonly IProducer<string, string> _producer;
    private readonly ILogger<KafkaDlqPublisher> _logger;

    public KafkaDlqPublisher(IProducer<string, string> producer, ILogger<KafkaDlqPublisher> logger)
    {
        _producer = producer;
        _logger = logger;
    }

    /// <summary>
    /// Publish a failed ConsumeResult to <c>{originalTopic}.dlq</c>.
    /// Preserves original payload + key, attaches failure context in headers.
    /// </summary>
    /// <param name="result">The failed ConsumeResult from the consumer.</param>
    /// <param name="error">The last exception that caused the failure.</param>
    /// <param name="attempts">How many times the handler tried before giving up.</param>
    /// <param name="consumerGroup">The consumer group that failed to process the message.</param>
    public async Task PublishAsync(
        ConsumeResult<string, string> result,
        Exception error,
        int attempts,
        string consumerGroup,
        CancellationToken ct = default)
    {
        var dlqTopic = $"{result.Topic}.dlq";
        var headers = new Headers
        {
            { "x-original-topic",    Encoding.UTF8.GetBytes(result.Topic) },
            { "x-original-partition",Encoding.UTF8.GetBytes(result.Partition.Value.ToString()) },
            { "x-original-offset",   Encoding.UTF8.GetBytes(result.Offset.Value.ToString()) },
            { "x-consumer-group",    Encoding.UTF8.GetBytes(consumerGroup) },
            { "x-failure-reason",    Encoding.UTF8.GetBytes(Truncate(error.Message, 1000)) },
            { "x-failure-type",      Encoding.UTF8.GetBytes(error.GetType().FullName ?? "Unknown") },
            { "x-attempts",          Encoding.UTF8.GetBytes(attempts.ToString()) },
            { "x-failed-at",         Encoding.UTF8.GetBytes(DateTime.UtcNow.ToString("o")) }
        };

        // Copy original headers so we don't lose upstream tracing (e.g., outbox-id).
        if (result.Message.Headers is not null)
        {
            foreach (var h in result.Message.Headers)
                headers.Add($"x-original-{h.Key}", h.GetValueBytes());
        }

        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(10));

            await _producer.ProduceAsync(dlqTopic, new Message<string, string>
            {
                Key = result.Message.Key,
                Value = result.Message.Value,
                Headers = headers
            }, cts.Token);

            _logger.LogError(
                "Message SENT TO DLQ: topic={DlqTopic}, originalOffset={Offset}, group={Group}, attempts={Attempts}, reason={Reason}",
                dlqTopic, result.Offset.Value, consumerGroup, attempts, error.Message);
        }
        catch (Exception ex)
        {
            // DLQ publish itself failed — worst case. Log critical and let caller decide whether to commit.
            _logger.LogCritical(ex,
                "FAILED to publish to DLQ {DlqTopic} for offset {Offset}. Message will be retried on next poll.",
                dlqTopic, result.Offset.Value);
            throw;
        }
    }

    private static string Truncate(string s, int max) => s.Length <= max ? s : s[..max];
}
