# Hospital HRM Microservices — Code Standards

Language-specific standards split into sub-files:
- [.NET 8 Standards](./code-standards-dotnet.md)
- [Node.js Standards](./code-standards-nodejs.md)

---

## Database Standards

### PostgreSQL & SQL Server

**Naming Conventions:**
```sql
-- Tables: plural, snake_case
CREATE TABLE patients (
  id UUID PRIMARY KEY,
  email VARCHAR(255) NOT NULL UNIQUE,
  first_name VARCHAR(100) NOT NULL,
  last_name VARCHAR(100) NOT NULL,
  created_at TIMESTAMP DEFAULT NOW(),
  updated_at TIMESTAMP DEFAULT NOW()
);

-- Indexes on foreign keys and frequently queried columns
CREATE INDEX idx_patients_email ON patients(email);
CREATE INDEX idx_appointments_patient_id ON appointments(patient_id);
```

**Entity Framework Mapping (.NET):**
```csharp
modelBuilder.Entity<Patient>(entity =>
{
    entity.ToTable("patients");
    entity.HasKey(e => e.Id);
    entity.Property(e => e.Email).IsRequired().HasMaxLength(255);
    entity.Property(e => e.FirstName).IsRequired().HasMaxLength(100);
    entity.HasIndex(e => e.Email).IsUnique();
    entity.Property(e => e.CreatedAt).HasDefaultValueSql("NOW()");
});
```

### MongoDB

**Naming Conventions:**
```javascript
// Collections: plural, camelCase
db.createCollection('medicalRecords');

// Sample document
{
  _id: ObjectId,
  patientId: UUID,
  appointmentId: UUID,
  findings: "string",
  diagnosis: ["string"],
  labResults: [{
    testName: "string",
    result: "string",
    normalRange: "string"
  }],
  createdAt: Date,
  updatedAt: Date
}

// Indexes
db.medicalRecords.createIndex({ patientId: 1 });
db.medicalRecords.createIndex({ appointmentId: 1 });
```

**Mongoose Schema (.js):**
```javascript
const recordSchema = new mongoose.Schema({
  patientId: { type: String, required: true, index: true },
  appointmentId: { type: String, required: true },
  findings: { type: String },
  diagnosis: { type: [String] },
  labResults: [{
    testName: String,
    result: String,
    normalRange: String
  }],
  createdAt: { type: Date, default: Date.now },
  updatedAt: { type: Date, default: Date.now }
});
```

---

## API Standards

### RESTful Conventions

```
GET    /api/patients              # List all patients
POST   /api/patients              # Create patient
GET    /api/patients/{id}         # Get patient by ID
PUT    /api/patients/{id}         # Update patient
DELETE /api/patients/{id}         # Delete patient

GET    /api/patients/{id}/appointments  # Get patient's appointments
GET    /api/search?q=john          # Search patients
```

### Response Format

```json
// Success (200, 201)
{
  "data": {
    "id": "uuid",
    "firstName": "John",
    "lastName": "Doe",
    "email": "john@hospital.com"
  },
  "timestamp": "2026-03-18T10:30:00Z"
}

// Error (4xx, 5xx)
{
  "error": {
    "message": "Patient not found",
    "status": 404,
    "timestamp": "2026-03-18T10:30:00Z",
    "correlationId": "abc-123-def"
  }
}

// Paginated (200)
{
  "data": [...],
  "pagination": {
    "total": 1000,
    "page": 1,
    "pageSize": 50,
    "totalPages": 20
  }
}
```

### Query Parameters

```
// Pagination
GET /api/patients?page=1&pageSize=50

// Filtering
GET /api/appointments?patientId={id}&status=scheduled

// Sorting
GET /api/patients?sort=lastName,-createdAt

// Searching
GET /api/search?q=john&type=patient
```

---

## Security Standards

### Authentication & Authorization

```csharp
// .NET: Use [Authorize] attribute
[ApiController]
[Authorize]
public class PatientsController : ControllerBase { ... }

// Check specific claims if needed
var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
```

```javascript
// Node.js: JWT middleware
const authMiddleware = (req, res, next) => {
  const token = req.headers.authorization?.split(' ')[1];
  if (!token) return res.status(401).json({ error: 'No token' });

  try {
    const decoded = jwt.verify(token, process.env.JWT_SECRET);
    req.user = decoded;
    next();
  } catch {
    res.status(401).json({ error: 'Invalid token' });
  }
};
```

### Input Validation

```csharp
// .NET: Use FluentValidation
public class CreatePatientValidator : AbstractValidator<CreatePatientCommand>
{
    public CreatePatientValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
    }
}
```

