---
phase: 4
title: "Node.js Services Part 1 — MedicalRecordService + PharmacyService"
status: completed
priority: P1
effort: 3h
depends_on: [1]
blocks: [6]
completed: 2026-03-18
---

# Phase 4: Node.js Services Part 1

## Context Links
- [System Architecture](../../docs/system-architecture.md)
- [Code Standards Node.js](../../docs/code-standards-nodejs.md)
- [Phase 1 — Foundation](./phase-01-foundation.md)

<!-- Updated: Validation Session 1 - TypeScript (tsconfig, ts-node, nodemon); hospital-shared-js via file: protocol; Mongoose createIndexes() init -->

## Overview

Scaffold MedicalRecordService (Express + MongoDB/Mongoose) and PharmacyService (Express + SQL Server/Sequelize) in **TypeScript**. Both reference `hospital-shared-js` via `file:../../shared/hospital-shared-js`. Scaffold includes health endpoints, basic CRUD skeletons, model definitions, Mongoose `createIndexes()` on boot, RabbitMQ consumer stubs.

## File Ownership

```
services/
  MedicalRecordService/
    package.json
    Dockerfile
    .dockerignore
    .eslintrc.json
    jest.config.js
    src/
      app.js
      server.js
      config/
        index.js
      controllers/
        record-controller.js
        health-controller.js
      services/
        record-service.js
      models/
        medical-record.js
      routes/
        record-routes.js
        health-routes.js
      middleware/
        auth-middleware.js
        error-handler.js
      queue/
        rabbitmq-consumer.js
      __tests__/
        record-service.test.js

  PharmacyService/
    package.json
    Dockerfile
    .dockerignore
    .eslintrc.json
    jest.config.js
    src/
      app.js
      server.js
      config/
        index.js
      controllers/
        drug-controller.js
        prescription-controller.js
        health-controller.js
      services/
        drug-service.js
        prescription-service.js
      models/
        drug.js
        prescription.js
      routes/
        drug-routes.js
        prescription-routes.js
        health-routes.js
      middleware/
        auth-middleware.js
        error-handler.js
      queue/
        rabbitmq-publisher.js
      __tests__/
        drug-service.test.js
```

## Architecture

```
Express Router --> Controller --> Service --> Model --> Database
                                    |
                                    +--> RabbitMQ (publish/subscribe via @hospital/shared)
```

Layers per code-standards-nodejs.md:
- **Routes** — Express router definitions, middleware binding
- **Controllers** — Request/response handling, input extraction
- **Services** — Business logic, validation, orchestration
- **Models** — Mongoose schemas (MongoDB) or Sequelize models (SQL Server)
- **Queue** — RabbitMQ consumer/publisher using shared `rabbitmq-client.js`

## Implementation Steps

### MedicalRecordService

#### Step 1: Project Init

```bash
mkdir -p services/MedicalRecordService
cd services/MedicalRecordService
npm init -y
npm install express mongoose amqplib cors helmet dotenv
npm install --save-dev jest eslint nodemon
```

**1.1 `package.json` scripts:**
```json
{
  "name": "medical-record-service",
  "version": "1.0.0",
  "main": "src/server.js",
  "scripts": {
    "start": "node src/server.js",
    "dev": "nodemon src/server.js",
    "test": "jest --coverage",
    "lint": "eslint src/",
    "lint:fix": "eslint src/ --fix"
  },
  "dependencies": {
    "express": "^4.18.0",
    "mongoose": "^7.6.0",
    "amqplib": "^0.10.0",
    "cors": "^2.8.5",
    "helmet": "^7.1.0",
    "dotenv": "^16.3.0",
    "@hospital/shared": "file:../../shared/hospital-shared-js"
  }
}
```

Note: `@hospital/shared` linked via `file:` protocol — no npm registry needed.

#### Step 2: Config

**`src/config/index.js`** (~25 lines)

```javascript
require('dotenv').config();

module.exports = {
  port: process.env.PORT || 5003,
  mongoUri: process.env.MONGODB_URI || 'mongodb://mongo:27017/hospital',
  rabbitmq: {
    host: process.env.RABBITMQ_HOST || 'rabbitmq',
    port: process.env.RABBITMQ_PORT || 5672,
    user: process.env.RABBITMQ_USER || 'guest',
    password: process.env.RABBITMQ_PASSWORD || 'guest',
  },
  serviceName: 'medical-record-service',
};
```

#### Step 3: Mongoose Model

**`src/models/medical-record.js`** (~45 lines)

