using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace AuthServiceDotnet.Infrastructure.Keycloak;

/// <summary>
/// HTTP client for Keycloak Admin REST API.
/// Handles token acquisition, user CRUD, role assignment, and password management.
/// </summary>
public class KeycloakAdminClient
{
    private readonly HttpClient _http;
    private readonly IConfiguration _config;
    private readonly ILogger<KeycloakAdminClient> _logger;

    private string BaseUrl => _config["Keycloak:AdminUrl"] ?? "http://localhost:8080";
    private string Realm => _config["Keycloak:Realm"] ?? "hospital";
    private string AdminUser => _config["Keycloak:AdminUser"] ?? "admin";
    private string AdminPassword => _config["Keycloak:AdminPassword"] ?? "admin";

    public KeycloakAdminClient(HttpClient http, IConfiguration config, ILogger<KeycloakAdminClient> logger)
    {
        _http = http;
        _config = config;
        _logger = logger;
    }

    private async Task<string> GetAdminTokenAsync(CancellationToken ct)
    {
        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "password",
            ["client_id"] = "admin-cli",
            ["username"] = AdminUser,
            ["password"] = AdminPassword
        });

        var response = await _http.PostAsync($"{BaseUrl}/realms/master/protocol/openid-connect/token", content, ct);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(ct);
        return json.GetProperty("access_token").GetString()!;
    }

    private async Task SetAuthHeaderAsync(CancellationToken ct)
    {
        var token = await GetAdminTokenAsync(ct);
        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    /// <summary>Create a user in Keycloak and return the user ID.</summary>
    public async Task<string> CreateUserAsync(string email, string password, string firstName, string lastName, CancellationToken ct)
    {
        await SetAuthHeaderAsync(ct);

        var user = new
        {
            username = email,
            email,
            firstName,
            lastName,
            enabled = true,
            emailVerified = true,
            credentials = new[] { new { type = "password", value = password, temporary = false } }
        };

        var response = await _http.PostAsync(
            $"{BaseUrl}/admin/realms/{Realm}/users",
            new StringContent(JsonSerializer.Serialize(user), Encoding.UTF8, "application/json"), ct);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(ct);
            _logger.LogError("Keycloak create user failed: {Status} {Error}", response.StatusCode, error);
            throw new InvalidOperationException($"Failed to create Keycloak user: {error}");
        }

        var location = response.Headers.Location?.ToString()
            ?? throw new InvalidOperationException("Keycloak did not return user location");
        var userId = location.Split('/').Last();

        _logger.LogInformation("Created Keycloak user {Email} with ID {UserId}", email, userId);
        return userId;
    }

    /// <summary>Assign a realm role to a user.</summary>
    public async Task AssignRoleAsync(string userId, string roleName, CancellationToken ct)
    {
        await SetAuthHeaderAsync(ct);

        var roleResponse = await _http.GetAsync($"{BaseUrl}/admin/realms/{Realm}/roles/{roleName}", ct);
        roleResponse.EnsureSuccessStatusCode();
        var role = await roleResponse.Content.ReadAsStringAsync(ct);

        var response = await _http.PostAsync(
            $"{BaseUrl}/admin/realms/{Realm}/users/{userId}/role-mappings/realm",
            new StringContent($"[{role}]", Encoding.UTF8, "application/json"), ct);
        response.EnsureSuccessStatusCode();

        _logger.LogInformation("Assigned role {Role} to user {UserId}", roleName, userId);
    }

    /// <summary>Change a user's password.</summary>
    public async Task ChangePasswordAsync(string userId, string newPassword, CancellationToken ct)
    {
        await SetAuthHeaderAsync(ct);

        var credential = new { type = "password", value = newPassword, temporary = false };
        var response = await _http.PutAsync(
            $"{BaseUrl}/admin/realms/{Realm}/users/{userId}/reset-password",
            new StringContent(JsonSerializer.Serialize(credential), Encoding.UTF8, "application/json"), ct);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException($"Failed to change password: {error}");
        }

        _logger.LogInformation("Password changed for user {UserId}", userId);
    }

    /// <summary>Find user by email, return Keycloak user ID or null.</summary>
    public async Task<string?> FindUserByEmailAsync(string email, CancellationToken ct)
    {
        await SetAuthHeaderAsync(ct);

        var response = await _http.GetAsync($"{BaseUrl}/admin/realms/{Realm}/users?email={email}&exact=true", ct);
        response.EnsureSuccessStatusCode();

        var users = await response.Content.ReadFromJsonAsync<JsonElement[]>(ct);
        if (users == null || users.Length == 0) return null;
        return users[0].GetProperty("id").GetString();
    }

    /// <summary>Get user details by Keycloak user ID.</summary>
    public async Task<JsonElement?> GetUserByIdAsync(string userId, CancellationToken ct)
    {
        await SetAuthHeaderAsync(ct);

        var response = await _http.GetAsync($"{BaseUrl}/admin/realms/{Realm}/users/{userId}", ct);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<JsonElement>(ct);
    }

    /// <summary>List all users with optional search and pagination.</summary>
    public async Task<JsonElement[]> ListUsersAsync(string? search, int first, int max, CancellationToken ct)
    {
        await SetAuthHeaderAsync(ct);

        var query = $"?first={first}&max={max}";
        if (!string.IsNullOrWhiteSpace(search)) query += $"&search={search}";

        var response = await _http.GetAsync($"{BaseUrl}/admin/realms/{Realm}/users{query}", ct);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<JsonElement[]>(ct) ?? [];
    }

    /// <summary>Delete a user from Keycloak.</summary>
    public async Task DeleteUserAsync(string userId, CancellationToken ct)
    {
        await SetAuthHeaderAsync(ct);

        var response = await _http.DeleteAsync($"{BaseUrl}/admin/realms/{Realm}/users/{userId}", ct);
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException($"Failed to delete user: {error}");
        }

        _logger.LogInformation("Deleted Keycloak user {UserId}", userId);
    }

    /// <summary>Update user details (firstName, lastName, enabled).</summary>
    public async Task UpdateUserAsync(string userId, string? firstName, string? lastName, bool? enabled, CancellationToken ct)
    {
        await SetAuthHeaderAsync(ct);

        var updates = new Dictionary<string, object>();
        if (firstName != null) updates["firstName"] = firstName;
        if (lastName != null) updates["lastName"] = lastName;
        if (enabled.HasValue) updates["enabled"] = enabled.Value;

        var response = await _http.PutAsync(
            $"{BaseUrl}/admin/realms/{Realm}/users/{userId}",
            new StringContent(JsonSerializer.Serialize(updates), Encoding.UTF8, "application/json"), ct);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException($"Failed to update user: {error}");
        }

        _logger.LogInformation("Updated user {UserId}", userId);
    }

    /// <summary>Get roles assigned to a user.</summary>
    public async Task<JsonElement[]> GetUserRolesAsync(string userId, CancellationToken ct)
    {
        await SetAuthHeaderAsync(ct);

        var response = await _http.GetAsync($"{BaseUrl}/admin/realms/{Realm}/users/{userId}/role-mappings/realm", ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<JsonElement[]>(ct) ?? [];
    }

    /// <summary>Remove a realm role from a user.</summary>
    public async Task RemoveRoleAsync(string userId, string roleName, CancellationToken ct)
    {
        await SetAuthHeaderAsync(ct);

        var roleResponse = await _http.GetAsync($"{BaseUrl}/admin/realms/{Realm}/roles/{roleName}", ct);
        roleResponse.EnsureSuccessStatusCode();
        var role = await roleResponse.Content.ReadAsStringAsync(ct);

        var request = new HttpRequestMessage(HttpMethod.Delete,
            $"{BaseUrl}/admin/realms/{Realm}/users/{userId}/role-mappings/realm")
        {
            Content = new StringContent($"[{role}]", Encoding.UTF8, "application/json")
        };

        var response = await _http.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();
        _logger.LogInformation("Removed role {Role} from user {UserId}", roleName, userId);
    }
}
