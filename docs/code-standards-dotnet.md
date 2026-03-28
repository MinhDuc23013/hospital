# Hospital HRM Microservices — Code Standards & Guidelines

This document defines coding conventions, architectural patterns, and quality standards for all services in the Hospital HRM microservices platform.

---

## General Principles

1. **Readability over cleverness** — Code is read 10x more than written
2. **Explicit over implicit** — Clear intent reduces bugs and maintenance burden
3. **YAGNI** — Don't add features until they're needed
4. **KISS** — Keep solutions simple and straightforward
5. **DRY** — Don't repeat yourself; extract to shared libraries
6. **Fail fast** — Validate input early, throw meaningful errors
7. **Security first** — Encryption, validation, and access control are non-negotiable

---

## .NET 8 Standards

### File Organization

```
Service/
├── Controllers/            # HTTP entry points
├── Application/
│   ├── Commands/          # Write operations (CQS pattern)
│   ├── Queries/           # Read operations
│   ├── DTOs/              # Data Transfer Objects
│   └── Handlers/          # Command/Query handlers
├── Domain/
│   ├── Entities/          # Business logic
│   ├── Events/            # Domain events
│   ├── Exceptions/        # Custom exceptions
│   └── ValueObjects/      # Immutable value types
├── Infrastructure/
│   ├── Persistence/       # Database context, migrations
│   ├── MessageBus/        # RabbitMQ, MassTransit setup
│   ├── Services/          # External service clients
│   └── Repositories/      # Data access (if needed)
├── Middleware/            # Custom pipeline middleware
├── Extensions/            # Dependency injection, configuration
├── appsettings.json       # Configuration (commit)
├── appsettings.Development.json  # Dev config (not in git)
├── Dockerfile
├── Program.cs             # Startup, DI configuration
└── ServiceName.csproj
```

### Naming Conventions

| Element | Convention | Example |
|---|---|---|
| Namespaces | PascalCase | `HospitalShared.DTOs` |
| Classes, Records | PascalCase | `PatientService`, `CreatePatientCommand` |
| Interfaces | PascalCase, I prefix | `IPatientRepository`, `IMessagePublisher` |
| Methods | PascalCase | `CreatePatient()`, `GetPatientById()` |
| Properties | PascalCase | `PatientId`, `FirstName` |
| Private fields | camelCase, _ prefix | `_patientRepository`, `_logger` |
| Constants | UPPER_SNAKE_CASE | `DEFAULT_PAGE_SIZE = 50` |
| Async methods | PascalCase + Async | `CreatePatientAsync()`, `GetPatientsAsync()` |

### Code Organization

**Class Structure:**
```csharp
public class PatientService
{
    // Fields (private, readonly)
    private readonly IPatientRepository _patientRepository;
    private readonly ILogger<PatientService> _logger;

    // Constructor
    public PatientService(IPatientRepository patientRepository, ILogger<PatientService> logger)
    {
        _patientRepository = patientRepository ?? throw new ArgumentNullException(nameof(patientRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    // Public methods
    public async Task<PatientDto> CreatePatientAsync(CreatePatientCommand command)
    {
        // Validation
        if (string.IsNullOrWhiteSpace(command.Email))
            throw new ArgumentException("Email is required", nameof(command.Email));

        // Business logic
        var patient = Patient.Create(command);

        // Persist
        await _patientRepository.AddAsync(patient);
        await _patientRepository.SaveChangesAsync();

        _logger.LogInformation("Patient created: {PatientId}", patient.Id);

        return PatientMapper.ToDto(patient);
    }

    // Private helper methods
    private bool IsValidEmail(string email) => email.Contains("@");
}
```

### Architecture Patterns

**Clean Architecture Layers:**
```
Controllers (API)
    ↓ (depend on)
Application Services (CQRS)
    ↓ (depend on)
Domain (Entities, Rules)
    ↓ (depend on)
Infrastructure (DB, Messaging)
```

**Controller Example:**
```csharp
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PatientsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<PatientsController> _logger;

    public PatientsController(IMediator mediator, ILogger<PatientsController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    [HttpPost]
    [ProduceResponseType(typeof(PatientDto), StatusCodes.Status201Created)]
    [ProduceResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PatientDto>> CreatePatient([FromBody] CreatePatientCommand command)
    {
        var result = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetPatient), new { id = result.Id }, result);
    }

    [HttpGet("{id}")]
    [ProduceResponseType(typeof(PatientDto), StatusCodes.Status200OK)]
    [ProduceResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PatientDto>> GetPatient(Guid id)
    {
        var query = new GetPatientQuery { Id = id };
        var result = await _mediator.Send(query);

        if (result == null)
            return NotFound();

        return Ok(result);
    }
}
```

**CQRS Handler Example:**
```csharp
public class CreatePatientCommandHandler : IRequestHandler<CreatePatientCommand, PatientDto>
{
    private readonly IPatientRepository _repository;
    private readonly IPublishEndpoint _publishEndpoint;

    public CreatePatientCommandHandler(IPatientRepository repository, IPublishEndpoint publishEndpoint)
    {
        _repository = repository;
        _publishEndpoint = publishEndpoint;
    }

    public async Task<PatientDto> Handle(CreatePatientCommand request, CancellationToken cancellationToken)
    {
        var patient = Patient.Create(request.FirstName, request.LastName, request.Email);
        await _repository.AddAsync(patient, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        // Publish domain event
        await _publishEndpoint.Publish(new PatientCreatedEvent
        {
            PatientId = patient.Id,
            Email = patient.Email
        }, cancellationToken);

        return PatientMapper.ToDto(patient);
    }
}
```

### Logging Standards

```csharp
// Structured logging with correlation IDs
_logger.LogInformation("Patient created: {PatientId}, Email: {Email}", patient.Id, patient.Email);

// Error logging with exception
_logger.LogError(ex, "Failed to create patient: {Email}", command.Email);

// Levels: Trace, Debug, Information, Warning, Error, Critical
```

### Error Handling

```csharp
public class PatientService
{
    public async Task<PatientDto> GetPatientAsync(Guid id)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Patient ID is required", nameof(id));

        var patient = await _patientRepository.GetByIdAsync(id);
        if (patient == null)
            throw new DomainException($"Patient {id} not found");

        return PatientMapper.ToDto(patient);
    }
}

// Custom exception
public class DomainException : Exception
{
    public DomainException(string message) : base(message) { }
}
```

### Testing Conventions

```csharp
// Arrange-Act-Assert (AAA)
[Fact]
public async Task CreatePatient_WithValidEmail_ReturnsPatientDto()
{
    // Arrange
    var command = new CreatePatientCommand { Email = "test@hospital.com", FirstName = "John" };
    var handler = new CreatePatientCommandHandler(_mockRepository.Object);

    // Act
    var result = await handler.Handle(command, CancellationToken.None);

    // Assert
    Assert.NotNull(result);
    Assert.Equal("test@hospital.com", result.Email);
    _mockRepository.Verify(x => x.AddAsync(It.IsAny<Patient>()), Times.Once);
}

[Fact]
public async Task CreatePatient_WithNullEmail_ThrowsArgumentException()
{
    // Arrange
    var command = new CreatePatientCommand { Email = null };
    var handler = new CreatePatientCommandHandler(_mockRepository.Object);

    // Act & Assert
    await Assert.ThrowsAsync<ArgumentException>(() => handler.Handle(command, CancellationToken.None));
}
```

---