```javascript
const mongoose = require('mongoose');

const labResultSchema = new mongoose.Schema({
  testName: { type: String, required: true },
  result: { type: String },
  normalRange: { type: String },
  timestamp: { type: Date, default: Date.now },
});

const medicalRecordSchema = new mongoose.Schema({
  patientId: { type: String, required: true, index: true },
  appointmentId: { type: String, required: true },
  findings: { type: String, default: '' },
  diagnosis: { type: [String], default: [] },
  labResults: { type: [labResultSchema], default: [] },
  documents: [{
    filename: String,
    url: String,
    uploadedAt: { type: Date, default: Date.now },
  }],
  createdBy: { type: String },
}, { timestamps: true });

module.exports = mongoose.model('MedicalRecord', medicalRecordSchema);
```

#### Step 4: Service Layer

**`src/services/record-service.js`** (~60 lines)

```javascript
const MedicalRecord = require('../models/medical-record');
const { createLogger } = require('@hospital/shared');

class RecordService {
  constructor() {
    this._logger = createLogger('medical-record-service');
  }

  async create(data) {
    const record = new MedicalRecord(data);
    await record.save();
    this._logger.info('Medical record created', { recordId: record._id, patientId: data.patientId });
    return record;
  }

  async getByPatientId(patientId, page = 1, pageSize = 50) {
    const skip = (page - 1) * pageSize;
    const [items, total] = await Promise.all([
      MedicalRecord.find({ patientId }).skip(skip).limit(pageSize).sort({ createdAt: -1 }),
      MedicalRecord.countDocuments({ patientId }),
    ]);
    return { items, total, page, pageSize };
  }

  async getById(id) {
    return MedicalRecord.findById(id);
  }

  async update(id, data) {
    return MedicalRecord.findByIdAndUpdate(id, data, { new: true });
  }
}

module.exports = RecordService;
```

#### Step 5: Controller

**`src/controllers/record-controller.js`** (~50 lines)

```javascript
class RecordController {
  constructor(recordService) {
    this._recordService = recordService;
  }

  async create(req, res, next) {
    try {
      const record = await this._recordService.create(req.body);
      res.status(201).json({ data: record });
    } catch (err) { next(err); }
  }

  async getByPatientId(req, res, next) {
    try {
      const { patientId } = req.params;
      const { page, pageSize } = req.query;
      const result = await this._recordService.getByPatientId(patientId, +page || 1, +pageSize || 50);
      res.json({ data: result.items, pagination: { total: result.total, page: result.page, pageSize: result.pageSize } });
    } catch (err) { next(err); }
  }

  async getById(req, res, next) { ... }
  async update(req, res, next) { ... }
}

module.exports = RecordController;
```

**`src/controllers/health-controller.js`** (~10 lines)
- Returns `{ service: "medical-record-service", status: "healthy", timestamp: new Date() }`

#### Step 6: Routes

**`src/routes/record-routes.js`** (~20 lines)

```javascript
const express = require('express');
const router = express.Router();

module.exports = (recordController) => {
  router.get('/patient/:patientId', (req, res, next) => recordController.getByPatientId(req, res, next));
  router.get('/:id', (req, res, next) => recordController.getById(req, res, next));
  router.post('/', (req, res, next) => recordController.create(req, res, next));
  router.put('/:id', (req, res, next) => recordController.update(req, res, next));
  return router;
};
```

**`src/routes/health-routes.js`** (~8 lines) — `GET /health`

#### Step 7: RabbitMQ Consumer Stub

**`src/queue/rabbitmq-consumer.js`** (~35 lines)

```javascript
const { connectRabbitMQ, subscribe } = require('@hospital/shared');
const { EVENT_NAMES } = require('@hospital/shared');

async function startConsumers(recordService) {
  const channel = await connectRabbitMQ();

  // Subscribe to AppointmentScheduledEvent
  await subscribe(channel, EVENT_NAMES.APPOINTMENT_SCHEDULED, async (msg) => {
    const { patientId, appointmentId } = msg;
    await recordService.create({ patientId, appointmentId, findings: '', diagnosis: [] });
  });

  // Subscribe to PrescriptionIssuedEvent
  await subscribe(channel, EVENT_NAMES.PRESCRIPTION_ISSUED, async (msg) => {
    // Stub: append prescription info to record
  });
}

module.exports = { startConsumers };
```

#### Step 8: App & Server

**`src/app.js`** (~30 lines)

