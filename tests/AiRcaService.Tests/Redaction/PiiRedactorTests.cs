using FluentAssertions;
using HospitalSystem.AiRcaService.Infrastructure.Loki;
using HospitalSystem.AiRcaService.Infrastructure.Redaction;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace AiRcaService.Tests.Redaction;

/// <summary>
/// CI-gated: ALL 20+ cases must pass. Fail = block merge.
/// Tests PHI redaction by key blocklist and regex patterns.
/// </summary>
public sealed class PiiRedactorTests
{
    private readonly PiiRedactor _sut = new(NullLogger<PiiRedactor>.Instance);

    // ── Helper ────────────────────────────────────────────────────────────────
    private static LokiLogEntry MakeEntry(
        string message,
        Dictionary<string, string>? labels = null)
        => new(DateTime.UtcNow, "info", message,
               labels ?? new Dictionary<string, string>(), null);

    private string RedactMessage(string message)
    {
        var result = _sut.Redact(new[] { MakeEntry(message) });
        return result.RedactedEntries[0].Message;
    }

    // ── Pattern tests (1–7) ───────────────────────────────────────────────────

    // Case 1: EMAIL redacted from message text
    [Fact]
    public void Redact_EmailInMessage_ShouldRedact()
    {
        var result = RedactMessage("User test@hospital.vn logged in");
        result.Should().Contain("[REDACTED:EMAIL]");
        result.Should().NotContain("test@hospital.vn");
    }

    // Case 2: PHONE_VN with prefix 0
    [Fact]
    public void Redact_PhoneVnPrefix0_ShouldRedact()
    {
        var result = RedactMessage("Call 0912345678 for support");
        result.Should().Contain("[REDACTED:PHONE_VN]");
        result.Should().NotContain("0912345678");
    }

    // Case 3: PHONE_VN with prefix +84
    [Fact]
    public void Redact_PhoneVnPrefix84_ShouldRedact()
    {
        var result = RedactMessage("Contact +84912345678 immediately");
        result.Should().Contain("[REDACTED:PHONE_VN]");
        result.Should().NotContain("+84912345678");
    }

    // Case 4: CCCD 12 digits
    [Fact]
    public void Redact_Cccd12Digits_ShouldRedact()
    {
        var result = RedactMessage("CCCD: 012345678901");
        result.Should().Contain("[REDACTED:CCCD_12]");
        result.Should().NotContain("012345678901");
    }

    // Case 5: CCCD 9 digits
    [Fact]
    public void Redact_Cccd9Digits_ShouldRedact()
    {
        var result = RedactMessage("Old ID: 012345678");
        result.Should().Contain("[REDACTED:CCCD_9]");
        result.Should().NotContain("012345678");
    }

    // Case 6: BHYT format AB + 13 digits
    [Fact]
    public void Redact_BhytCode_ShouldRedact()
    {
        var result = RedactMessage("BHYT: AB1234567890123");
        result.Should().Contain("[REDACTED:BHYT]");
        result.Should().NotContain("AB1234567890123");
    }

    // Case 7: DATE_ISO NOT redacted from free text (removed to preserve LLM temporal context)
    // DateOfBirth covered via BlockedKeys label; free-text dates are operational timestamps.
    [Fact]
    public void Redact_DateIsoInFreeText_ShouldNotRedact()
    {
        var result = RedactMessage("Request timestamp 2024-01-15 processed");
        result.Should().NotContain("[REDACTED:DATE_ISO]");
        result.Should().Contain("2024-01-15"); // preserved — operational timestamp
    }

    // Case 7b: BHYT lowercase — must also be redacted after IgnoreCase fix
    [Fact]
    public void Redact_BhytLowercase_ShouldRedact()
    {
        var result = RedactMessage("Insurance code ab1234567890123 verified");
        result.Should().Contain("[REDACTED:BHYT]");
        result.Should().NotContain("ab1234567890123");
    }

    // ── Blocked key label tests (8–10) ────────────────────────────────────────

    // Case 8: Label key "Email" → value redacted
    [Fact]
    public void Redact_BlockedKeyEmail_ShouldRedactLabelValue()
    {
        var entry = MakeEntry("some log", new Dictionary<string, string>
        {
            ["Email"] = "test@test.com"
        });
        var result = _sut.Redact(new[] { entry });
        result.RedactedEntries[0].Labels["Email"].Should().Contain("[REDACTED:");
        result.RedactedEntries[0].Labels["Email"].Should().NotContain("test@test.com");
    }

