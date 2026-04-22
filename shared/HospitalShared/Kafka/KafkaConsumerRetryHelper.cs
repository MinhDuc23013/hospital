using Confluent.Kafka;
using Microsoft.Extensions.Logging;

namespace HospitalShared.Kafka;

/// <summary>
/// Wraps a handler in an in-process retry loop with exponential backoff. If the handler still
/// fails after <paramref name="maxAttempts"/>, the message is shipped to the DLQ via
/// <see cref="KafkaDlqPublisher"/> and the caller commits the offset to unblock the partition.
/// </summary>
/// <remarks>
/// Retry state is in-process only — on consumer restart the count resets. Worst case: 2× maxAttempts
/// before DLQ. Acceptable trade-off vs persistent counter which would require re-producing the
/// message each attempt.
/// </remarks>
public static class KafkaConsumerRetryHelper
{
    /// <summary>
    /// Execute <paramref name="handler"/> with retry + DLQ. Returns <c>true</c> if the message was
    /// handled successfully OR shipped to DLQ — in both cases the caller MUST commit the offset.
    /// Returns <c>false</c> only when DLQ publish itself failed; do NOT commit so the message is
    /// redelivered on next poll.
    /// </summary>
    public static async Task<bool> HandleWithDlqAsync(
        ConsumeResult<string, string> result,
        Func<CancellationToken, Task> handler,
        KafkaDlqPublisher dlq,
        string consumerGroup,
        ILogger logger,
        CancellationToken ct,
        int maxAttempts = 3,
        TimeSpan? initialBackoff = null)
    {
        var backoff = initialBackoff ?? TimeSpan.FromSeconds(1);
        Exception? lastError = null;

        for (int attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                await handler(ct);
                if (attempt > 1)
                    logger.LogInformation(
                        "Handler succeeded on retry #{Attempt} for topic={Topic} offset={Offset}",
                        attempt, result.Topic, result.Offset.Value);
                return true;
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex)
            {
                lastError = ex;
                logger.LogWarning(ex,
                    "Handler attempt {Attempt}/{Max} failed: topic={Topic} offset={Offset} reason={Reason}",
                    attempt, maxAttempts, result.Topic, result.Offset.Value, ex.Message);

                if (attempt < maxAttempts)
                {
                    await Task.Delay(backoff, ct);
                    backoff = TimeSpan.FromMilliseconds(backoff.TotalMilliseconds * 2);
                }
            }
        }

        // All attempts failed — ship to DLQ.
        try
        {
            await dlq.PublishAsync(result, lastError!, maxAttempts, consumerGroup, ct);
            return true; // caller commits — message survives in DLQ
        }
        catch
        {
            // DLQ publish failed — caller should NOT commit; retry on next poll.
            return false;
        }
    }
}
