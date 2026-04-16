using System.Text.Json;
using Elastic.Clients.Elasticsearch;
using SearchServiceDotnet.Application.Models;

namespace SearchServiceDotnet.Application.Services;

// ── Internal DTOs for deserializing upstream service responses ────────────────

file class DoctorDto
{
    public string Id        { get; set; } = string.Empty;
    public string FullName  { get; set; } = string.Empty;
    public string Specialty { get; set; } = string.Empty;
    public string? Phone    { get; set; }
    public string? Email    { get; set; }
    public bool   IsActive  { get; set; }
    public DateTime CreatedAt { get; set; }
}

file class PatientDto
{
    public string Id        { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName  { get; set; } = string.Empty;
    public string Email     { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

file class AppointmentDto
{
    public string Id              { get; set; } = string.Empty;
    public string PatientId       { get; set; } = string.Empty;
    public string DoctorId        { get; set; } = string.Empty;
    public DateTime ScheduledTime { get; set; }
    public int DurationMinutes    { get; set; }
    public string Status          { get; set; } = string.Empty;
    public DateTime CreatedAt     { get; set; }
}

file class PaymentDto
{
    public string Id            { get; set; } = string.Empty;
    public string AppointmentId { get; set; } = string.Empty;
    public string PatientId     { get; set; } = string.Empty;
    public decimal Amount       { get; set; }
    public string Currency      { get; set; } = "VND";
    public string Method        { get; set; } = string.Empty;
    public string Status        { get; set; } = string.Empty;
    public DateTime CreatedAt   { get; set; }
}

file class PagedResult<T>
{
    public List<T> Data { get; set; } = new();
    // Some services nest under "items" — support both conventions
    public List<T> Items { get; set; } = new();
    // Returns whichever list has data
    public List<T> Resolved => Data.Count > 0 ? Data : Items;
}

/// <summary>
/// Fetches all data from upstream microservices via HTTP and bulk-indexes
/// the records into Elasticsearch. Designed for full reindex scenarios.
/// </summary>
public class ReindexService
{
    private readonly HttpClient            _httpClient;
    private readonly ElasticsearchClient   _esClient;
    private readonly ILogger<ReindexService> _logger;

    private readonly string _patientServiceUrl;
    private readonly string _appointmentServiceUrl;
    private readonly string _paymentServiceUrl;
    private readonly string _doctorServiceUrl;

    private const string PatientIndex     = "hospital-patients";
    private const string AppointmentIndex = "hospital-appointments";
    private const string PaymentIndex     = "hospital-payments";
    private const string DoctorIndex      = "hospital-doctors";

    private static readonly JsonSerializerOptions _jsonOpts = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public ReindexService(
        HttpClient httpClient,
        ElasticsearchClient esClient,
        IConfiguration config,
        ILogger<ReindexService> logger)
    {
        _httpClient           = httpClient;
        _esClient             = esClient;
        _logger               = logger;

        _patientServiceUrl     = config["Services:PatientService"]      ?? "http://patient-service:5001";
        _appointmentServiceUrl = config["Services:AppointmentService"]  ?? "http://appointment-service:5002";
        _paymentServiceUrl     = config["Services:PaymentService"]      ?? "http://payment-service:5008";
        _doctorServiceUrl      = config["Services:DoctorScheduleService"] ?? "http://doctor-schedule-service:5007";
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>Reindexes patients, appointments, and payments in parallel.</summary>
    public async Task<ReindexSummary> ReindexAllAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("Starting full reindex");

        var tasks = new[]
        {
            ReindexPatientsAsync(ct),
            ReindexAppointmentsAsync(ct),
            ReindexPaymentsAsync(ct),
            ReindexDoctorsAsync(ct)
        };

        var results = await Task.WhenAll(tasks);

        var summary = new ReindexSummary
        {
            Patients     = results[0],
            Appointments = results[1],
            Payments     = results[2],
            Doctors      = results[3]
        };

        _logger.LogInformation(
            "Full reindex done — patients={P} appointments={A} payments={Pay} doctors={D}",
            summary.Patients, summary.Appointments, summary.Payments, summary.Doctors);

        return summary;
    }

    /// <summary>Fetches all patients from PatientService and bulk-indexes to hospital-patients.</summary>
    public async Task<int> ReindexPatientsAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("Reindexing patients from {Url}", _patientServiceUrl);

        var docs  = new List<PatientDocument>();
        int page  = 1;
        const int pageSize = 1000;

        while (true)
        {
            var url  = $"{_patientServiceUrl}/api/patients?page={page}&pageSize={pageSize}";
            var batch = await FetchPageAsync<PatientDto>(url, ct);
            if (batch == null) break; // service unavailable

            if (batch.Count == 0) break;

            docs.AddRange(batch.Select(p => new PatientDocument
            {
                PatientId = p.Id,
                FirstName = p.FirstName,
                LastName  = p.LastName,
                Email     = p.Email,
                CreatedAt = p.CreatedAt
            }));

            _logger.LogDebug("Patients page {Page}: fetched {Count}", page, batch.Count);

            if (batch.Count < pageSize) break; // last page
            page++;
        }

        var indexed = await BulkIndexAsync(PatientIndex, docs, d => d.PatientId, ct);
        _logger.LogInformation("Patients reindexed: {Count}", indexed);
        return indexed;
    }

    /// <summary>Fetches all appointments from AppointmentService and bulk-indexes to hospital-appointments.</summary>
    public async Task<int> ReindexAppointmentsAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("Reindexing appointments from {Url}", _appointmentServiceUrl);

        var docs  = new List<AppointmentDocument>();
        int page  = 1;
        const int pageSize = 1000;

        while (true)
        {
            var url   = $"{_appointmentServiceUrl}/api/appointments?page={page}&pageSize={pageSize}";
            var batch = await FetchPageAsync<AppointmentDto>(url, ct);
            if (batch == null) break;

            if (batch.Count == 0) break;

            docs.AddRange(batch.Select(a => new AppointmentDocument
            {
                AppointmentId  = a.Id,
                PatientId      = a.PatientId,
                DoctorId       = a.DoctorId,
                ScheduledTime  = a.ScheduledTime,
                DurationMinutes = a.DurationMinutes,
                Status         = a.Status,
                CreatedAt      = a.CreatedAt
            }));

            _logger.LogDebug("Appointments page {Page}: fetched {Count}", page, batch.Count);

            if (batch.Count < pageSize) break;
            page++;
        }

        var indexed = await BulkIndexAsync(AppointmentIndex, docs, d => d.AppointmentId, ct);
        _logger.LogInformation("Appointments reindexed: {Count}", indexed);
        return indexed;
    }

    /// <summary>Fetches all payments from PaymentService and bulk-indexes to hospital-payments.</summary>
    public async Task<int> ReindexPaymentsAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("Reindexing payments from {Url}", _paymentServiceUrl);

        var docs  = new List<PaymentDocument>();
        int page  = 1;
        const int pageSize = 1000;

        while (true)
        {
            var url   = $"{_paymentServiceUrl}/api/payments?page={page}&pageSize={pageSize}";
            var batch = await FetchPageAsync<PaymentDto>(url, ct);
            if (batch == null) break;

            if (batch.Count == 0) break;

            docs.AddRange(batch.Select(p => new PaymentDocument
            {
                PaymentId     = p.Id,
                AppointmentId = p.AppointmentId,
                PatientId     = p.PatientId,
                Amount        = p.Amount,
                Currency      = p.Currency,
                Method        = p.Method,
                Status        = p.Status,
                CreatedAt     = p.CreatedAt
            }));

            _logger.LogDebug("Payments page {Page}: fetched {Count}", page, batch.Count);

            if (batch.Count < pageSize) break;
            page++;
        }

        var indexed = await BulkIndexAsync(PaymentIndex, docs, d => d.PaymentId, ct);
        _logger.LogInformation("Payments reindexed: {Count}", indexed);
        return indexed;
    }

    /// <summary>Fetches all doctors from DoctorScheduleService and bulk-indexes to hospital-doctors.</summary>
    public async Task<int> ReindexDoctorsAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("Reindexing doctors from {Url}", _doctorServiceUrl);

        var docs  = new List<DoctorDocument>();
        int page  = 1;
        const int pageSize = 1000;

        while (true)
        {
            var url   = $"{_doctorServiceUrl}/api/doctors?page={page}&pageSize={pageSize}";
            var batch = await FetchPageAsync<DoctorDto>(url, ct);
            if (batch == null) break;
            if (batch.Count == 0) break;

            docs.AddRange(batch.Select(d => new DoctorDocument
            {
                DoctorId  = d.Id,
                FullName  = d.FullName,
                Specialty = d.Specialty,
                Phone     = d.Phone,
                Email     = d.Email,
                IsActive  = d.IsActive,
                CreatedAt = d.CreatedAt
            }));

            _logger.LogDebug("Doctors page {Page}: fetched {Count}", page, batch.Count);
            if (batch.Count < pageSize) break;
            page++;
        }

        var indexed = await BulkIndexAsync(DoctorIndex, docs, d => d.DoctorId, ct);
        _logger.LogInformation("Doctors reindexed: {Count}", indexed);
        return indexed;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>
    /// GETs a paginated endpoint and returns the list of items.
    /// Returns null when the service is unavailable (logs a warning, no throw).
    /// </summary>
    private async Task<List<T>?> FetchPageAsync<T>(string url, CancellationToken ct)
    {
        try
        {
            // Token forwarding handled by TokenForwardingHandler (DelegatingHandler)
            var response = await _httpClient.GetAsync(url, ct);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(ct);

            // Try paged wrapper first: { data: [...] } or { items: [...] }
            var paged = JsonSerializer.Deserialize<PagedResult<T>>(json, _jsonOpts);
            if (paged != null && paged.Resolved.Count > 0)
                return paged.Resolved;

            // Fall back to plain array response
            var array = JsonSerializer.Deserialize<List<T>>(json, _jsonOpts);
            return array ?? new List<T>();
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or OperationCanceledException)
        {
            // service unreachable — log and return null to signal caller to skip
            _logger.LogWarning(ex, "Service unavailable at {Url} — skipping reindex for this type", url);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Unexpected error fetching {Url}", url);
            return null;
        }
    }

    /// <summary>
    /// Indexes documents using Elasticsearch Bulk API for high throughput.
    /// Sends 1000 docs per bulk request instead of 1 request per doc.
    /// </summary>
    private async Task<int> BulkIndexAsync<T>(
        string index,
        List<T> docs,
        Func<T, string> idSelector,
        CancellationToken ct) where T : class
    {
        if (docs.Count == 0) return 0;

        int indexed = 0;
        const int chunkSize = 1000;

        for (int i = 0; i < docs.Count; i += chunkSize)
        {
            var chunk = docs.Skip(i).Take(chunkSize).ToList();

            try
            {
                var operations = new List<Elastic.Clients.Elasticsearch.Core.Bulk.IBulkOperation>();
                foreach (var doc in chunk)
                {
                    operations.Add(new Elastic.Clients.Elasticsearch.Core.Bulk.BulkIndexOperation<T>(doc)
                    {
                        Id = idSelector(doc)
                    });
                }

                var response = await _esClient.BulkAsync(new Elastic.Clients.Elasticsearch.BulkRequest(index)
                {
                    Operations = operations
                }, ct);

                if (response.IsValidResponse)
                {
                    indexed += chunk.Count - (int)response.Errors.GetHashCode(); // fallback
                    indexed = i + chunk.Count; // simpler: count all sent so far
                    if (response.Errors)
                        _logger.LogWarning("Bulk index to {Index} had {ErrorCount} errors", index,
                            response.ItemsWithErrors.Count());
                    else
                        indexed = i + chunk.Count;
                }
                else
                {
                    _logger.LogWarning("Bulk index to {Index} failed: {Debug}", index, response.DebugInformation);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error bulk-indexing {Count} docs into {Index}", chunk.Count, index);
            }
        }

        return Math.Min(indexed, docs.Count);
    }
}

/// <summary>Summary counts returned by ReindexAllAsync.</summary>
public class ReindexSummary
{
    public int Patients     { get; set; }
    public int Appointments { get; set; }
    public int Payments     { get; set; }
    public int Doctors      { get; set; }
}
