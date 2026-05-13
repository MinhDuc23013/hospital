namespace HospitalSystem.AiRcaService.Infrastructure.Jaeger;

/// <summary>DTOs for deserializing Jaeger /api/traces/{id} response.</summary>
public sealed record JaegerTraceResponse(
    IReadOnlyList<JaegerTraceData> Data
);

public sealed record JaegerTraceData(
    string TraceID,
    IReadOnlyList<JaegerSpan> Spans,
    Dictionary<string, JaegerProcess> Processes
);

public sealed record JaegerSpan(
    string TraceID,
    string SpanID,
    string OperationName,
    string ProcessID,
    long StartTime,     // microseconds since epoch
    long Duration,      // microseconds
    IReadOnlyList<JaegerTag> Tags,
    IReadOnlyList<JaegerLog> Logs
);

public sealed record JaegerTag(string Key, string Type, object? Value);

public sealed record JaegerLog(long Timestamp, IReadOnlyList<JaegerTag> Fields);

public sealed record JaegerProcess(string ServiceName, IReadOnlyList<JaegerTag> Tags);
