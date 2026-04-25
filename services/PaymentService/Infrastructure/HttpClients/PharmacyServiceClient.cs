using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PaymentService.Infrastructure.HttpClients;

public record PrescriptionItemResponse(Guid DrugId, string DrugName, int Quantity, string? Dosage);

public record PrescriptionResponse(
    Guid Id,
    Guid PatientId,
    Guid? AppointmentId,
    string Status,
    List<PrescriptionItemResponse> Items);

public record DrugPriceResponse(Guid Id, string Name, decimal Price);

/// <summary>HTTP client for fetching prescriptions and drug prices from PharmacyService.</summary>
public class PharmacyServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<PharmacyServiceClient> _logger;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public PharmacyServiceClient(HttpClient httpClient, ILogger<PharmacyServiceClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<List<PrescriptionResponse>> GetByAppointmentAsync(Guid appointmentId, CancellationToken ct = default)
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<List<PrescriptionResponse>>(
                $"api/prescriptions?appointmentId={appointmentId}", JsonOpts, ct);
            return response ?? [];
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch prescriptions for appointment {AppointmentId}", appointmentId);
            return [];
        }
    }

    public async Task<DrugPriceResponse?> GetDrugAsync(Guid drugId, CancellationToken ct = default)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<DrugPriceResponse>($"api/drugs/{drugId}", JsonOpts, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch drug {DrugId}", drugId);
            return null;
        }
    }
}