```javascript
// Node.js: Use joi or class-validator
const schema = joi.object({
  email: joi.string().email().required(),
  firstName: joi.string().max(100).required()
});

const { error, value } = schema.validate(data);
```

### Secrets Management

- **Never commit secrets to git** (use `.env` with `.env.example` template)
- Store secrets in environment variables or secrets management tool
- Rotate credentials regularly
- Use HTTPS/TLS for all inter-service communication

---

## Performance Standards

| Metric | Target |
|---|---|
| **API Response Time (p95)** | <500ms for read, <1000ms for write |
| **Database Query Time** | <100ms for single queries |
| **Memory Usage per Service** | <500MB (steady state) |
| **CPU Usage per Service** | <50% under normal load |

### Optimization Techniques

- Add indexes on frequently queried columns
- Cache read-heavy operations (Redis)
- Implement pagination (default: 50 items/page)
- Use async operations to avoid blocking
- Monitor slow queries and optimize
- Use connection pooling for databases

---

## Testing Standards

### Coverage Targets

| Layer | Target |
|---|---|
| **Unit Tests** | >80% coverage |
| **Integration Tests** | >60% coverage |
| **E2E Tests** | >40% coverage |

### Test Naming

```csharp
// MethodName_Scenario_ExpectedResult
[Fact]
public async Task CreatePatient_WithValidEmail_ReturnsPatientDto() { }

[Fact]
public async Task CreatePatient_WithInvalidEmail_ThrowsArgumentException() { }
```

```javascript
describe('PatientService', () => {
  describe('createPatient', () => {
    it('should create patient with valid email', async () => {});
    it('should throw error with invalid email', async () => {});
  });
});
```

---

## Documentation Standards

### Code Comments

```csharp
// Use XML comments for public APIs
/// <summary>
/// Creates a new patient record.
/// </summary>
/// <param name="command">The create patient command with patient details.</param>
/// <returns>The created patient DTO.</returns>
/// <exception cref="ArgumentException">Thrown when email is invalid.</exception>
public async Task<PatientDto> CreatePatientAsync(CreatePatientCommand command) { }

// Avoid obvious comments
// ❌ DON'T: Set the patient name
patientName = "John";

// ✅ DO: Explain WHY, not WHAT
// Use patient's full name instead of abbreviated version per hospital policy
patientName = $"{firstName} {lastName}";
```

### README Requirements

Each service must have a README with:
- Service purpose and responsibilities
- API endpoints (list main routes)
- Database setup instructions
- Environment variables required
- Running locally (docker-compose)
- Testing instructions
- Contributing guidelines

---

## Git Standards

### Commit Messages (Conventional Commits)

```
feat: Add patient search by email
fix: Correct appointment cancellation bug
docs: Update architecture documentation
refactor: Extract validation logic to shared library
test: Add unit tests for PatientService
chore: Update dependencies
```

### Branch Naming

```
feature/patient-service-crud
bugfix/appointment-cancellation
docs/architecture-update
```

### Code Review Checklist

- [ ] Code follows standards (naming, structure, comments)
- [ ] Tests included and passing (>80% coverage)
- [ ] No hardcoded secrets or credentials
- [ ] Documentation updated
- [ ] No breaking changes without migration path
- [ ] Performance impact assessed
- [ ] Security review completed (if auth/data handling)

---

## Tools & Linting

### .NET

```bash
# Format code
dotnet format

# Code analysis
dotnet build /p:EnforceCodeStyleInBuild=true
```

**EditorConfig (.editorconfig):**
```
root = true

[*.cs]
csharp_indent_case_contents = true
csharp_indent_switch_labels = true
csharp_space_after_keywords_in_control_flow_statements = true
```

### Node.js

```bash
# ESLint
npm run lint
npm run lint:fix

# Prettier (formatting)
npm run format
npm run format:check
```

**.eslintrc.json:**
```json
{
  "env": { "node": true, "es2021": true },
  "extends": "eslint:recommended",
  "rules": {
    "no-console": "warn",
    "no-unused-vars": "warn"
  }
}
```

---

## Checklist: Before Submitting PR

- [ ] Code follows naming conventions
- [ ] No console.log or commented code
- [ ] All functions have clear purpose
- [ ] Error handling is present and meaningful
- [ ] Unit/integration tests pass
- [ ] Test coverage >80%
- [ ] No secrets in commit
- [ ] Documentation updated
- [ ] Code formatted and linted
- [ ] Commit messages are clear and descriptive

