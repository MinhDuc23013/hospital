using System.Text;
using System.Text.Json;

namespace PatientService.Infrastructure.HttpClients;

/// <summary>HTTP client for Auth Service — creates Keycloak users and assigns roles.</summary>
public class AuthServiceClient
{
    private readonly HttpClient _http;
    private readonly ILogger<AuthServiceClient> _logger;

    public AuthServiceClient(HttpClient http, ILogger<AuthServiceClient> logger)
    {
        _http = http;
        _logger = logger;
    }

    /// <summary>Create a user in Auth Service and return the Keycloak user ID.</summary>
    public async Task<string> CreateUserAsync(string email, string password, string firstName, string lastName, string role, CancellationToken ct)
    {
        var payload = new { email, password, firstName, lastName, role };
        var json = JsonSerializer.Serialize(payload);

        var response = await _http.PostAsync("/api/auth/users",
            new StringContent(json, Encoding.UTF8, "application/json"), ct);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(ct);
            _logger.LogError("Auth Service create user failed: {Status} {Error}", response.StatusCode, error);
            throw new InvalidOperationException($"Failed to create auth user: {error}");
        }

        var result = await response.Content.ReadFromJsonAsync<JsonElement>(ct);
        var keycloakUserId = result.GetProperty("keycloakUserId").GetString()
            ?? throw new InvalidOperationException("Auth Service did not return keycloakUserId");

        _logger.LogInformation("Auth user created for {Email}, KeycloakUserId={KeycloakUserId}", email, keycloakUserId);
        return keycloakUserId;
    }

    /// <summary>Delete a user from Auth Service (compensation on failure).</summary>
    public async Task DeleteUserAsync(string keycloakUserId, CancellationToken ct)
    {
        var response = await _http.DeleteAsync($"/api/auth/users/{keycloakUserId}", ct);
        if (response.IsSuccessStatusCode)
            _logger.LogInformation("Compensated: deleted auth user {KeycloakUserId}", keycloakUserId);
        else
            _logger.LogWarning("Failed to compensate auth user {KeycloakUserId}", keycloakUserId);
    }
}
