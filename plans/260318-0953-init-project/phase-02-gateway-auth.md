---
phase: 2
title: "API Gateway + Keycloak Configuration"
status: completed
priority: P1
effort: 2h
depends_on: [1]
blocks: [6]
completed: 2026-03-18
---

# Phase 2: Gateway + Auth

## Context Links
- [System Architecture](../../docs/system-architecture.md)
- [Code Standards .NET](../../docs/code-standards-dotnet.md)
- [Phase 1 — Foundation](./phase-01-foundation.md)

<!-- Updated: Validation Session 1 - Working Keycloak realm-export.json (auto-loaded on boot via docker-compose volume mount) -->

## Overview

Create HospitalGateway .NET 8 project with YARP reverse proxy, JWT validation middleware, rate limiting, health check endpoint, and **pre-configured Keycloak realm** (realm-export.json auto-imported on first boot).

## File Ownership

```
gateway/
  HospitalGateway/
    HospitalGateway.csproj
    Program.cs
    appsettings.json
    appsettings.Development.json
    Middleware/
      CorrelationIdMiddleware.cs
    Extensions/
      ServiceCollectionExtensions.cs
    Dockerfile
    .dockerignore
infra/
  keycloak/
    realm-export.json
```

**NOTE:** `infra/keycloak/` is owned by Phase 2 (not Phase 1). Phase 1 only creates the Keycloak Docker container; Phase 2 configures the realm.

## Key Insights
- YARP handles routing, load balancing, health checks natively — no custom reverse proxy code needed
- JWT validation uses built-in `Microsoft.AspNetCore.Authentication.JwtBearer` — validates against Keycloak OIDC discovery
- Rate limiting uses built-in `Microsoft.AspNetCore.RateLimiting` (.NET 7+)
- Correlation IDs: generate UUID per request, forward as `X-Correlation-Id` header to downstream

## Architecture

```
Client Request
    |
    v
HospitalGateway (port 8000)
    |-- CorrelationIdMiddleware (add X-Correlation-Id)
    |-- Authentication (JWT Bearer from Keycloak)
    |-- RateLimiting (fixed window per client)
    |-- YARP ReverseProxy
    |       |-- /api/patients/**     --> patient-service:5001
    |       |-- /api/appointments/** --> appointment-service:5002
    |       |-- /api/medical-records/** --> medical-record-service:5003
    |       |-- /api/prescriptions/** --> pharmacy-service:5004
    |       |-- /api/drugs/**        --> pharmacy-service:5004
    |       |-- /api/notifications/** --> notification-service:5005
    |       |-- /api/search/**       --> search-service:5006
    |-- /health (direct, no auth required)
```

## Related Code Files

### Files to Create

| File | Purpose | ~Lines |
|---|---|---|
| `HospitalGateway.csproj` | Project file with NuGet refs | 25 |
| `Program.cs` | DI, middleware pipeline, YARP setup | 80 |
| `appsettings.json` | YARP routes/clusters, JWT config | 120 |
| `appsettings.Development.json` | Dev overrides (relaxed auth) | 15 |
| `Middleware/CorrelationIdMiddleware.cs` | Add correlation ID to requests | 35 |
| `Extensions/ServiceCollectionExtensions.cs` | Auth + rate limit registration | 60 |
| `Dockerfile` | Multi-stage .NET build | 30 |
| `.dockerignore` | Exclude bin/obj/node_modules | 10 |
| `infra/keycloak/realm-export.json` | Hospital realm with roles/client | 80 |

## Implementation Steps

### Step 1: Create .NET Project

```bash
mkdir -p gateway/HospitalGateway
cd gateway/HospitalGateway
dotnet new web -n HospitalGateway --framework net8.0
```

### Step 2: Add NuGet Packages

Add to `HospitalGateway.csproj`:
```xml
<ItemGroup>
  <PackageReference Include="Yarp.ReverseProxy" Version="2.1.*" />
  <PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="8.0.*" />
  <PackageReference Include="Serilog.AspNetCore" Version="8.0.*" />
  <PackageReference Include="Serilog.Sinks.Seq" Version="6.0.*" />
  <PackageReference Include="Serilog.Sinks.Console" Version="5.0.*" />
</ItemGroup>

<ItemGroup>
  <ProjectReference Include="..\..\shared\HospitalShared\HospitalShared.csproj" />
</ItemGroup>
```

### Step 3: Program.cs

Middleware pipeline order:
1. `UseSerilogRequestLogging()`
2. `UseCorrelationId()` (custom)
3. `UseAuthentication()`
4. `UseAuthorization()`
5. `UseRateLimiter()`
6. Map `/health` endpoint (no auth) — returns `{ "status": "healthy", "timestamp": "..." }`
7. `MapReverseProxy()`

Key setup:
```csharp
var builder = WebApplication.CreateBuilder(args);

// Serilog
builder.Host.UseSerilog((ctx, cfg) => cfg
    .ReadFrom.Configuration(ctx.Configuration)
    .WriteTo.Console()
    .WriteTo.Seq(ctx.Configuration["Seq:Url"] ?? "http://localhost:5341"));

// YARP
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

// JWT Auth
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options => {
        options.Authority = builder.Configuration["Keycloak:Authority"];
        options.Audience = builder.Configuration["Keycloak:Audience"];
        options.RequireHttpsMetadata = false; // dev only
    });

// Rate Limiting
builder.Services.AddRateLimiter(options => {
    options.AddFixedWindowLimiter("default", opt => {
        opt.PermitLimit = 100;
        opt.Window = TimeSpan.FromMinutes(1);
    });
});
```

