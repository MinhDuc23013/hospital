# Hospital HRM Microservices — Code Standards (Node.js)

## Node.js Standards

### File Organization

```
src/
├── controllers/          # Request handlers
├── routes/              # Express routes
├── services/            # Business logic
├── models/              # Database schemas (Mongoose, Sequelize)
├── middleware/          # Custom middleware
├── utils/               # Shared utilities
├── config/              # Configuration
├── queue/               # RabbitMQ handlers (amqplib)
├── validators/          # Input validation
├── logger.js            # Winston configuration
├── app.js               # Express app setup
├── server.js            # Server startup
└── errors/              # Custom error classes
```

### Naming Conventions

| Element | Convention | Example |
|---|---|---|
| Files | kebab-case | `patient-controller.js`, `rabbitmq-client.js` |
| Classes/Constructors | PascalCase | `class PatientService {}` |
| Functions | camelCase | `getPatient()`, `createPatientAsync()` |
| Variables | camelCase | `const patientId = 123`, `let count = 0` |
| Constants | UPPER_SNAKE_CASE | `const DEFAULT_PAGE_SIZE = 50` |
| Private fields | camelCase, _ prefix | `this._patientRepository` |
| Async functions | camelCase (no Async suffix) | `async getPatient()` |

### Code Style

**Function Example:**
```javascript
// patient-service.js
const logger = require('../logger');

class PatientService {
  constructor(patientRepository, messagePublisher) {
    this._patientRepository = patientRepository;
    this._messagePublisher = messagePublisher;
  }

  async createPatient(firstName, lastName, email) {
    // Validation
    if (!email || !email.includes('@')) {
      throw new Error('Valid email is required');
    }

    // Business logic
    const patient = {
      id: generateUuid(),
      firstName,
      lastName,
      email,
      createdAt: new Date()
    };

    // Persist
    await this._patientRepository.add(patient);
    logger.info('Patient created', { patientId: patient.id, email });

    // Publish event
    await this._messagePublisher.publish('PatientCreated', {
      patientId: patient.id,
      email
    });

    return patient;
  }

  async getPatientById(patientId) {
    if (!patientId) {
      throw new Error('Patient ID is required');
    }

    const patient = await this._patientRepository.getById(patientId);
    if (!patient) {
      throw new Error(`Patient ${patientId} not found`);
    }

    return patient;
  }
}

module.exports = PatientService;
```

**Route Example:**
```javascript
// routes/patient-routes.js
const express = require('express');
const router = express.Router();
const PatientController = require('../controllers/patient-controller');
const { authMiddleware } = require('../middleware/auth-middleware');
const { validatePatientInput } = require('../validators/patient-validator');

router.use(authMiddleware); // Protect all routes

router.post('/', validatePatientInput, (req, res, next) => {
  PatientController.create(req, res).catch(next);
});

router.get('/:id', (req, res, next) => {
  PatientController.getById(req, res).catch(next);
});

router.put('/:id', validatePatientInput, (req, res, next) => {
  PatientController.update(req, res).catch(next);
});

module.exports = router;
```

**Controller Example:**
```javascript
// controllers/patient-controller.js
const PatientService = require('../services/patient-service');

class PatientController {
  constructor(patientService) {
    this._patientService = patientService;
  }

  async create(req, res) {
    const { firstName, lastName, email } = req.body;
    const patient = await this._patientService.createPatient(firstName, lastName, email);
    res.status(201).json(patient);
  }

  async getById(req, res) {
    const patient = await this._patientService.getPatientById(req.params.id);
    res.status(200).json(patient);
  }
}

module.exports = PatientController;
```

### Middleware & Error Handling

```javascript
// middleware/error-handler.js
const logger = require('../logger');

function errorHandler(err, req, res, next) {
  const statusCode = err.statusCode || 500;
  const message = err.message || 'Internal Server Error';

  logger.error('Request error', {
    statusCode,
    message,
    path: req.path,
    method: req.method,
    stack: err.stack
  });

  res.status(statusCode).json({
    error: {
      message,
      status: statusCode,
      timestamp: new Date().toISOString()
    }
  });
}

module.exports = errorHandler;

// app.js
const express = require('express');
const errorHandler = require('./middleware/error-handler');
const app = express();

app.use(express.json());
app.use('/api/patients', require('./routes/patient-routes'));

app.use(errorHandler);

module.exports = app;
```

### Logging Standards

```javascript
// logger.js (Winston)
const winston = require('winston');

const logger = winston.createLogger({
  level: process.env.LOG_LEVEL || 'info',
  format: winston.format.combine(
    winston.format.timestamp(),
    winston.format.errors({ stack: true }),
    winston.format.splat(),
    winston.format.json()
  ),
  defaultMeta: { service: 'patient-service' },
  transports: [
    new winston.transports.Console(),
    new winston.transports.File({ filename: 'logs/error.log', level: 'error' }),
    new winston.transports.File({ filename: 'logs/combined.log' })
  ]
});

module.exports = logger;

// Usage
const logger = require('../logger');
logger.info('Patient created', { patientId: patient.id });
logger.error('Database error', { error: err.message });
```

### Testing Conventions

```javascript
// __tests__/services/patient-service.test.js (Jest)
const PatientService = require('../../src/services/patient-service');

describe('PatientService', () => {
  let patientService;
  let mockRepository;

  beforeEach(() => {
    mockRepository = {
      add: jest.fn().mockResolvedValue({}),
      getById: jest.fn()
    };
    patientService = new PatientService(mockRepository);
  });

  describe('createPatient', () => {
    it('should create patient with valid email', async () => {
      // Arrange
      const firstName = 'John';
      const lastName = 'Doe';
      const email = 'john@hospital.com';

      // Act
      const result = await patientService.createPatient(firstName, lastName, email);

      // Assert
      expect(result).toHaveProperty('id');
      expect(result.email).toBe(email);
      expect(mockRepository.add).toHaveBeenCalled();
    });

    it('should throw error with invalid email', async () => {
      // Act & Assert
      await expect(patientService.createPatient('John', 'Doe', 'invalid-email'))
        .rejects.toThrow('Valid email is required');
    });
  });
});
```

---

