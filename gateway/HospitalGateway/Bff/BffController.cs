using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

namespace HospitalGateway.Bff;

public record BffLoginRequest(string Username, string Password, string? Totp);
public record BffCallbackRequest(string Code, string RedirectUri);
record KeycloakTokenResponse(
    string access_token, string refresh_token, int expires_in, string? id_token);

/// <summary>
/// BFF (Backend for Frontend) session endpoints.
/// Browser holds only an httpOnly session cookie — tokens live in Redis.
/// </summary>
[ApiController]
[Route("bff")]
public class BffController : ControllerBase
{
    private readonly BffSessionService _sessions;
    private readonly IHttpClientFactory _http;
    private readonly IConfiguration _config;
    private readonly ILogger<BffController> _logger;

    private string KeycloakUrl => _config["Keycloak:AdminUrl"] ?? "http://keycloak:8080";
    private string Realm       => _config["Keycloak:Realm"]    ?? "hospital";
    private string ClientId    => _config["BFF:ClientId"]      ?? "hospital-frontend";
    private string ClientSecret => _config["BFF:ClientSecret"] ?? "";

    public BffController(
        BffSessionService sessions, IHttpClientFactory http,
        IConfiguration config, ILogger<BffController> logger)
    {
        _sessions = sessions;
        _http     = http;
        _config   = config;
        _logger   = logger;
    }

    /// <summary>Login with username/password (+ optional TOTP). Sets httpOnly session cookie.</summary>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] BffLoginRequest req, CancellationToken ct)
    {
        var form = new Dictionary<string, string>
        {
            ["grant_type"]    = "password",
            ["client_id"]     = ClientId,
            ["client_secret"] = ClientSecret,
            ["username"]      = req.Username,
            ["password"]      = req.Password,
        };
        if (!string.IsNullOrEmpty(req.Totp)) form["totp"] = req.Totp;

        var response = await PostToKeycloak(form, ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            return StatusCode((int)response.StatusCode, JsonDocument.Parse(body).RootElement);
        }

        var tokens = await response.Content.ReadFromJsonAsync<KeycloakTokenResponse>(ct);
        if (tokens is null) return StatusCode(500);

        var session   = BuildSession(tokens);
        var sessionId = await _sessions.CreateAsync(session, ct);
        SetSessionCookie(sessionId);

        _logger.LogInformation("BFF login: {Email}", session.Email);
        return Ok(new { user = ToUserDto(session) });
    }

    /// <summary>Exchange OAuth authorization code for session (Google login callback).</summary>
    [HttpPost("callback")]
    public async Task<IActionResult> Callback([FromBody] BffCallbackRequest req, CancellationToken ct)
    {
        var form = new Dictionary<string, string>
        {
            ["grant_type"]    = "authorization_code",
            ["client_id"]     = ClientId,
            ["client_secret"] = ClientSecret,
            ["code"]          = req.Code,
            ["redirect_uri"]  = req.RedirectUri,
        };

        var response = await PostToKeycloak(form, ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            return StatusCode((int)response.StatusCode, JsonDocument.Parse(body).RootElement);
        }

        var tokens = await response.Content.ReadFromJsonAsync<KeycloakTokenResponse>(ct);
        if (tokens is null) return StatusCode(500);

        var session   = BuildSession(tokens);
        var sessionId = await _sessions.CreateAsync(session, ct);
        SetSessionCookie(sessionId);

        return Ok(new { user = ToUserDto(session) });
    }

    /// <summary>Refresh access token using stored refresh token. Transparent to browser.</summary>
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh(CancellationToken ct)
    {
        var sessionId = Request.Cookies["bff_session"];
        if (sessionId is null) return Unauthorized();

        var session = await _sessions.GetAsync(sessionId, ct);
        if (session is null) return Unauthorized();

        var form = new Dictionary<string, string>
        {
            ["grant_type"]    = "refresh_token",
            ["client_id"]     = ClientId,
            ["client_secret"] = ClientSecret,
            ["refresh_token"] = session.RefreshToken,
        };

        var response = await PostToKeycloak(form, ct);
        if (!response.IsSuccessStatusCode)
        {
            // Refresh token expired — clear session, force re-login
            await _sessions.DeleteAsync(sessionId, ct);
            ClearSessionCookie();
            return Unauthorized(new { error = "RefreshTokenExpired" });
        }

        var tokens    = await response.Content.ReadFromJsonAsync<KeycloakTokenResponse>(ct);
        if (tokens is null) return StatusCode(500);

        var expiresAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds() + tokens.expires_in;
        await _sessions.UpdateTokensAsync(
            sessionId, tokens.access_token, tokens.refresh_token ?? session.RefreshToken, expiresAt, ct);

        return Ok();
    }

