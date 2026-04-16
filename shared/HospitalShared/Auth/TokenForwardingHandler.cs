using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;

namespace HospitalShared.Auth;

/// <summary>
/// DelegatingHandler that forwards the incoming request's Authorization header
/// to outgoing HttpClient calls. Enables service-to-service auth propagation
/// without manual token handling in each client.
///
/// Usage: builder.Services.AddHttpClient&lt;MyClient&gt;().AddTokenForwarding();
/// </summary>
public class TokenForwardingHandler : DelegatingHandler
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public TokenForwardingHandler(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var authHeader = _httpContextAccessor.HttpContext?.Request.Headers.Authorization.FirstOrDefault();
        if (!string.IsNullOrEmpty(authHeader) && !request.Headers.Contains("Authorization"))
        {
            request.Headers.TryAddWithoutValidation("Authorization", authHeader);
        }

        return base.SendAsync(request, cancellationToken);
    }
}

/// <summary>Extension to register TokenForwardingHandler on any HttpClient.</summary>
public static class TokenForwardingExtensions
{
    public static IHttpClientBuilder AddTokenForwarding(this IHttpClientBuilder builder)
    {
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddTransient<TokenForwardingHandler>();
        return builder.AddHttpMessageHandler<TokenForwardingHandler>();
    }
}
