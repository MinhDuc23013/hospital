# PharmacyService & MedicalRecordService Exploration Report

## Summary
Both services are Node.js/TypeScript microservices with different architectures:
- **PharmacyService**: SQL Server (MSSQL) + Kafka event streaming
- **MedicalRecordService**: MongoDB + RabbitMQ event consumers

---

## PharmacyService

### Tech Stack
- **Runtime**: Node.js + TypeScript
- **Framework**: Express.js
- **Database**: Microsoft SQL Server (MSSQL) via Sequelize ORM
- **Message Queue**: Kafka (for audit/trace events)
- **Server**: Runs on configurable port (default from config)

### Database
- **Type**: Relational (MSSQL)
- **ORM**: Sequelize v6.35.2
- **Driver**: tedious (MSSQL native driver)
- **Connection**: Configured via environment variables (host, port, username, password, database)
- **Features**: Timestamps enabled, underscored column names

### Endpoints & Routes

#### Drugs API (`/api/drugs`)
| Method | Endpoint | Controller Method | Description |
|--------|----------|-------------------|-------------|
| GET | `/` | `DrugController.list()` | List all drugs with pagination & search |
| GET | `/:id` | `DrugController.getById()` | Get single drug by ID |
| GET | `/low-stock` | `DrugController.getLowStock()` | Get drugs below minimum stock threshold |

#### Prescriptions API (`/api/prescriptions`)
| Method | Endpoint | Controller Method | Description |
|--------|----------|-------------------|-------------|
| POST | `/` | `PrescriptionController.create()` | Create new prescription |
| GET | `/:id` | `PrescriptionController.getById()` | Get prescription by ID |
| PUT | `/:id/dispense` | `PrescriptionController.dispense()` | Mark prescription as dispensed |

#### Health Check
| Method | Endpoint |
|--------|----------|
| GET | `/health` |

### Models

**Drug**
- `id` (int, PK, auto-increment)
- `code` (string, unique)
- `name` (string)
- `dosage` (string, optional)
- `unit` (string, optional)
- `price` (decimal, optional)
- `currentStock` (int, default: 0)
- `minimumStock` (int, default: 10)
- `supplier` (string, optional)
- `expirationDate` (date, optional)
- Timestamps: `createdAt`, `updatedAt`

**Prescription**
- `id` (UUID, PK, generated)
- `patientId` (UUID)
- `drugId` (int, FK to Drug)
- `quantity` (int)
- `instructions` (text, optional)
- `status` (ENUM: 'Pending', 'Dispensed', 'Expired', default: 'Pending')
- `issuedAt` (date, default: NOW)
- `validUntil` (date, optional)
- Timestamps: `createdAt`, `updatedAt`

### Key Features
- Inventory management with low-stock alerts
- Prescription lifecycle tracking (Pending → Dispensed)
- Kafka-based event publishing for audit trails
- Graceful Kafka connection handling (non-blocking)
- Error handling middleware

---

## MedicalRecordService

### Tech Stack
- **Runtime**: Node.js + TypeScript
- **Framework**: Express.js
- **Database**: MongoDB via Mongoose ODM
- **Message Queue**: RabbitMQ (for event consumers)
- **Server**: Runs on configurable port (default from config)

### Database
- **Type**: Document-based NoSQL (MongoDB)
- **ODM**: Mongoose v8.0.3
- **Connection**: Via MongoDB URI from config
- **Features**: Automatic timestamps, indexed patientId field

### Endpoints & Routes

#### Medical Records API (`/api/medical-records`)
| Method | Endpoint | Controller Method | Description |
|--------|----------|-------------------|-------------|
| GET | `/patient/:patientId` | `RecordController.getByPatientId()` | Get all records for a patient (paginated) |
| GET | `/:id` | `RecordController.getById()` | Get single record by ID |
| POST | `/` | `RecordController.create()` | Create new medical record |
| PUT | `/:id` | `RecordController.update()` | Update existing medical record |

#### Health Check
| Method | Endpoint |
|--------|----------|
| GET | `/health` |

### Models

**MedicalRecord**
- `_id` (MongoDB ObjectId, auto-generated)
- `patientId` (string, indexed)
- `appointmentId` (string)
- `findings` (string, default: '')
- `diagnosis` (array of strings, default: [])
- `labResults` (array of LabResult objects, default: [])
  - `testName` (string, required)
  - `result` (string, optional)
  - `normalRange` (string, optional)
  - `timestamp` (date, default: now)
- `createdBy` (string, optional)
- `createdAt` (date, auto)
- `updatedAt` (date, auto)

### Key Features
- Patient-centric record retrieval with indexing
- Lab results as nested documents
- RabbitMQ-based event consumers (non-blocking startup)
- Pagination support for large datasets
- Comprehensive CRUD operations
- Error handling middleware

---

## Integration Points

### Cross-Service Communication
- **MedicalRecordService** → **PharmacyService**: Records may reference prescriptions
- **PharmacyService** → **MedicalRecordService**: Prescription dispensing may trigger record updates
- Both services expose shared hospital utilities via `@hospital/shared` package

### Event-Driven Architecture
- **PharmacyService**: Publishes prescription events to Kafka
- **MedicalRecordService**: Consumes events from RabbitMQ (likely from other services)
- Async, non-blocking event handling in both services

---

## Shared Package
Both services depend on `@hospital/shared` (local NPM package):
- Logger factory: `createLogger(serviceName)`
- Shared utilities for cross-service concerns

---

## Comparison Matrix

| Aspect | PharmacyService | MedicalRecordService |
|--------|-----------------|----------------------|
| **Database** | MSSQL (Relational) | MongoDB (NoSQL) |
| **ORM/ODM** | Sequelize | Mongoose |
| **Message Queue** | Kafka (Publisher) | RabbitMQ (Consumer) |
| **Primary Domain** | Inventory & Prescriptions | Patient Medical Records |
| **Main Entities** | Drugs, Prescriptions | Medical Records, Lab Results |
| **Search Pattern** | Text search on drugs | Patient ID index queries |
| **Data Model** | Normalized, relational | Document-oriented, flexible |

