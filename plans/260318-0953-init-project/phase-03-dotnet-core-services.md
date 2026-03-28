---
phase: 3
title: ".NET Core Services — PatientService + AppointmentService"
status: completed
priority: P1
effort: 3h
depends_on: [1]
blocks: [6]
completed: 2026-03-18
---

# Phase 3: .NET Core Services

## Context Links
- [System Architecture](../../docs/system-architecture.md)
- [Code Standards .NET](../../docs/code-standards-dotnet.md)
- [Phase 1 — Foundation](./phase-01-foundation.md)

<!-- Updated: Validation Session 1 - MassTransit wired in DI (not stub); EF Core initial migration included; Clean Architecture + CQRS/MediatR confirmed -->

## Overview

Scaffold PatientService and AppointmentService as .NET 8 Web API projects with **Clean Architecture + CQRS/MediatR**. Both use PostgreSQL via EF Core (+ `dotnet ef migrations add Initial`), **MassTransit registered in DI** with empty consumer/publisher classes, Serilog for logging. Scaffold only — health endpoints, basic CRUD skeleton, DB context, migration, event stubs.

## File Ownership

```
services/
  PatientService/
    PatientService.csproj
    Program.cs
    appsettings.json
    Dockerfile
    .dockerignore
    Controllers/
      PatientsController.cs
      HealthController.cs
    Application/
      Commands/CreatePatientCommand.cs
      Commands/UpdatePatientCommand.cs
      Queries/GetPatientQuery.cs
      Queries/ListPatientsQuery.cs
      Handlers/CreatePatientHandler.cs
      Handlers/GetPatientHandler.cs
      Validators/CreatePatientValidator.cs
    Domain/
      Entities/Patient.cs
      Events/PatientDomainEvents.cs
      Exceptions/DomainException.cs
    Infrastructure/
      Persistence/PatientDbContext.cs
      Persistence/PatientConfiguration.cs
      Repositories/PatientRepository.cs
      Repositories/IPatientRepository.cs
      MessageBus/EventPublisher.cs
    Extensions/
      ServiceCollectionExtensions.cs
    Middleware/
      ExceptionHandlingMiddleware.cs

  AppointmentService/
    AppointmentService.csproj
    Program.cs
    appsettings.json
    Dockerfile
    .dockerignore
    Controllers/
      AppointmentsController.cs
      HealthController.cs
    Application/
      Commands/ScheduleAppointmentCommand.cs
      Commands/CancelAppointmentCommand.cs
      Queries/GetAppointmentQuery.cs
      Queries/ListAppointmentsQuery.cs
      Handlers/ScheduleAppointmentHandler.cs
      Handlers/GetAppointmentHandler.cs
      Validators/ScheduleAppointmentValidator.cs
    Domain/
      Entities/Appointment.cs
      Entities/TimeSlot.cs
      Enums/AppointmentStatus.cs
      Events/AppointmentDomainEvents.cs
      Exceptions/DomainException.cs
    Infrastructure/
      Persistence/AppointmentDbContext.cs
      Persistence/AppointmentConfiguration.cs
      Repositories/AppointmentRepository.cs
      Repositories/IAppointmentRepository.cs
      MessageBus/EventPublisher.cs
      HttpClients/PatientServiceClient.cs
    Extensions/
      ServiceCollectionExtensions.cs
    Middleware/
      ExceptionHandlingMiddleware.cs
```

## Architecture (Both Services)

```
Controller (HTTP) --> MediatR --> Handler --> Repository --> DbContext --> PostgreSQL
                                    |
                                    +--> EventPublisher --> RabbitMQ (MassTransit)
```

Clean Architecture layers:
- **Controllers** — HTTP endpoints, model binding, response mapping
- **Application** — Commands/Queries (CQRS via MediatR), Validators (FluentValidation), Handlers
- **Domain** — Entities (aggregate roots), Value Objects, Domain Events, Exceptions
- **Infrastructure** — EF Core DbContext, Repositories, MassTransit publisher, HTTP clients

## Implementation Steps

### Step 1: PatientService Project Setup

```bash
cd services
dotnet new webapi -n PatientService --framework net8.0 --no-https
cd PatientService
```

**1.1 NuGet packages for `PatientService.csproj`:**

