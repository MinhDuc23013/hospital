using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PaymentService.Infrastructure.HttpClients;

public record ImagingOrderResponse(
    Guid Id,
    Guid PatientId,
    Guid AppointmentId,
    string DoctorId,
    string Type,
    string BodyPart,
    decimal Price,
    string Status);

/// <summary>HTTP client for fetching imaging orders by appointment from ImagingService.</summary>
public class ImagingServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ImagingServiceClient> _logger;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public ImagingServiceClient(HttpClient httpClient, ILogger<ImagingServiceClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<List<ImagingOrderResponse>> GetByAppointmentAsync(Guid appointmentId, CancellationToken ct = default)
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<List<ImagingOrderResponse>>(
                $"api/imaging-orders?appointmentId={appointmentId}", JsonOpts, ct);
            return response ?? [];
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch imaging orders for appointment {AppointmentId}", appointmentId);
            return [];
        }
    }
}
