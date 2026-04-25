using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PaymentService.Infrastructure.HttpClients;

public record LabOrderItemResponse(
    Guid Id,
    string TestName,
    string TestCode,
    string Category,
    decimal UnitPrice);

public record LabOrderResponse(
    Guid Id,
    Guid PatientId,
    Guid AppointmentId,
    string DoctorId,
    string Status,
    List<LabOrderItemResponse> Items);

/// <summary>HTTP client for fetching lab orders by appointment from LabTestService.</summary>
public class LabTestServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<LabTestServiceClient> _logger;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public LabTestServiceClient(HttpClient httpClient, ILogger<LabTestServiceClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<List<LabOrderResponse>> GetByAppointmentAsync(Guid appointmentId, CancellationToken ct = default)
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<List<LabOrderResponse>>(
                $"api/lab-orders?appointmentId={appointmentId}", JsonOpts, ct);
            return response ?? [];
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch lab orders for appointment {AppointmentId}", appointmentId);
            return [];
        }
    }
}
