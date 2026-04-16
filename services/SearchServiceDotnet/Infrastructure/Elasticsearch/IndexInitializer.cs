using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.Mapping;
using SearchServiceDotnet.Application.Models;

namespace SearchServiceDotnet.Infrastructure.Elasticsearch;

/// <summary>
/// IHostedService that creates Elasticsearch indexes on startup if they do not already exist.
/// Non-fatal: logs errors but does not prevent the service from starting.
/// </summary>
public class IndexInitializer : IHostedService
{
    private readonly ElasticsearchClient _client;
    private readonly ILogger<IndexInitializer> _logger;

    private const string PatientIndex     = "hospital-patients";
    private const string DrugIndex        = "hospital-drugs";
    private const string AppointmentIndex = "hospital-appointments";
    private const string PaymentIndex     = "hospital-payments";
    private const string SlotIndex        = "hospital-slots";
    private const string DoctorIndex      = "hospital-doctors";

    public IndexInitializer(ElasticsearchClient client, ILogger<IndexInitializer> logger)
    {
        _client = client;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await EnsurePatientIndexAsync(cancellationToken);
        await EnsureDrugIndexAsync(cancellationToken);
        await EnsureAppointmentIndexAsync(cancellationToken);
        await EnsurePaymentIndexAsync(cancellationToken);
        await EnsureSlotIndexAsync(cancellationToken);
        await EnsureDoctorIndexAsync(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    // ── Patients ────────────────────────────────────────────────────────────

    private async Task EnsurePatientIndexAsync(CancellationToken ct)
    {
        try
        {
            var exists = await _client.Indices.ExistsAsync(PatientIndex, ct);
            if (exists.Exists)
            {
                _logger.LogInformation("Index '{Index}' already exists — skipping creation", PatientIndex);
                return;
            }

            var response = await _client.Indices.CreateAsync(PatientIndex, c => c
                .Mappings(m => m
                    .Properties<PatientDocument>(p => p
                        .Keyword(f => f.PatientId)
                        .Text(f => f.FirstName)
                        .Text(f => f.LastName)
                        .Keyword(f => f.Email)
                        .Date(f => f.CreatedAt)
                    )
                ), ct);

            if (response.IsValidResponse)
                _logger.LogInformation("Created index '{Index}'", PatientIndex);
            else
                _logger.LogWarning("Failed to create index '{Index}': {Debug}", PatientIndex, response.DebugInformation);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Non-fatal error initializing index '{Index}'", PatientIndex);
        }
    }

    // ── Drugs ────────────────────────────────────────────────────────────────

    private async Task EnsureDrugIndexAsync(CancellationToken ct)
    {
        try
        {
            var exists = await _client.Indices.ExistsAsync(DrugIndex, ct);
            if (exists.Exists)
            {
                _logger.LogInformation("Index '{Index}' already exists — skipping creation", DrugIndex);
                return;
            }

            var response = await _client.Indices.CreateAsync(DrugIndex, c => c
                .Mappings(m => m
                    .Properties<DrugDocument>(p => p
                        .IntegerNumber(f => f.Id)
                        .Text(f => f.Name)
                        .Keyword(f => f.Code)
                        .Text(f => f.Dosage)
                    )
                ), ct);

            if (response.IsValidResponse)
                _logger.LogInformation("Created index '{Index}'", DrugIndex);
            else
                _logger.LogWarning("Failed to create index '{Index}': {Debug}", DrugIndex, response.DebugInformation);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Non-fatal error initializing index '{Index}'", DrugIndex);
        }
    }

    // ── Appointments ──────────────────────────────────────────────────────────

    private async Task EnsureAppointmentIndexAsync(CancellationToken ct)
    {
        try
        {
            var exists = await _client.Indices.ExistsAsync(AppointmentIndex, ct);
            if (exists.Exists)
            {
                _logger.LogInformation("Index '{Index}' already exists — skipping creation", AppointmentIndex);
                return;
            }

            var response = await _client.Indices.CreateAsync(AppointmentIndex, c => c
                .Mappings(m => m
                    .Properties<AppointmentDocument>(p => p
                        .Keyword(f => f.AppointmentId)
                        .Keyword(f => f.PatientId)
                        .Keyword(f => f.DoctorId)
                        .Date(f => f.ScheduledTime)
                        .IntegerNumber(f => f.DurationMinutes)
                        .Keyword(f => f.Status)
                        .Date(f => f.CreatedAt)
                    )
                ), ct);

            if (response.IsValidResponse)
                _logger.LogInformation("Created index '{Index}'", AppointmentIndex);
            else
                _logger.LogWarning("Failed to create index '{Index}': {Debug}", AppointmentIndex, response.DebugInformation);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Non-fatal error initializing index '{Index}'", AppointmentIndex);
        }
    }

    // ── Payments ──────────────────────────────────────────────────────────────

    private async Task EnsurePaymentIndexAsync(CancellationToken ct)
    {
        try
        {
            var exists = await _client.Indices.ExistsAsync(PaymentIndex, ct);
            if (exists.Exists)
            {
                _logger.LogInformation("Index '{Index}' already exists — skipping creation", PaymentIndex);
                return;
            }

            var response = await _client.Indices.CreateAsync(PaymentIndex, c => c
                .Mappings(m => m
                    .Properties<PaymentDocument>(p => p
                        .Keyword(f => f.PaymentId)
                        .Keyword(f => f.AppointmentId)
                        .Keyword(f => f.PatientId)
                        .FloatNumber(f => f.Amount)
                        .Keyword(f => f.Currency)
                        .Keyword(f => f.Method)
                        .Keyword(f => f.Status)
                        .Date(f => f.CreatedAt)
                    )
                ), ct);

            if (response.IsValidResponse)
                _logger.LogInformation("Created index '{Index}'", PaymentIndex);
            else
                _logger.LogWarning("Failed to create index '{Index}': {Debug}", PaymentIndex, response.DebugInformation);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Non-fatal error initializing index '{Index}'", PaymentIndex);
        }
    }

    // ── Doctors ───────────────────────────────────────────────────────────────

    private async Task EnsureDoctorIndexAsync(CancellationToken ct)
    {
        try
        {
            var exists = await _client.Indices.ExistsAsync(DoctorIndex, ct);
            if (exists.Exists)
            {
                _logger.LogInformation("Index '{Index}' already exists — skipping creation", DoctorIndex);
                return;
            }

            var response = await _client.Indices.CreateAsync(DoctorIndex, c => c
                .Mappings(m => m
                    .Properties<DoctorDocument>(p => p
                        .Keyword(f => f.DoctorId)
                        .Text(f => f.FullName, t => t.Analyzer("standard"))
                        .Keyword(f => f.Specialty)
                        .Keyword(f => f.Phone)
                        .Keyword(f => f.Email)
                        .Boolean(f => f.IsActive)
                        .Date(f => f.CreatedAt)
                    )
                ), ct);

            if (response.IsValidResponse)
                _logger.LogInformation("Created index '{Index}'", DoctorIndex);
            else
                _logger.LogWarning("Failed to create index '{Index}': {Debug}", DoctorIndex, response.DebugInformation);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Non-fatal error initializing index '{Index}'", DoctorIndex);
        }
    }

    // ── Slots ─────────────────────────────────────────────────────────────────

    private async Task EnsureSlotIndexAsync(CancellationToken ct)
    {
        try
        {
            var exists = await _client.Indices.ExistsAsync(SlotIndex, ct);
            if (exists.Exists)
            {
                _logger.LogInformation("Index '{Index}' already exists — skipping creation", SlotIndex);
                return;
            }

            var response = await _client.Indices.CreateAsync(SlotIndex, c => c
                .Mappings(m => m
                    .Properties<SlotDocument>(p => p
                        .Keyword(f => f.SlotId)
                        .Keyword(f => f.ScheduleId)
                        .Keyword(f => f.DoctorId)
                        .Keyword(f => f.PatientId)
                        .Date(f => f.ScheduledTime)
                        .IntegerNumber(f => f.DurationMinutes)
                        .Keyword(f => f.Status)
                        .Date("reservedUntil")
                    )
                ), ct);

            if (response.IsValidResponse)
                _logger.LogInformation("Created index '{Index}'", SlotIndex);
            else
                _logger.LogWarning("Failed to create index '{Index}': {Debug}", SlotIndex, response.DebugInformation);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Non-fatal error initializing index '{Index}'", SlotIndex);
        }
    }
}
