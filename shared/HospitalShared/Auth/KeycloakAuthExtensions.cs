using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace HospitalShared.Auth;

/// <summary>
/// Shared Keycloak JWT authentication setup for all services.
/// Validates tokens from Keycloak and maps realm_access.roles to ClaimTypes.Role.
/// </summary>
public static class KeycloakAuthExtensions
{
    public static IServiceCollection AddKeycloakAuth(this IServiceCollection services, IConfiguration config)
    {
        var authority = config["Keycloak:Authority"]
            ?? throw new InvalidOperationException("Keycloak:Authority must be configured.");

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.Authority = authority;
                options.RequireHttpsMetadata = false;

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = false,
                    ValidateLifetime = true,
                    RoleClaimType = ClaimTypes.Role
                };

                options.Events = new JwtBearerEvents
                {
                    OnTokenValidated = ctx =>
                    {
                        MapKeycloakRoles(ctx.Principal);
                        return Task.CompletedTask;
                    }
                };
            });

        services.AddAuthorization();
        return services;
    }

    /// <summary>Map Keycloak realm_access.roles → ClaimTypes.Role claims.</summary>
    private static void MapKeycloakRoles(ClaimsPrincipal? principal)
    {
        if (principal?.Identity is not ClaimsIdentity identity) return;

        var realmAccess = principal.FindFirst("realm_access")?.Value;
        if (realmAccess == null) return;

        var parsed = JsonDocument.Parse(realmAccess);
        if (!parsed.RootElement.TryGetProperty("roles", out var roles)) return;

        foreach (var role in roles.EnumerateArray())
        {
            var name = role.GetString();
            if (name != null && !identity.HasClaim(ClaimTypes.Role, name))
                identity.AddClaim(new Claim(ClaimTypes.Role, name));
        }
    }
}