### Step 4: appsettings.json — YARP Routes

```json
{
  "ReverseProxy": {
    "Routes": {
      "patient-route": {
        "ClusterId": "patient-cluster",
        "Match": { "Path": "/api/patients/{**catch-all}" }
      },
      "appointment-route": {
        "ClusterId": "appointment-cluster",
        "Match": { "Path": "/api/appointments/{**catch-all}" }
      },
      "medical-record-route": {
        "ClusterId": "medical-record-cluster",
        "Match": { "Path": "/api/medical-records/{**catch-all}" }
      },
      "pharmacy-drugs-route": {
        "ClusterId": "pharmacy-cluster",
        "Match": { "Path": "/api/drugs/{**catch-all}" }
      },
      "pharmacy-prescriptions-route": {
        "ClusterId": "pharmacy-cluster",
        "Match": { "Path": "/api/prescriptions/{**catch-all}" }
      },
      "notification-route": {
        "ClusterId": "notification-cluster",
        "Match": { "Path": "/api/notifications/{**catch-all}" }
      },
      "search-route": {
        "ClusterId": "search-cluster",
        "Match": { "Path": "/api/search/{**catch-all}" }
      }
    },
    "Clusters": {
      "patient-cluster": {
        "Destinations": {
          "destination1": { "Address": "http://patient-service:5001" }
        }
      },
      "appointment-cluster": {
        "Destinations": {
          "destination1": { "Address": "http://appointment-service:5002" }
        }
      },
      "medical-record-cluster": {
        "Destinations": {
          "destination1": { "Address": "http://medical-record-service:5003" }
        }
      },
      "pharmacy-cluster": {
        "Destinations": {
          "destination1": { "Address": "http://pharmacy-service:5004" }
        }
      },
      "notification-cluster": {
        "Destinations": {
          "destination1": { "Address": "http://notification-service:5005" }
        }
      },
      "search-cluster": {
        "Destinations": {
          "destination1": { "Address": "http://search-service:5006" }
        }
      }
    }
  },
  "Keycloak": {
    "Authority": "http://keycloak:8080/realms/hospital",
    "Audience": "hospital-gateway"
  },
  "Seq": {
    "Url": "http://seq:5341"
  }
}
```

### Step 5: CorrelationIdMiddleware.cs

- Check incoming `X-Correlation-Id` header; if missing, generate `Guid.NewGuid().ToString()`
- Set on `HttpContext.TraceIdentifier`
- Add to response headers
- Push to Serilog `LogContext` via `LogContext.PushProperty("CorrelationId", correlationId)`

### Step 6: Dockerfile

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8000

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["gateway/HospitalGateway/HospitalGateway.csproj", "gateway/HospitalGateway/"]
COPY ["shared/HospitalShared/HospitalShared.csproj", "shared/HospitalShared/"]
RUN dotnet restore "gateway/HospitalGateway/HospitalGateway.csproj"
COPY . .
WORKDIR "/src/gateway/HospitalGateway"
RUN dotnet publish -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8000
ENTRYPOINT ["dotnet", "HospitalGateway.dll"]
```

### Step 7: Keycloak Realm Export

Create `infra/keycloak/realm-export.json`:
- Realm name: `hospital`
- Roles: `admin`, `doctor`, `nurse`, `pharmacist`, `patient`, `receptionist`
- Client: `hospital-gateway` (confidential, service accounts enabled)
- Valid redirect URIs: `http://localhost:8000/*`
- Token settings: access token lifespan 300s, refresh token 1800s

Mount in docker-compose: `./infra/keycloak/realm-export.json:/opt/keycloak/data/import/realm-export.json`
Keycloak start command: `start-dev --import-realm`

## Todo List

- [x] Create `gateway/HospitalGateway/` .NET project
- [x] Add NuGet packages (YARP, JwtBearer, Serilog)
- [x] Add ProjectReference to HospitalShared
- [x] Implement `Program.cs` with middleware pipeline
- [x] Configure `appsettings.json` with YARP routes
- [x] Implement `CorrelationIdMiddleware.cs`
- [x] Implement `ServiceCollectionExtensions.cs`
- [x] Create `Dockerfile` (multi-stage)
- [x] Create `infra/keycloak/realm-export.json`
- [x] Verify `dotnet build` succeeds
- [x] Verify Dockerfile builds: `docker build -f gateway/HospitalGateway/Dockerfile .`

## Success Criteria

- `dotnet build gateway/HospitalGateway/` compiles without errors
- Gateway starts on port 8000
- `GET /health` returns 200 with JSON status
- YARP routes configured for all 6 downstream services
- Keycloak realm imports on container start
- Correlation ID header present on all responses

## Risk Assessment

| Risk | Impact | Mitigation |
|---|---|---|
| Keycloak realm import fails silently | High | Check Keycloak logs, verify realm exists in admin console |
| YARP route mismatch (catch-all pattern) | Medium | Test each route pattern with curl |
| JWT validation fails in Docker (DNS) | Medium | Use container names, verify Keycloak URL reachable from gateway container |

## Security Considerations

- Health endpoint exempt from auth (needed for Docker healthcheck, Prometheus)
- All other routes require valid JWT Bearer token
- Rate limiting prevents brute-force attacks
- No secrets in appsettings.json — use env vars for Keycloak client secret
- HTTPS not enforced in dev (Docker internal network); required in production