    // Case 9: Label key "Phone"
    [Fact]
    public void Redact_BlockedKeyPhone_ShouldRedactLabelValue()
    {
        var entry = MakeEntry("log", new Dictionary<string, string>
        {
            ["Phone"] = "0912345678"
        });
        var result = _sut.Redact(new[] { entry });
        result.RedactedEntries[0].Labels["Phone"].Should().Contain("[REDACTED:");
        result.RedactedEntries[0].Labels["Phone"].Should().NotContain("0912345678");
    }

    // Case 10: Label key "To"
    [Fact]
    public void Redact_BlockedKeyTo_ShouldRedactLabelValue()
    {
        var entry = MakeEntry("notification sent", new Dictionary<string, string>
        {
            ["To"] = "patient@email.com"
        });
        var result = _sut.Redact(new[] { entry });
        result.RedactedEntries[0].Labels["To"].Should().Contain("[REDACTED:");
        result.RedactedEntries[0].Labels["To"].Should().NotContain("patient@email.com");
    }

    // ── Combined / edge cases (11–20+) ────────────────────────────────────────

    // Case 11: Three PHI types in one message
    [Fact]
    public void Redact_ThreePiiTypesInOneMessage_ShouldRedactAll()
    {
        var msg    = "Email test@x.vn phone 0912345678 id AB1234567890123";
        var result = RedactMessage(msg);
        result.Should().Contain("[REDACTED:EMAIL]");
        result.Should().Contain("[REDACTED:PHONE_VN]");
        result.Should().Contain("[REDACTED:BHYT]");
        result.Should().NotContain("test@x.vn");
        result.Should().NotContain("0912345678");
        result.Should().NotContain("AB1234567890123");
    }

    // Case 12: Empty message → no crash
    [Fact]
    public void Redact_EmptyMessage_ShouldNotCrash()
    {
        var result = _sut.Redact(new[] { MakeEntry(string.Empty) });
        result.RedactedEntries[0].Message.Should().BeEmpty();
        result.TotalRedactions.Should().Be(0);
    }

    // Case 13: Very long message (10k chars)
    [Fact]
    public void Redact_VeryLongMessage_ShouldRedactCorrectly()
    {
        var filler = new string('x', 5000);
        var msg    = $"{filler} test@hospital.vn {filler}";
        var result = RedactMessage(msg);
        result.Should().Contain("[REDACTED:EMAIL]");
        result.Should().NotContain("test@hospital.vn");
    }

    // Case 14: No PHI → no redaction, count = 0
    [Fact]
    public void Redact_NoPhi_ShouldReturnZeroRedactions()
    {
        var result = _sut.Redact(new[] { MakeEntry("Normal log entry: request processed OK") });
        result.TotalRedactions.Should().Be(0);
        result.RedactionsByType.Should().BeEmpty();
    }

    // Case 15: Multiple emails in same message
    [Fact]
    public void Redact_MultipleEmailsInMessage_ShouldRedactAll()
    {
        var msg    = "Sent to admin@hospital.vn and doctor@clinic.org";
        var result = RedactMessage(msg);
        result.Should().NotContain("admin@hospital.vn");
        result.Should().NotContain("doctor@clinic.org");
        result.Should().Contain("[REDACTED:EMAIL]");
    }

    // Case 16: Phone + email combo
    [Fact]
    public void Redact_PhoneAndEmailCombo_ShouldRedactBoth()
    {
        var msg    = "Call +84901234567 or email user@example.com";
        var result = RedactMessage(msg);
        result.Should().Contain("[REDACTED:PHONE_VN]");
        result.Should().Contain("[REDACTED:EMAIL]");
    }

    // Case 17: Labels dict empty → handle gracefully
    [Fact]
    public void Redact_EmptyLabels_ShouldHandleGracefully()
    {
        var entry  = MakeEntry("no labels here", new Dictionary<string, string>());
        var result = _sut.Redact(new[] { entry });
        result.RedactedEntries[0].Labels.Should().BeEmpty();
        result.Should().NotBeNull();
    }