```xml
<ItemGroup>
  <PackageReference Include="Microsoft.EntityFrameworkCore" Version="8.0.*" />
  <PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="8.0.*" />
  <PackageReference Include="Microsoft.EntityFrameworkCore.Tools" Version="8.0.*" />
  <PackageReference Include="MediatR" Version="12.2.*" />
  <PackageReference Include="MassTransit.RabbitMQ" Version="8.1.*" />
  <PackageReference Include="FluentValidation.AspNetCore" Version="11.3.*" />
  <PackageReference Include="Serilog.AspNetCore" Version="8.0.*" />
  <PackageReference Include="Serilog.Sinks.Seq" Version="6.0.*" />
  <PackageReference Include="Serilog.Sinks.Console" Version="5.0.*" />
  <PackageReference Include="AutoMapper.Extensions.Microsoft.DependencyInjection" Version="12.0.*" />
</ItemGroup>
<ItemGroup>
  <ProjectReference Include="..\..\shared\HospitalShared\HospitalShared.csproj" />
</ItemGroup>
```

### Step 2: PatientService — Domain Layer

**2.1 `Domain/Entities/Patient.cs`** (~50 lines)

```csharp
public class Patient
{
    public Guid Id { get; private set; }
    public string Email { get; private set; }
    public string FirstName { get; private set; }
    public string LastName { get; private set; }
    public DateTime DateOfBirth { get; private set; }
    public string? PhoneNumber { get; private set; }
    public string? AddressStreet { get; private set; }
    public string? AddressCity { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    // Factory method
    public static Patient Create(string email, string firstName, string lastName, DateTime dob)
    {
        return new Patient
        {
            Id = Guid.NewGuid(),
            Email = email,
            FirstName = firstName,
            LastName = lastName,
            DateOfBirth = dob,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }
}
```

**2.2 `Domain/Exceptions/DomainException.cs`** (~10 lines) — Custom exception with message

### Step 3: PatientService — Infrastructure Layer

**3.1 `Infrastructure/Persistence/PatientDbContext.cs`** (~30 lines)

```csharp
public class PatientDbContext : DbContext
{
    public DbSet<Patient> Patients => Set<Patient>();

    public PatientDbContext(DbContextOptions<PatientDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new PatientConfiguration());
    }
}
```

**3.2 `Infrastructure/Persistence/PatientConfiguration.cs`** (~25 lines)

EF Core configuration per code-standards.md:
- Table name: `patients` (snake_case)
- Email: required, max 255, unique index
- FirstName/LastName: required, max 100
- CreatedAt: default `NOW()`

**3.3 `Infrastructure/Repositories/IPatientRepository.cs`** (~15 lines)

```csharp
public interface IPatientRepository
{
    Task<Patient?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<(List<Patient> Items, int Total)> ListAsync(int page, int pageSize, CancellationToken ct = default);
    Task AddAsync(Patient patient, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
```

**3.4 `Infrastructure/Repositories/PatientRepository.cs`** (~40 lines) — EF Core implementation

**3.5 `Infrastructure/MessageBus/EventPublisher.cs`** (~20 lines) — Wraps MassTransit `IPublishEndpoint`

### Step 4: PatientService — Application Layer

**4.1 Commands** (~15 lines each)
- `CreatePatientCommand` — record with FirstName, LastName, Email, DateOfBirth, PhoneNumber
- `UpdatePatientCommand` — record with Id + fields

**4.2 Queries** (~10 lines each)
- `GetPatientQuery` — record with Id, returns PatientDto
- `ListPatientsQuery` — record with Page, PageSize, returns paginated list

**4.3 Handlers** (~40 lines each)
- `CreatePatientHandler` — validates, creates entity, saves, publishes PatientCreatedEvent, returns DTO
- `GetPatientHandler` — fetches by ID, maps to DTO, returns (or null)

**4.4 Validators** (~15 lines)
- `CreatePatientValidator` — FluentValidation: Email not empty + valid format, FirstName required, LastName required

### Step 5: PatientService — Controllers

**5.1 `Controllers/PatientsController.cs`** (~60 lines)

