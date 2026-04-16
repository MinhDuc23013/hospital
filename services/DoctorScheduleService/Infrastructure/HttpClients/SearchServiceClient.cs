using System.Text.Json;
using Microsoft.AspNetCore.Http;

namespace DoctorScheduleService.Infrastructure.HttpClients;

/// <summary>HTTP client for SearchService — delegates full-text search to Elasticsearch.</summary>
public class SearchServiceClient
{
    private readonly HttpClient _http;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<SearchServiceClient> _logger;
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    public SearchServiceClient(HttpClient http, IHttpContextAccessor httpContextAccessor, ILogger<SearchServiceClient> logger)
    {
        _http = http;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    /// <summary>Search doctors via SearchService Elasticsearch index. Returns null on failure (caller should fallback to DB).</summary>
    public async Task<SearchResult?> SearchDoctorsAsync(
        string searchName, bool? isActive, int page, int pageSize, CancellationToken ct)
    {
        var url = $"/api/search?q={Uri.EscapeDataString(searchName)}&type=doctor&page={page}&pageSize={pageSize}";
        if (isActive.HasValue)
            url += $"&isActive={isActive.Value.ToString().ToLower()}";

        try
        {
            // Forward Authorization header from incoming request
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            var authHeader = _httpContextAccessor.HttpContext?.Request.Headers.Authorization.FirstOrDefault();
            if (!string.IsNullOrEmpty(authHeader))
                request.Headers.TryAddWithoutValidation("Authorization", authHeader);

            var response = await _http.SendAsync(request, ct);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("SearchService returned {Status} for doctor search", response.StatusCode);
                return null;
            }

            var result = await response.Content.ReadFromJsonAsync<SearchResult>(JsonOpts, ct);
            return result;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            _logger.LogWarning(ex, "SearchService unavailable, will fallback to DB");
            return null;
        }
    }

    public class SearchResult
    {
        public List<DoctorSearchItem> Data { get; set; } = new();
        public PaginationInfo Pagination { get; set; } = new();
    }

    public class DoctorSearchItem
    {
        public string DoctorId  { get; set; } = string.Empty;
        public string FullName  { get; set; } = string.Empty;
        public string Specialty { get; set; } = string.Empty;
        public string? Phone   { get; set; }
        public string? Email   { get; set; }
        public bool   IsActive { get; set; }
    }

    public class PaginationInfo
    {
        public long Total    { get; set; }
        public int  Page     { get; set; }
        public int  PageSize { get; set; }
    }
}