    // Case 18: Full LokiLogEntry round-trip — non-PHI fields preserved
    [Fact]
    public void Redact_RoundTrip_NonPhiFieldsPreserved()
    {
        var ts    = new DateTime(2024, 1, 15, 10, 30, 0, DateTimeKind.Utc);
        var entry = new LokiLogEntry(ts, "error", "DB connection failed",
            new Dictionary<string, string> { ["service"] = "patient-service" }, "trace-abc-123");

        var result = _sut.Redact(new[] { entry });
        var redacted = result.RedactedEntries[0];

        redacted.Timestamp.Should().Be(ts);
        redacted.Level.Should().Be("error");
        redacted.CorrelationId.Should().Be("trace-abc-123");
        redacted.Labels["service"].Should().Be("patient-service");
        redacted.Message.Should().Be("DB connection failed"); // no PHI → unchanged
    }

    // Case 19: Redact count matches actual redactions
    [Fact]
    public void Redact_CountMatchesActualRedactions()
    {
        // 1 email pattern in message + 1 phone pattern in message + 1 Email label key = 3 total
        var entry = new LokiLogEntry(DateTime.UtcNow, "info",
            "Contact user@test.vn or 0987654321",
            new Dictionary<string, string> { ["Email"] = "other@test.vn" },
            null);

        var result = _sut.Redact(new[] { entry });
        result.TotalRedactions.Should().Be(3);
    }

    // Case 24: Blocked key case-insensitive (email vs Email)
    [Fact]
    public void Redact_BlockedKey_IsCaseInsensitive()
    {
        var entry = MakeEntry("log", new Dictionary<string, string>
        {
            ["email"] = "lower@case.vn",  // lowercase key must also be blocked
            ["EMAIL"] = "upper@case.vn"   // uppercase key must also be blocked
        });
        var result = _sut.Redact(new[] { entry });
        result.RedactedEntries[0].Labels["email"].Should().Contain("[REDACTED:");
        result.RedactedEntries[0].Labels["EMAIL"].Should().Contain("[REDACTED:");
    }

    // Case 20: CCCD 9-digit in order context — over-redact accepted
    [Fact]
    public void Redact_Cccd9InOrderContext_OverRedactAccepted()
    {
        // OrderId=123456789 will be redacted — documented trade-off: prefer over-redact
        var result = RedactMessage("OrderId=123456789 processed");
        result.Should().Contain("[REDACTED:CCCD_9]");
        // This is expected/accepted behavior — not a bug
    }

    // Case 21: RedactionsByType accurately tracks pattern types
    [Fact]
    public void Redact_ByTypeDictionary_TracksCorrectly()
    {
        var msg    = "email: a@b.vn phone: 0912345678";
        var entry  = MakeEntry(msg);
        var result = _sut.Redact(new[] { entry });

        result.RedactionsByType.Should().ContainKey("EMAIL");
        result.RedactionsByType.Should().ContainKey("PHONE_VN");
        result.RedactionsByType["EMAIL"].Should().Be(1);
        result.RedactionsByType["PHONE_VN"].Should().Be(1);
    }

    // Case 22: Multiple blocked keys in labels
    [Fact]
    public void Redact_MultipleBlockedKeysInLabels_ShouldRedactAll()
    {
        var entry = MakeEntry("log", new Dictionary<string, string>
        {
            ["Email"]       = "x@y.com",
            ["Phone"]       = "0901234567",
            ["FirstName"]   = "John",
            ["service"]     = "patient-service" // NOT blocked
        });
        var result = _sut.Redact(new[] { entry });
        var labels = result.RedactedEntries[0].Labels;

        labels["Email"].Should().Contain("[REDACTED:");
        labels["Phone"].Should().Contain("[REDACTED:");
        labels["FirstName"].Should().Contain("[REDACTED:");
        labels["service"].Should().Be("patient-service"); // unchanged
    }

    // Case 23: Batch of entries — each independently redacted
    [Fact]
    public void Redact_BatchOfEntries_EachRedactedIndependently()
    {
        var entries = new[]
        {
            MakeEntry("user@a.vn called"),
            MakeEntry("normal log line"),
            MakeEntry("patient BHYT AB1234567890123 checked")
        };
        var result = _sut.Redact(entries);

        result.RedactedEntries.Should().HaveCount(3);
        result.RedactedEntries[0].Message.Should().Contain("[REDACTED:EMAIL]");
        result.RedactedEntries[1].Message.Should().Be("normal log line");
        result.RedactedEntries[2].Message.Should().Contain("[REDACTED:BHYT]");
    }
}