```csharp
[ApiController]
[Route("api/[controller]")]
public class PatientsController : ControllerBase
{
    private readonly IMediator _mediator;

    [HttpPost]
    public async Task<ActionResult<PatientDto>> Create([FromBody] CreatePatientCommand command)
    {
        var result = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<PatientDto>> GetById(Guid id) { ... }

    [HttpGet]
    public async Task<ActionResult> List([FromQuery] int page = 1, [FromQuery] int pageSize = 50) { ... }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<PatientDto>> Update(Guid id, [FromBody] UpdatePatientCommand command) { ... }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id) { ... }  // soft delete
}
```

**5.2 `Controllers/HealthController.cs`** (~15 lines)
- `GET /health` returns `{ "service": "PatientService", "status": "healthy", "timestamp": "..." }`

### Step 6: PatientService — Program.cs & Config

**6.1 `Program.cs`** (~70 lines)

```csharp
var builder = WebApplication.CreateBuilder(args);

// Serilog
builder.Host.UseSerilog(...);

// EF Core + PostgreSQL
builder.Services.AddDbContext<PatientDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("PostgreSQL")));

// MediatR
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));

// FluentValidation
builder.Services.AddValidatorsFromAssembly(typeof(Program).Assembly);

// MassTransit + RabbitMQ
builder.Services.AddMassTransit(x => {
    x.UsingRabbitMq((ctx, cfg) => {
        cfg.Host(builder.Configuration["RabbitMQ:Host"], "/", h => {
            h.Username(builder.Configuration["RabbitMQ:User"]);
            h.Password(builder.Configuration["RabbitMQ:Password"]);
        });
    });
});

// Repositories
builder.Services.AddScoped<IPatientRepository, PatientRepository>();

builder.Services.AddControllers();

var app = builder.Build();
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.MapControllers();
app.Run();
```

**6.2 `appsettings.json`** (~30 lines)

```json
{
  "ConnectionStrings": {
    "PostgreSQL": "Host=postgres;Database=hospital_db;Username=hospital;Password=dev_password_change_me"
  },
  "RabbitMQ": {
    "Host": "rabbitmq",
    "User": "guest",
    "Password": "guest"
  },
  "Seq": { "Url": "http://seq:5341" },
  "Urls": "http://+:5001"
}
```

### Step 7: PatientService — Middleware

**7.1 `Middleware/ExceptionHandlingMiddleware.cs`** (~40 lines)

Catches exceptions, returns standardized error JSON:
- `DomainException` -> 404
- `ArgumentException` -> 400
- `ValidationException` -> 400
- Unhandled -> 500
- Logs with Serilog

