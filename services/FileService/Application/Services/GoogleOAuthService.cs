using System.Text.Json;
using FileService.Application.Configuration;
using FileService.Domain.Exceptions;
using FileService.Infrastructure.Repositories;
using Microsoft.Extensions.Options;

namespace FileService.Application.Services;

/// <summary>
/// Orchestrates the org-wide, single-connection Google OAuth handshake (real user-delegated
/// auth, replacing the broken service-account approach which has 0 bytes of its own Drive quota).
/// </summary>
public class GoogleOAuthService
{
    private const string Scope = "https://www.googleapis.com/auth/drive.file https://www.googleapis.com/auth/userinfo.email";

    private readonly GoogleOAuthOptions _options;
    private readonly IGoogleDriveConnectionRepository _repo;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<GoogleOAuthService> _logger;
    private GoogleOAuthClientInfo? _clientInfo;

    public GoogleOAuthService(
        IOptions<GoogleOAuthOptions> options,
        IGoogleDriveConnectionRepository repo,
        IHttpClientFactory httpClientFactory,
        ILogger<GoogleOAuthService> logger)
    {
        _options = options.Value;
        _repo = repo;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public string BuildAuthUrl()
    {
        var client = GetClientInfo();
        var query = string.Join("&", new[]
        {
            $"client_id={Uri.EscapeDataString(client.ClientId)}",
            $"redirect_uri={Uri.EscapeDataString(client.RedirectUri)}",
            "response_type=code",
            $"scope={Uri.EscapeDataString(Scope)}",
            "access_type=offline",
            "prompt=consent",
        });
        return $"{client.AuthUri}?{query}";
    }

    public async Task ExchangeCodeAndStoreAsync(string code, CancellationToken ct)
    {
        var client = GetClientInfo();
        var httpClient = _httpClientFactory.CreateClient();

        var tokenResponse = await httpClient.PostAsync(client.TokenUri, new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "authorization_code",
            ["code"] = code,
            ["client_id"] = client.ClientId,
            ["client_secret"] = client.ClientSecret,
            ["redirect_uri"] = client.RedirectUri,
        }), ct);
        tokenResponse.EnsureSuccessStatusCode();

        using var tokenDoc = JsonDocument.Parse(await tokenResponse.Content.ReadAsStringAsync(ct));
        var root = tokenDoc.RootElement;
        var accessToken = root.GetProperty("access_token").GetString() ?? string.Empty;
        var refreshToken = root.TryGetProperty("refresh_token", out var rt) ? rt.GetString() ?? string.Empty : string.Empty;
        var expiresIn = root.GetProperty("expires_in").GetInt32();
        var expiresAt = DateTime.UtcNow.AddSeconds(expiresIn);

        using var userInfoRequest = new HttpRequestMessage(HttpMethod.Get, "https://www.googleapis.com/oauth2/v2/userinfo");
        userInfoRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
        var userInfoResponse = await httpClient.SendAsync(userInfoRequest, ct);
        userInfoResponse.EnsureSuccessStatusCode();

        using var userInfoDoc = JsonDocument.Parse(await userInfoResponse.Content.ReadAsStringAsync(ct));
        var email = userInfoDoc.RootElement.TryGetProperty("email", out var e) ? e.GetString() ?? string.Empty : string.Empty;

        await _repo.UpsertAsync(accessToken, refreshToken, expiresAt, email, ct);
        _logger.LogInformation("Google Drive connected for {Email}", email);
    }

    public async Task<string> GetValidAccessTokenAsync(CancellationToken ct)
    {
        var conn = await _repo.GetAsync(ct) ?? throw new DriveNotConnectedException();

        if (conn.ExpiresAt > DateTime.UtcNow.AddMinutes(2))
            return conn.AccessToken;

        var client = GetClientInfo();
        var httpClient = _httpClientFactory.CreateClient();

        var tokenResponse = await httpClient.PostAsync(client.TokenUri, new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "refresh_token",
            ["refresh_token"] = conn.RefreshToken,
            ["client_id"] = client.ClientId,
            ["client_secret"] = client.ClientSecret,
        }), ct);
        tokenResponse.EnsureSuccessStatusCode();

        using var tokenDoc = JsonDocument.Parse(await tokenResponse.Content.ReadAsStringAsync(ct));
        var root = tokenDoc.RootElement;
        var accessToken = root.GetProperty("access_token").GetString() ?? string.Empty;
        var expiresIn = root.GetProperty("expires_in").GetInt32();
        var expiresAt = DateTime.UtcNow.AddSeconds(expiresIn);

        await _repo.UpdateAccessTokenAsync(accessToken, expiresAt, ct);
        return accessToken;
    }

    public async Task<(bool Connected, string? Email)> GetStatusAsync(CancellationToken ct)
    {
        var conn = await _repo.GetAsync(ct);
        return conn is null ? (false, null) : (true, conn.ConnectedEmail);
    }

    private GoogleOAuthClientInfo GetClientInfo()
    {
        if (_clientInfo is not null)
            return _clientInfo;

        if (string.IsNullOrWhiteSpace(_options.ClientSecretJsonPath) || !File.Exists(_options.ClientSecretJsonPath))
            throw new InvalidOperationException(
                $"Google OAuth client secret not found at '{_options.ClientSecretJsonPath}'. Drive connect feature is unavailable.");

        using var doc = JsonDocument.Parse(File.ReadAllText(_options.ClientSecretJsonPath));
        var web = doc.RootElement.GetProperty("web");

        var redirectUris = web.GetProperty("redirect_uris").EnumerateArray().Select(x => x.GetString() ?? string.Empty).ToList();

        _clientInfo = new GoogleOAuthClientInfo(
            ClientId: web.GetProperty("client_id").GetString() ?? string.Empty,
            ClientSecret: web.GetProperty("client_secret").GetString() ?? string.Empty,
            RedirectUri: redirectUris.FirstOrDefault() ?? string.Empty,
            AuthUri: web.GetProperty("auth_uri").GetString() ?? string.Empty,
            TokenUri: web.GetProperty("token_uri").GetString() ?? string.Empty);

        return _clientInfo;
    }
}
