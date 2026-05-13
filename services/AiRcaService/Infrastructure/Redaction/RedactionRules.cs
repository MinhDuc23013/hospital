using System.Text.RegularExpressions;

namespace HospitalSystem.AiRcaService.Infrastructure.Redaction;

/// <summary>
/// PHI blocklist keys and regex patterns — sourced from docs/logging-phi-audit.md.
/// Prefer over-redact to under-redact (e.g. 9-digit CCCD overlaps with OrderId — accepted trade-off).
/// </summary>
public static class RedactionRules
{
    /// <summary>Label/key names whose values must always be redacted regardless of content.</summary>
    public static readonly HashSet<string> BlockedKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "Email", "To", "Phone", "PhoneNumber", "FirstName", "LastName",
        "FullName", "DateOfBirth", "Subject", "Message", "Diagnosis", "Findings"
    };

    /// <summary>
    /// Compiled regex patterns for detecting PHI in free-text log messages.
    /// Key = redaction type label used in replacement token.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, Regex> ValuePatterns =
        new Dictionary<string, Regex>
        {
            // Order matters: longer/more-specific patterns first to avoid partial matches
            ["EMAIL"]    = new Regex(@"\b[\w.\-]+@[\w.\-]+\.\w{2,}\b",
                               RegexOptions.Compiled | RegexOptions.IgnoreCase),
            // \b doesn't work before + so use lookbehind for non-digit/non-plus context
            ["PHONE_VN"] = new Regex(@"(?<![+\d])(0|\+84)[1-9]\d{8,9}(?!\d)",
                               RegexOptions.Compiled),
            // IgnoreCase: lowercase BHYT codes must also be redacted
            ["BHYT"]     = new Regex(@"\b[A-Z]{2}\d{13}\b",
                               RegexOptions.Compiled | RegexOptions.IgnoreCase),
            ["CCCD_12"]  = new Regex(@"\b\d{12}\b",
                               RegexOptions.Compiled),
            // CCCD_9 last — highest false-positive risk (order IDs etc.)
            ["CCCD_9"]   = new Regex(@"\b\d{9}\b",
                               RegexOptions.Compiled),
            // DATE_ISO removed: ISO dates are also operational timestamps, redacting them
            // degrades LLM temporal context. DateOfBirth covered via BlockedKeys ("DateOfBirth").
            // Re-add with lookahead for DOB context if needed in future audit.
        };
}
