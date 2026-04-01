# MedicalRecordService Exploration

## 1. API Routes/Endpoints
**Base Path:** `/api/medical-records`

| Method | Endpoint | Handler | Purpose |
|--------|----------|---------|---------|
| GET | `/health` | HealthController | Service health check |
| POST | `/` | RecordController.create | Create medical record |
| GET | `/:id` | RecordController.getById | Fetch single record by ID |
| GET | `/patient/:patientId` | RecordController.getByPatientId | Fetch all records for a patient (paginated) |
| PUT | `/:id` | RecordController.update | Update medical record |

**Pagination:** Supports `page` and `pageSize` query params (default: page=1, pageSize=50)

## 2. Database
**Type:** MongoDB (Mongoose ODM)

**Collection:** `MedicalRecord`

**Schema Fields:**
- `patientId` (String, required, indexed) - Patient identifier
- `appointmentId` (String, required) - Associated appointment
- `findings` (String, default: '') - Clinical findings
- `diagnosis` (Array<String>, default: []) - Diagnoses
- `labResults` (Array, default: []) - Lab test results
  - `testName` (String, required)
  - `result` (String, optional)
  - `normalRange` (String, optional)
  - `timestamp` (Date, auto-set to now)
- `createdBy` (String, optional) - Creator identifier
- `createdAt` (Date, auto-generated)
- `updatedAt` (Date, auto-generated)

## 3. Event Consumers (RabbitMQ)
**Status:** Consumer-only, no publishers

**Exchange:** `hospital.events`
**Queue:** `medical-record.appointment-scheduled`

**Subscribed Events:**
- `AppointmentScheduled` - Auto-creates empty medical record when appointment is scheduled
  - Reads: `patientId`, `appointmentId` from event
  - Action: Creates record with empty findings & diagnosis

## 4. Event Publishers
**None** - MedicalRecordService does not publish events

## 5. Key Business Logic
**RecordService Class:**
- `create(data)` - Instantiate and save new MedicalRecord
- `getByPatientId(patientId, page, pageSize)` - Query records by patient with pagination
- `getById(id)` - Fetch single record by MongoDB _id
- `update(id, data)` - Partial update using findByIdAndUpdate

**Key Behaviors:**
- Auto-indexing on patientId for query performance
- Automatic timestamps (createdAt, updatedAt)
- Pagination sorted by createdAt descending
- RabbitMQ consumer starts non-blocking at startup
- Graceful degradation if RabbitMQ unavailable at startup

## Configuration
- **Port:** 5003 (via PORT env var)
- **MongoDB:** mongodb://localhost:27017/hospital (via MONGODB_URI env var)
- **RabbitMQ:** amqp://guest:guest@localhost:5672 (configurable via RABBITMQ_USER, RABBITMQ_PASSWORD, RABBITMQ_HOST, RABBITMQ_PORT)
