using System.Net.Http.Json;
using System.Text.Json;

namespace HospitalGateway.Services;

/// <summary>
/// Aggregates booking details from multiple microservices in a single gateway call.
/// Calls AppointmentService first (to get patientId/doctorId), then fans out
/// to PatientService, DoctorScheduleService, and PaymentService in parallel.
/// Partial failures are tolerated — missing service data returns null for that field.
/// </summary>
public class BookingAggregationService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _config;
    private readonly ILogger<BookingAggregationService> _logger;

    public BookingAggregationService(
        IHttpClientFactory httpClientFactory,
        IConfiguration config,
        ILogger<BookingAggregationService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _config = config;
        _logger = logger;
    }

    public async Task<BookingDetailsResponse?> GetBookingDetailsAsync(Guid appointmentId, CancellationToken ct)
    {
        // Step 1: fetch appointment first — we need patientId and doctorId for fan-out
        var appointmentBaseUrl = _config["Services:AppointmentService"] ?? "http://appointment-service:5002";
        var appointment = await GetAsync<AppointmentDto>($"{appointmentBaseUrl}/api/appointments/{appointmentId}", ct);

        if (appointment is null)
        {
            _logger.LogWarning("Appointment {AppointmentId} not found", appointmentId);
            return null;
        }

        // Step 2: fan out to 3 services in parallel
        var patientBaseUrl = _config["Services:PatientService"] ?? "http://patient-service:5001";
        var doctorBaseUrl = _config["Services:DoctorScheduleService"] ?? "http://doctor-schedule-service:5007";
        var paymentBaseUrl = _config["Services:PaymentService"] ?? "http://payment-service:5008";

        var patientTask = GetAsync<PatientDto>($"{patientBaseUrl}/api/patients/{appointment.PatientId}", ct);
        var doctorTask = GetAsync<DoctorDto>($"{doctorBaseUrl}/api/doctors/{appointment.DoctorId}", ct);
        var paymentTask = GetAsync<PaymentDto>($"{paymentBaseUrl}/api/payments/appointment/{appointmentId}", ct);

        await Task.WhenAll(patientTask, doctorTask, paymentTask);

        return new BookingDetailsResponse
        {
            Appointment = appointment,
            Patient = patientTask.Result,
            Doctor = doctorTask.Result,
            Payment = paymentTask.Result
        };
    }

    /// <summary>Fetches a resource and deserializes it; returns null on non-success or exception.</summary>
    private async Task<T?> GetAsync<T>(string url, CancellationToken ct) where T : class
    {
        try
        {
            var client = _httpClientFactory.CreateClient("aggregation");
            var response = await client.GetAsync(url, ct);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Non-success {StatusCode} fetching {Url}", (int)response.StatusCode, url);
                return null;
            }

            return await response.Content.ReadFromJsonAsync<T>(JsonOpts, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch {Url}", url);
            return null;
        }
    }

    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };
}

// ---------------------------------------------------------------------------
// DTOs — minimal fields required for display; nullable fields are optional data
// ---------------------------------------------------------------------------

public class BookingDetailsResponse
{
    public AppointmentDto? Appointment { get; set; }
    public PatientDto? Patient { get; set; }
    public DoctorDto? Doctor { get; set; }
    public PaymentDto? Payment { get; set; }
}

public class AppointmentDto
{
    public Guid Id { get; set; }
    public Guid PatientId { get; set; }
    public string DoctorId { get; set; } = "";
    public DateTime ScheduledTime { get; set; }
    public int DurationMinutes { get; set; }
    public string Status { get; set; } = "";
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class PatientDto
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string? Email { get; set; }
    public string? Phone { get; set; }
}

public class DoctorDto
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = "";
    public string Specialty { get; set; } = "";
    public string? Phone { get; set; }
}

public class PaymentDto
{
    public Guid Id { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "VND";
    public string Method { get; set; } = "";
    public string Status { get; set; } = "";
    public DateTime? PaidAt { get; set; }
}
