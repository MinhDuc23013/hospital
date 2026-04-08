using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using System.Text.Json;
using System.Threading.RateLimiting;

namespace HospitalGateway.Extensions;

/// <summary>Extension methods for registering gateway services.</summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddGatewayAuthentication(
        this IServiceCollection services, IConfiguration config)
    {
        var authority = config["Keycloak:Authority"]
            ?? throw new InvalidOperationException("Keycloak:Authority must be configured.");
        var audience = config["Keycloak:Audience"] ?? "hospital-gateway";

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.Authority = authority;
                options.Audience = audience;
                options.RequireHttpsMetadata = false; // for local dev

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = false, // Keycloak audience is in azp, not aud
                    ValidateLifetime = true,
                    RoleClaimType = ClaimTypes.Role
                };

                options.Events = new JwtBearerEvents
                {
                    OnTokenValidated = ctx =>
                    {
                        // Map Keycloak realm_access.roles → ClaimTypes.Role
                        MapKeycloakRoles(ctx);
                        return Task.CompletedTask;
                    }
                };
            });

        services.AddAuthorization();
        return services;
    }

    /// <summary>
    /// Keycloak stores roles in realm_access.roles JSON object inside the token.
    /// .NET expects flat ClaimTypes.Role claims. This maps one to the other.
    /// </summary>
    private static void MapKeycloakRoles(TokenValidatedContext ctx)
    {
        var identity = ctx.Principal?.Identity as ClaimsIdentity;
        if (identity == null) return;

        // Extract realm_access.roles from token
        var realmAccess = ctx.Principal?.FindFirst("realm_access")?.Value;
        if (realmAccess != null)
        {
            var parsed = JsonDocument.Parse(realmAccess);
            if (parsed.RootElement.TryGetProperty("roles", out var roles))
            {
                foreach (var role in roles.EnumerateArray())
                {
                    var roleName = role.GetString();
                    if (roleName != null && !identity.HasClaim(ClaimTypes.Role, roleName))
                        identity.AddClaim(new Claim(ClaimTypes.Role, roleName));
                }
            }
        }

        // Also check resource_access.hospital-gateway.roles
        var resourceAccess = ctx.Principal?.FindFirst("resource_access")?.Value;
        if (resourceAccess != null)
        {
            var parsed = JsonDocument.Parse(resourceAccess);
            if (parsed.RootElement.TryGetProperty("hospital-gateway", out var client)
                && client.TryGetProperty("roles", out var roles))
            {
                foreach (var role in roles.EnumerateArray())
                {
                    var roleName = role.GetString();
                    if (roleName != null && !identity.HasClaim(ClaimTypes.Role, roleName))
                        identity.AddClaim(new Claim(ClaimTypes.Role, roleName));
                }
            }
        }
    }

    public static IServiceCollection AddGatewayRateLimiting(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            // Default: 100 requests per minute per client IP
            options.AddFixedWindowLimiter("default", opt =>
            {
                opt.PermitLimit = 100;
                opt.Window = TimeSpan.FromMinutes(1);
                opt.QueueLimit = 0;
            });
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        });
        return services;
    }
}
