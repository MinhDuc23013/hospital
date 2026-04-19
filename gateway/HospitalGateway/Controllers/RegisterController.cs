using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StackExchange.Redis;

namespace HospitalGateway.Controllers;

/// <summary>
/// Self-registration endpoint for patient accounts.
/// Creates user in Keycloak with role 'patient', triggers email verification.
/// </summary>
[ApiController]
[Route("api/auth")]
[AllowAnonymous]
public class RegisterController : ControllerBase
{
    private const int MaxRegistrationsPerHour = 3;
    private static readonly TimeSpan RateLimitWindow = TimeSpan.FromHours(1);

    private readonly IConnectionMultiplexer _redis;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _config;
    private readonly ILogger<RegisterController> _logger;

    public RegisterController(
        IConnectionMultiplexer redis,
        IHttpClientFactory httpClientFactory,
        IConfiguration config,
        ILogger<RegisterController> logger)
    {
        _redis = redis;
        _httpClientFactory = httpClientFactory;
        _config = config;
        _logger = logger;
    }

    public record RegisterRequest(
        string Email,
        string Password,
        string FirstName,
        string LastName);

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest req, CancellationToken ct)
    {
        // Basic validation
        if (string.IsNullOrWhiteSpace(req.Email) || !req.Email.Contains('@'))
            return BadRequest(new { error = new { code = "INVALID_EMAIL", message = "Invalid email format" } });
        if (string.IsNullOrWhiteSpace(req.Password) || req.Password.Length < 8)
            return BadRequest(new { error = new { code = "WEAK_PASSWORD", message = "Password must be at least 8 characters" } });
        if (string.IsNullOrWhiteSpace(req.FirstName) || string.IsNullOrWhiteSpace(req.LastName))
            return BadRequest(new { error = new { code = "INVALID_NAME", message = "First name and last name are required" } });

        // Rate limit by IP (prevent spam registrations)
        var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var db = _redis.GetDatabase();
        var rlKey = $"register_rate:{clientIp}";
        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        await db.SortedSetRemoveRangeByScoreAsync(rlKey, double.NegativeInfinity, now - (long)RateLimitWindow.TotalMilliseconds);
        var count = await db.SortedSetLengthAsync(rlKey);
        if (count >= MaxRegistrationsPerHour)
        {
            return StatusCode(429, new { error = new { code = "TOO_MANY_REGISTRATIONS", message = "Too many registration attempts. Try again later." } });
        }

        try
        {
            // Get Keycloak admin token
            var adminToken = await GetAdminTokenAsync(ct);
            if (adminToken is null)
                return StatusCode(503, new { error = new { code = "AUTH_SERVICE_UNAVAILABLE", message = "Registration temporarily unavailable" } });

            var keycloakBase = _config["Keycloak:AdminUrl"] ?? "http://keycloak:8080";
            var realm = "hospital";

            using var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

            // Check if user already exists
            var searchResp = await client.GetAsync(
                $"{keycloakBase}/admin/realms/{realm}/users?email={Uri.EscapeDataString(req.Email)}&exact=true", ct);
            if (searchResp.IsSuccessStatusCode)
            {
                var existingUsers = await searchResp.Content.ReadFromJsonAsync<JsonElement[]>(cancellationToken: ct);
                if (existingUsers is { Length: > 0 })
                    return Conflict(new { error = new { code = "EMAIL_EXISTS", message = "Email already registered" } });
            }

            // Create user with emailVerified=false + requiredActions=[VERIFY_EMAIL]
            var createPayload = new
            {
                username = req.Email,
                email = req.Email,
                firstName = req.FirstName,
                lastName = req.LastName,
                enabled = true,
                emailVerified = false,
                requiredActions = new[] { "VERIFY_EMAIL" },
                credentials = new[]
                {
                    new { type = "password", value = req.Password, temporary = false }
                }
            };

            var createResp = await client.PostAsync(
                $"{keycloakBase}/admin/realms/{realm}/users",
                new StringContent(JsonSerializer.Serialize(createPayload), Encoding.UTF8, "application/json"), ct);

            if (!createResp.IsSuccessStatusCode)
            {
                var errBody = await createResp.Content.ReadAsStringAsync(ct);
                _logger.LogWarning("Keycloak user create failed: {Status} {Body}", createResp.StatusCode, errBody);
                return StatusCode((int)createResp.StatusCode, new { error = new { code = "CREATE_FAILED", message = "Failed to create user" } });
            }

            // Get created user ID from Location header
            var location = createResp.Headers.Location?.ToString() ?? "";
            var userId = location.Split('/').LastOrDefault() ?? "";
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogError("Could not extract user ID from Location header: {Location}", location);
                return StatusCode(500, new { error = new { code = "CREATE_FAILED", message = "User created but ID not returned" } });
            }

            // Assign 'patient' realm role
            await AssignPatientRoleAsync(client, keycloakBase, realm, userId, ct);

            // Trigger verification email
            await client.PutAsync(
                $"{keycloakBase}/admin/realms/{realm}/users/{userId}/send-verify-email",
                new StringContent(""), ct);

            // Track for rate limit
            await db.SortedSetAddAsync(rlKey, $"{now}-{userId}", now);
            await db.KeyExpireAsync(rlKey, RateLimitWindow);

            _logger.LogInformation("Registered patient user {UserId} ({Email})", userId, req.Email);

            return Ok(new
            {
                success = true,
                message = "Registration successful. Please check your email to verify your account.",
                userId,
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Registration failed for email {Email}", req.Email);
            return StatusCode(500, new { error = new { code = "INTERNAL_ERROR", message = "Registration failed" } });
        }
    }

    private async Task<string?> GetAdminTokenAsync(CancellationToken ct)
    {
        var keycloakBase = _config["Keycloak:AdminUrl"] ?? "http://keycloak:8080";
        var adminUser = _config["Keycloak:AdminUser"] ?? "admin";
        var adminPass = _config["Keycloak:AdminPassword"] ?? "admin_password_change_me";

        using var client = _httpClientFactory.CreateClient();
        var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "password",
            ["client_id"] = "admin-cli",
            ["username"] = adminUser,
            ["password"] = adminPass,
        });

        var resp = await client.PostAsync($"{keycloakBase}/realms/master/protocol/openid-connect/token", form, ct);
        if (!resp.IsSuccessStatusCode) return null;

        var json = await resp.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);
        return json.GetProperty("access_token").GetString();
    }

    private async Task AssignPatientRoleAsync(HttpClient client, string keycloakBase, string realm, string userId, CancellationToken ct)
    {
        // Get 'patient' realm role
        var roleResp = await client.GetAsync($"{keycloakBase}/admin/realms/{realm}/roles/patient", ct);
        if (!roleResp.IsSuccessStatusCode)
        {
            _logger.LogWarning("Patient role not found in realm; skipping role assignment");
            return;
        }
        var role = await roleResp.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);
        var roleArray = new[] { new
        {
            id = role.GetProperty("id").GetString(),
            name = role.GetProperty("name").GetString(),
        } };

        await client.PostAsync(
            $"{keycloakBase}/admin/realms/{realm}/users/{userId}/role-mappings/realm",
            new StringContent(JsonSerializer.Serialize(roleArray), Encoding.UTF8, "application/json"), ct);
    }
}