```javascript
const express = require('express');
const cors = require('cors');
const helmet = require('helmet');
const { errorHandler } = require('@hospital/shared');

const app = express();
app.use(helmet());
app.use(cors());
app.use(express.json());

// Routes injected in server.js after DI setup
module.exports = app;
```

**`src/server.js`** (~35 lines)

```javascript
require('dotenv').config();
const mongoose = require('mongoose');
const config = require('./config');
const app = require('./app');
const RecordService = require('./services/record-service');
const RecordController = require('./controllers/record-controller');
const recordRoutes = require('./routes/record-routes');
const healthRoutes = require('./routes/health-routes');
const { startConsumers } = require('./queue/rabbitmq-consumer');
const { errorHandler } = require('@hospital/shared');

async function start() {
  await mongoose.connect(config.mongoUri);
  console.log('Connected to MongoDB');

  const recordService = new RecordService();
  const recordController = new RecordController(recordService);

  app.use('/api/medical-records', recordRoutes(recordController));
  app.use('/', healthRoutes);
  app.use(errorHandler);

  await startConsumers(recordService).catch(err => console.error('RabbitMQ not ready, will retry', err));

  app.listen(config.port, () => console.log(`MedicalRecordService listening on port ${config.port}`));
}

start().catch(console.error);
```

#### Step 9: Middleware Stubs

**`src/middleware/auth-middleware.js`** (~15 lines) — Stub that extracts JWT from Authorization header (no validation in scaffold)

**`src/middleware/error-handler.js`** — Re-export from `@hospital/shared`

#### Step 10: Dockerfile

```dockerfile
FROM node:20-alpine AS base
WORKDIR /app
EXPOSE 5003

COPY shared/hospital-shared-js/ /shared/hospital-shared-js/
COPY services/MedicalRecordService/package*.json ./
RUN npm install --production
COPY services/MedicalRecordService/ .

ENV PORT=5003
CMD ["node", "src/server.js"]
```

#### Step 11: Test Stub

**`src/__tests__/record-service.test.js`** (~20 lines)

```javascript
describe('RecordService', () => {
  it('should be defined', () => {
    const RecordService = require('../services/record-service');
    expect(RecordService).toBeDefined();
  });
});
```

---

### PharmacyService

#### Step 12: Project Init

```bash
mkdir -p services/PharmacyService
cd services/PharmacyService
npm init -y
npm install express sequelize tedious amqplib cors helmet dotenv
npm install --save-dev jest eslint nodemon
```

- Uses `sequelize` + `tedious` driver for SQL Server

#### Step 13: Sequelize Models

**`src/models/drug.js`** (~35 lines)

```javascript
const { DataTypes } = require('sequelize');

module.exports = (sequelize) => {
  return sequelize.define('Drug', {
    id: { type: DataTypes.INTEGER, primaryKey: true, autoIncrement: true },
    code: { type: DataTypes.STRING(50), unique: true, allowNull: false },
    name: { type: DataTypes.STRING(200), allowNull: false },
    dosage: { type: DataTypes.STRING(100) },
    unit: { type: DataTypes.STRING(50) },
    price: { type: DataTypes.DECIMAL(10, 2) },
    currentStock: { type: DataTypes.INTEGER, defaultValue: 0 },
    minimumStock: { type: DataTypes.INTEGER, defaultValue: 10 },
    supplier: { type: DataTypes.STRING(200) },
    expirationDate: { type: DataTypes.DATE },
  }, { tableName: 'drugs', timestamps: true, underscored: true });
};
```

**`src/models/prescription.js`** (~30 lines)

```javascript
module.exports = (sequelize) => {
  return sequelize.define('Prescription', {
    id: { type: DataTypes.UUID, primaryKey: true, defaultValue: DataTypes.UUIDV4 },
    patientId: { type: DataTypes.UUID, allowNull: false },
    drugId: { type: DataTypes.INTEGER, allowNull: false },
    quantity: { type: DataTypes.INTEGER, allowNull: false },
    instructions: { type: DataTypes.TEXT },
    status: { type: DataTypes.ENUM('Pending', 'Dispensed', 'Expired'), defaultValue: 'Pending' },
    issuedAt: { type: DataTypes.DATE, defaultValue: DataTypes.NOW },
    validUntil: { type: DataTypes.DATE },
  }, { tableName: 'prescriptions', timestamps: true, underscored: true });
};
```

#### Step 14: Config

**`src/config/index.js`** (~30 lines)

