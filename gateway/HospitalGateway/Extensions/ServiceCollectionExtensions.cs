using HospitalShared.Auth;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;

namespace HospitalGateway.Extensions;

/// <summary>Extension methods for registering gateway services.</summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddGatewayAuthentication(
        this IServiceCollection services, IConfiguration config)
    {
        services.AddKeycloakAuth(config);
        return services;
    }

    public static IServiceCollection AddGatewayRateLimiting(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
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
