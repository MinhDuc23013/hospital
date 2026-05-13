using HospitalSystem.AiRcaService.Infrastructure.Loki;

namespace HospitalSystem.AiRcaService.Infrastructure.Redaction;

/// <summary>
/// Redacts PHI from LokiLogEntry batch using:
/// 1. Key blocklist — labels whose names are in RedactionRules.BlockedKeys
/// 2. Regex patterns — value patterns applied to Message text
/// </summary>
public sealed class PiiRedactor : IPiiRedactor
{
    private readonly ILogger<PiiRedactor> _logger;

    public PiiRedactor(ILogger<PiiRedactor> logger) => _logger = logger;

    public RedactionResult Redact(IReadOnlyList<LokiLogEntry> entries)
    {
        var redactedEntries = new List<LokiLogEntry>(entries.Count);
        var countsByType = new Dictionary<string, int>(StringComparer.Ordinal);
        int totalRedactions = 0;

        foreach (var entry in entries)
        {
            var (redactedEntry, entryCount, entryTypes) = RedactEntry(entry);
            redactedEntries.Add(redactedEntry);
            totalRedactions += entryCount;

            foreach (var (type, count) in entryTypes)
                countsByType[type] = countsByType.GetValueOrDefault(type) + count;
        }

        // Audit: log count only, never the redacted values
        if (totalRedactions > 0)
            _logger.LogInformation(
                "PHI redaction complete. TotalRedactions={TotalRedactions} ByType={ByType}",
                totalRedactions,
                string.Join(", ", countsByType.Select(kv => $"{kv.Key}:{kv.Value}")));

        return new RedactionResult(redactedEntries, totalRedactions, countsByType);
    }

    private static (LokiLogEntry Entry, int Count, Dictionary<string, int> Types)
        RedactEntry(LokiLogEntry entry)
    {
        var countsByType = new Dictionary<string, int>(StringComparer.Ordinal);
        int count = 0;

        // --- Redact Labels by blocked key names ---
        var redactedLabels = new Dictionary<string, string>(entry.Labels.Count, StringComparer.Ordinal);
        foreach (var (key, value) in entry.Labels)
        {
            if (RedactionRules.BlockedKeys.Contains(key))
            {
                var token = $"[REDACTED:{key.ToUpperInvariant()}_KEY]";
                redactedLabels[key] = token;
                countsByType[$"{key.ToUpperInvariant()}_KEY"] =
                    countsByType.GetValueOrDefault($"{key.ToUpperInvariant()}_KEY") + 1;
                count++;
            }
            else
            {
                redactedLabels[key] = value;
            }
        }

        // --- Redact Message text by regex patterns ---
        var message = entry.Message ?? string.Empty;
        foreach (var (patternName, regex) in RedactionRules.ValuePatterns)
        {
            var replacement = $"[REDACTED:{patternName}]";
            var newMessage = regex.Replace(message, m =>
            {
                countsByType[patternName] = countsByType.GetValueOrDefault(patternName) + 1;
                count++;
                return replacement;
            });
            message = newMessage;
        }

        var redactedEntry = entry with { Message = message, Labels = redactedLabels };
        return (redactedEntry, count, countsByType);
    }
}