### Step 8: PatientService — Dockerfile

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 5001

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["services/PatientService/PatientService.csproj", "services/PatientService/"]
COPY ["shared/HospitalShared/HospitalShared.csproj", "shared/HospitalShared/"]
RUN dotnet restore "services/PatientService/PatientService.csproj"
COPY . .
WORKDIR "/src/services/PatientService"
RUN dotnet publish -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:5001
ENTRYPOINT ["dotnet", "PatientService.dll"]
```

---

### Step 9: AppointmentService (Mirror PatientService Structure)

AppointmentService follows the same Clean Architecture pattern. Key differences:

**9.1 Domain**

`Domain/Entities/Appointment.cs`:
- Id (Guid), PatientId (Guid), ProviderId (string), ScheduledTime (DateTime), Duration (TimeSpan)
- Status (enum: Scheduled, InProgress, Completed, Cancelled), Notes (string?)

`Domain/Entities/TimeSlot.cs`:
- Id (Guid), ProviderId (string), SlotDateTime (DateTime), IsAvailable (bool)

`Domain/Enums/AppointmentStatus.cs`:
- Enum: Scheduled = 0, InProgress = 1, Completed = 2, Cancelled = 3

**9.2 Application**

Commands:
- `ScheduleAppointmentCommand` — PatientId, ProviderId, ScheduledTime, Duration, Notes
- `CancelAppointmentCommand` — AppointmentId, Reason

Queries:
- `GetAppointmentQuery` — Id
- `ListAppointmentsQuery` — PatientId (optional), ProviderId (optional), Page, PageSize

Handlers:
- `ScheduleAppointmentHandler` — validates, checks time slot, creates appointment, publishes AppointmentScheduledEvent
- `GetAppointmentHandler` — fetches by ID

**9.3 Infrastructure**

- `AppointmentDbContext` with `Appointments` and `TimeSlots` DbSets
- `AppointmentConfiguration` — table `appointments`, indexes on patient_id, provider_id, scheduled_time
- `IAppointmentRepository` / `AppointmentRepository`
- `HttpClients/PatientServiceClient.cs` (~25 lines) — calls `GET /api/patients/{id}` to validate patient exists before scheduling. Uses `HttpClient` with base address from config.

**9.4 Controllers**

`AppointmentsController.cs`:
- `POST /api/appointments` — ScheduleAppointmentCommand
- `GET /api/appointments/{id}` — GetAppointmentQuery
- `GET /api/appointments` — ListAppointmentsQuery (filter by patientId, providerId)
- `PUT /api/appointments/{id}` — Update
- `DELETE /api/appointments/{id}` — CancelAppointmentCommand

`HealthController.cs` — same pattern as PatientService

**9.5 Config**

`appsettings.json`:
```json
{
  "ConnectionStrings": {
    "PostgreSQL": "Host=postgres;Database=hospital_db;Username=hospital;Password=dev_password_change_me"
  },
  "RabbitMQ": { "Host": "rabbitmq", "User": "guest", "Password": "guest" },
  "Services": {
    "PatientService": "http://patient-service:5001"
  },
  "Seq": { "Url": "http://seq:5341" },
  "Urls": "http://+:5002"
}
```

**9.6 Dockerfile** — same pattern, EXPOSE 5002, ASPNETCORE_URLS=http://+:5002

## Commands to Run

```bash
# PatientService
cd services && dotnet new webapi -n PatientService --framework net8.0 --no-https
cd PatientService && dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL --version 8.0.4
dotnet add package MediatR --version 12.2.0
dotnet add package MassTransit.RabbitMQ --version 8.1.3
dotnet add package FluentValidation.AspNetCore --version 11.3.0
dotnet add package Serilog.AspNetCore --version 8.0.1
dotnet add package Serilog.Sinks.Seq --version 6.0.0

# AppointmentService
cd services && dotnet new webapi -n AppointmentService --framework net8.0 --no-https
# (same packages as PatientService)

# Build verification
dotnet build services/PatientService/
dotnet build services/AppointmentService/
```

## Todo List

- [x] Create PatientService project with NuGet packages
- [x] Implement Patient entity and domain exceptions
- [x] Implement PatientDbContext and EF Core configuration
- [x] Implement IPatientRepository / PatientRepository
- [x] Implement CreatePatientCommand + Handler
- [x] Implement GetPatientQuery + Handler
- [x] Implement PatientsController with CRUD endpoints
- [x] Implement HealthController
- [x] Implement ExceptionHandlingMiddleware
- [x] Configure Program.cs (DI, middleware, EF, MassTransit)
- [x] Create PatientService Dockerfile
- [x] Create AppointmentService project (mirror structure)
- [x] Implement Appointment + TimeSlot entities
- [x] Implement AppointmentDbContext
- [x] Implement AppointmentsController
- [x] Implement PatientServiceClient (HTTP)
- [x] Create AppointmentService Dockerfile
- [x] Verify both services compile: `dotnet build`

## Success Criteria

- `dotnet build services/PatientService/` succeeds
- `dotnet build services/AppointmentService/` succeeds
- Both Dockerfiles build successfully
- PatientService starts on port 5001, `GET /health` returns 200
- AppointmentService starts on port 5002, `GET /health` returns 200
- Clean Architecture structure matches code-standards-dotnet.md

## Risk Assessment

| Risk | Impact | Mitigation |
|---|---|---|
| EF Core migration not created yet | Low | Migrations added in implementation, not scaffold |
| MassTransit connection fails at startup | Medium | Use `AddMassTransit` with retry config, graceful degradation |
| ProjectReference path issues in Docker | High | Dockerfile COPY from workspace root, not service dir |
| Shared library not built before services | Medium | Build order in docker-compose depends_on or multi-stage |

## Security Considerations

- No `[Authorize]` attribute in scaffold phase — added during implementation
- Connection strings use env vars in production (appsettings has dev defaults only)
- ExceptionHandlingMiddleware strips stack traces in non-Development environments
- Soft delete for patients (never hard delete medical data)