    /// <summary>Return current user info from session. 401 if no session.</summary>
    [HttpGet("me")]
    public async Task<IActionResult> Me(CancellationToken ct)
    {
        var sessionId = Request.Cookies["bff_session"];
        if (sessionId is null) return Unauthorized();

        var session = await _sessions.GetAsync(sessionId, ct);
        if (session is null) return Unauthorized();

        return Ok(ToUserDto(session));
    }

    /// <summary>Logout — deletes Redis session and clears cookie.</summary>
    [HttpPost("logout")]
    public async Task<IActionResult> Logout(CancellationToken ct)
    {
        var sessionId = Request.Cookies["bff_session"];
        if (sessionId is not null)
            await _sessions.DeleteAsync(sessionId, ct);

        ClearSessionCookie();
        return Ok();
    }

    // ── helpers ──────────────────────────────────────────────────────────────

    private Task<HttpResponseMessage> PostToKeycloak(Dictionary<string, string> form, CancellationToken ct)
    {
        var http = _http.CreateClient();
        return http.PostAsync(
            $"{KeycloakUrl}/realms/{Realm}/protocol/openid-connect/token",
            new FormUrlEncodedContent(form), ct);
    }

    private static BffSession BuildSession(KeycloakTokenResponse tokens)
    {
        var payload = ParseJwtPayload(tokens.access_token);
        return new BffSession(
            AccessToken:  tokens.access_token,
            RefreshToken: tokens.refresh_token ?? "",
            ExpiresAt:    DateTimeOffset.UtcNow.ToUnixTimeSeconds() + tokens.expires_in,
            UserId:       payload.TryGetProperty("sub",   out var sub)   ? sub.GetString()!   : "",
            Email:        payload.TryGetProperty("email", out var email) ? email.GetString()! : "",
            FullName:     payload.TryGetProperty("name",  out var name)  ? name.GetString()   : null,
            Roles:        ExtractRoles(payload)
        );
    }

    private static object ToUserDto(BffSession s) => new
    {
        userId   = s.UserId,
        email    = s.Email,
        fullName = s.FullName,
        roles    = s.Roles,
        expiresAt = s.ExpiresAt,
    };

    private static JsonElement ParseJwtPayload(string token)
    {
        var segment = token.Split('.')[1];
        // Pad base64url → standard base64
        var padded = segment.Replace('-', '+').Replace('_', '/');
        padded = padded.PadRight(padded.Length + (4 - padded.Length % 4) % 4, '=');
        return JsonSerializer.Deserialize<JsonElement>(Convert.FromBase64String(padded));
    }

    private static string[] ExtractRoles(JsonElement payload)
    {
        if (!payload.TryGetProperty("realm_access", out var ra)) return [];
        if (!ra.TryGetProperty("roles", out var roles)) return [];
        return [.. roles.EnumerateArray()
            .Select(r => r.GetString())
            .Where(r => r is not null)
            .Cast<string>()];
    }

    private void SetSessionCookie(string sessionId)
    {
        Response.Cookies.Append("bff_session", sessionId, new CookieOptions
        {
            HttpOnly = true,
            Secure   = false,             // set true behind HTTPS in production
            SameSite = SameSiteMode.Lax,
            Path     = "/",
            MaxAge   = TimeSpan.FromHours(8),
        });
    }

    private void ClearSessionCookie() =>
        Response.Cookies.Delete("bff_session", new CookieOptions { Path = "/", SameSite = SameSiteMode.Lax });
}
