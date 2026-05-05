using HospitalGateway.Bff;

namespace HospitalGateway.Middleware;

/// <summary>
/// Runs before UseAuthentication. Reads the httpOnly bff_session cookie,
/// looks up the access token in Redis, and injects Authorization + X-User-Id
/// headers so downstream YARP proxying and JWT validation work unchanged.
/// </summary>
public class BffTokenInjectionMiddleware
{
    private readonly RequestDelegate _next;

    public BffTokenInjectionMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext ctx, BffSessionService sessions)
    {
        // Only inject for proxied API routes; skip BFF and health endpoints
        if (ctx.Request.Path.StartsWithSegments("/api") &&
            !ctx.Request.Headers.ContainsKey("Authorization"))
        {
            var sessionId = ctx.Request.Cookies["bff_session"];
            if (sessionId is not null)
            {
                var session = await sessions.GetAsync(sessionId);
                if (session is not null)
                {
                    ctx.Request.Headers.Authorization = $"Bearer {session.AccessToken}";
                    ctx.Request.Headers["X-User-Id"]  = session.UserId;
                }
            }
        }

        await _next(ctx);
    }
}