```javascript
module.exports = {
  port: process.env.PORT || 5004,
  database: {
    host: process.env.MSSQL_SERVER || 'sqlserver',
    port: parseInt(process.env.MSSQL_PORT || '1433'),
    username: process.env.MSSQL_USER || 'sa',
    password: process.env.MSSQL_SA_PASSWORD || 'YourStrong!Passw0rd',
    database: process.env.MSSQL_DB || 'hospital_pharmacy',
    dialect: 'mssql',
  },
  rabbitmq: { ... },
  serviceName: 'pharmacy-service',
};
```

#### Step 15: Services

**`src/services/drug-service.js`** (~45 lines)
- `list(page, pageSize, search)` — paginated, optional name search
- `getById(id)`
- `getLowStock()` — where currentStock < minimumStock

**`src/services/prescription-service.js`** (~50 lines)
- `create(data)` — creates prescription, publishes PrescriptionIssuedEvent, checks stock levels
- `getById(id)`
- `dispense(id)` — marks as Dispensed, decrements drug stock

#### Step 16: Controllers

**`src/controllers/drug-controller.js`** (~30 lines) — list, getById, getLowStock
**`src/controllers/prescription-controller.js`** (~30 lines) — create, getById, dispense
**`src/controllers/health-controller.js`** (~10 lines)

#### Step 17: Routes

**`src/routes/drug-routes.js`** (~12 lines)
- `GET /api/drugs` — list
- `GET /api/drugs/:id` — getById
- `GET /api/inventory/low-stock` — getLowStock

**`src/routes/prescription-routes.js`** (~12 lines)
- `POST /api/prescriptions` — create
- `GET /api/prescriptions/:id` — getById
- `PUT /api/prescriptions/:id/dispense` — dispense

#### Step 18: RabbitMQ Publisher

**`src/queue/rabbitmq-publisher.js`** (~25 lines)
- Publishes `PrescriptionIssuedEvent` and `InventoryLowEvent` using shared client

#### Step 19: App, Server, Dockerfile

Same patterns as MedicalRecordService:
- `src/app.js` — Express setup
- `src/server.js` — Sequelize connect + sync, DI, start consumers, listen on 5004
- `Dockerfile` — node:20-alpine, EXPOSE 5004

Sequelize init in server.js:
```javascript
const { Sequelize } = require('sequelize');
const config = require('./config');

const sequelize = new Sequelize(config.database.database, config.database.username, config.database.password, {
  host: config.database.host,
  port: config.database.port,
  dialect: 'mssql',
  dialectOptions: { options: { encrypt: false, trustServerCertificate: true } },
});

await sequelize.authenticate();
await sequelize.sync({ alter: true }); // dev only
```

## Commands to Run

```bash
# MedicalRecordService
cd services/MedicalRecordService
npm install
npm test
npm run lint

# PharmacyService
cd services/PharmacyService
npm install
npm test
npm run lint
```

## Todo List

- [x] Create MedicalRecordService package.json + deps
- [x] Implement config/index.js
- [x] Implement medical-record Mongoose model
- [x] Implement record-service.js
- [x] Implement record-controller.js + health-controller.js
- [x] Implement routes (record-routes, health-routes)
- [x] Implement rabbitmq-consumer.js stub
- [x] Implement app.js + server.js
- [x] Create Dockerfile + .dockerignore
- [x] Create .eslintrc.json + jest.config.js
- [x] Create test stub
- [x] Create PharmacyService package.json + deps
- [x] Implement Drug + Prescription Sequelize models
- [x] Implement drug-service.js + prescription-service.js
- [x] Implement controllers
- [x] Implement routes
- [x] Implement rabbitmq-publisher.js
- [x] Create PharmacyService Dockerfile
- [x] Verify `node -e "require('./src/app')"` works for both

## Success Criteria

- Both `npm install` complete without errors
- `npm test` passes for both services
- `npm run lint` passes (or only warnings)
- Dockerfiles build successfully
- MedicalRecordService starts on port 5003, `GET /health` returns 200
- PharmacyService starts on port 5004, `GET /health` returns 200
- Mongoose model validates correctly
- Sequelize models define proper columns and types

## Risk Assessment

| Risk | Impact | Mitigation |
|---|---|---|
| SQL Server tedious driver compatibility | Medium | Pin tedious version, test connection early |
| `file:` protocol for shared lib breaks in Docker | High | COPY shared lib first in Dockerfile, adjust path |
| Mongoose connection timeout in Docker | Low | Add retry logic in server.js startup |
| Sequelize sync alter drops data | Low | Only use `alter: true` in dev; migrations in prod |
