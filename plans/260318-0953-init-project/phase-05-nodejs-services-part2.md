---
phase: 5
title: "Node.js Services Part 2 — NotificationService + SearchService"
status: completed
priority: P1
effort: 2h
depends_on: [1]
blocks: [6]
completed: 2026-03-18
---

# Phase 5: Node.js Services Part 2

## Context Links
- [System Architecture](../../docs/system-architecture.md)
- [Code Standards Node.js](../../docs/code-standards-nodejs.md)
- [Phase 1 — Foundation](./phase-01-foundation.md)

<!-- Updated: Validation Session 1 - TypeScript (tsconfig, ts-node, nodemon); hospital-shared-js via file: protocol -->

## Overview

Scaffold NotificationService (Express + Redis pub/sub) and SearchService (Express + Elasticsearch) in **TypeScript**. Both reference `hospital-shared-js` via `file:../../shared/hospital-shared-js`. Scaffold includes health endpoints, basic API skeletons, queue consumer stubs, and client wrappers.

## File Ownership

```
services/
  NotificationService/
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
        notification-controller.js
        health-controller.js
      services/
        notification-service.js
        email-service.js
        sms-service.js
      models/
        notification-template.js
        notification-log.js
      routes/
        notification-routes.js
        health-routes.js
      middleware/
        error-handler.js
      queue/
        rabbitmq-consumer.js
      __tests__/
        notification-service.test.js

  SearchService/
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
        search-controller.js
        health-controller.js
      services/
        search-service.js
        indexer-service.js
      routes/
        search-routes.js
        health-routes.js
      middleware/
        error-handler.js
      queue/
        rabbitmq-consumer.js
      __tests__/
        search-service.test.js
```

## Implementation Steps

### NotificationService

#### Step 1: Project Init

```bash
mkdir -p services/NotificationService
cd services/NotificationService
npm init -y
npm install express ioredis amqplib cors helmet dotenv nodemailer
npm install --save-dev jest eslint nodemon
```

**`package.json` dependencies:**
```json
{
  "name": "notification-service",
  "version": "1.0.0",
  "main": "src/server.js",
  "scripts": {
    "start": "node src/server.js",
    "dev": "nodemon src/server.js",
    "test": "jest --coverage",
    "lint": "eslint src/"
  },
  "dependencies": {
    "express": "^4.18.0",
    "ioredis": "^5.3.0",
    "amqplib": "^0.10.0",
    "nodemailer": "^6.9.0",
    "cors": "^2.8.5",
    "helmet": "^7.1.0",
    "dotenv": "^16.3.0",
    "@hospital/shared": "file:../../shared/hospital-shared-js"
  }
}
```

#### Step 2: Config

**`src/config/index.js`** (~30 lines)

```javascript
module.exports = {
  port: process.env.PORT || 5005,
  redis: {
    host: process.env.REDIS_HOST || 'redis',
    port: parseInt(process.env.REDIS_PORT || '6379'),
  },
  rabbitmq: {
    host: process.env.RABBITMQ_HOST || 'rabbitmq',
    port: process.env.RABBITMQ_PORT || 5672,
    user: process.env.RABBITMQ_USER || 'guest',
    password: process.env.RABBITMQ_PASSWORD || 'guest',
  },
  email: {
    host: process.env.SMTP_HOST || 'localhost',
    port: parseInt(process.env.SMTP_PORT || '587'),
    user: process.env.SMTP_USER || '',
    password: process.env.SMTP_PASSWORD || '',
    from: process.env.EMAIL_FROM || 'noreply@hospital.local',
  },
  serviceName: 'notification-service',
};
```

#### Step 3: Services

**`src/services/email-service.js`** (~30 lines)

```javascript
const nodemailer = require('nodemailer');
const config = require('../config');
const { createLogger } = require('@hospital/shared');

class EmailService {
  constructor() {
    this._logger = createLogger('notification-service');
    this._transporter = nodemailer.createTransport({
      host: config.email.host,
      port: config.email.port,
      auth: config.email.user ? { user: config.email.user, pass: config.email.password } : undefined,
    });
  }

  async send(to, subject, body) {
    try {
      await this._transporter.sendMail({ from: config.email.from, to, subject, html: body });
      this._logger.info('Email sent', { to, subject });
    } catch (err) {
      this._logger.error('Email send failed', { to, error: err.message });
      throw err;
    }
  }
}

module.exports = EmailService;
```

**`src/services/sms-service.js`** (~20 lines) — Stub with `send(phoneNumber, message)` that logs only (no Twilio in scaffold)

**`src/services/notification-service.js`** (~45 lines)

```javascript
class NotificationService {
  constructor(emailService, smsService, redisClient) {
    this._emailService = emailService;
    this._smsService = smsService;
    this._redis = redisClient;
    this._logger = createLogger('notification-service');
  }

  async sendNotification(type, recipient, data) {
    // Log to Redis for history
    const logEntry = { type, recipient, data, sentAt: new Date().toISOString(), status: 'sent' };
    await this._redis.lpush('notification:logs', JSON.stringify(logEntry));

    if (type === 'email') {
      await this._emailService.send(recipient, data.subject, data.body);
    } else if (type === 'sms') {
      await this._smsService.send(recipient, data.message);
    }

    return logEntry;
  }

  async getRecentLogs(limit = 50) {
    const logs = await this._redis.lrange('notification:logs', 0, limit - 1);
    return logs.map(JSON.parse);
  }
}

module.exports = NotificationService;
```

#### Step 4: Models (Redis-backed, simple structures)

**`src/models/notification-template.js`** (~20 lines)

```javascript
// Templates stored in Redis hash: notification:templates
class NotificationTemplate {
  static async getAll(redis) {
    const keys = await redis.hkeys('notification:templates');
    const templates = [];
    for (const key of keys) {
      templates.push(JSON.parse(await redis.hget('notification:templates', key)));
    }
    return templates;
  }

  static async create(redis, template) {
    await redis.hset('notification:templates', template.name, JSON.stringify(template));
    return template;
  }
}

module.exports = NotificationTemplate;
```

**`src/models/notification-log.js`** — Re-uses Redis list `notification:logs` from service

#### Step 5: Controllers

**`src/controllers/notification-controller.js`** (~35 lines)
- `send(req, res, next)` — POST body: `{ type, recipient, data }`
- `getLogs(req, res, next)` — GET query: `{ limit }`
- `getTemplates(req, res, next)` — GET
- `createTemplate(req, res, next)` — POST body: `{ name, subject, body, channels }`

**`src/controllers/health-controller.js`** (~10 lines) — standard health response

#### Step 6: Routes

**`src/routes/notification-routes.js`** (~15 lines)
- `POST /api/notifications/send`
- `GET /api/notifications/logs`
- `GET /api/notifications/templates`
- `POST /api/notifications/templates`

**`src/routes/health-routes.js`** (~8 lines)

#### Step 7: RabbitMQ Consumer

**`src/queue/rabbitmq-consumer.js`** (~40 lines)

```javascript
const { connectRabbitMQ, subscribe } = require('@hospital/shared');
const { EVENT_NAMES } = require('@hospital/shared');

async function startConsumers(notificationService) {
  const channel = await connectRabbitMQ();

  await subscribe(channel, EVENT_NAMES.PATIENT_CREATED, async (msg) => {
    await notificationService.sendNotification('email', msg.email, {
      subject: 'Welcome to Hospital HRM',
      body: `Hello ${msg.firstName}, welcome to our hospital system.`,
    });
  });

  await subscribe(channel, EVENT_NAMES.APPOINTMENT_SCHEDULED, async (msg) => {
    // Stub: send appointment reminder
  });

  await subscribe(channel, EVENT_NAMES.PRESCRIPTION_ISSUED, async (msg) => {
    // Stub: send prescription ready notification
  });

  await subscribe(channel, EVENT_NAMES.INVENTORY_LOW, async (msg) => {
    // Stub: alert pharmacy staff
  });
}

module.exports = { startConsumers };
```

#### Step 8: Server & App

**`src/app.js`** (~20 lines) — Express setup with helmet, cors, json parser

**`src/server.js`** (~35 lines)

```javascript
const Redis = require('ioredis');
const config = require('./config');
// ... DI setup
const redis = new Redis({ host: config.redis.host, port: config.redis.port });
// Wire services, controllers, routes
// Start RabbitMQ consumers
// Listen on config.port
```

#### Step 9: Dockerfile

```dockerfile
FROM node:20-alpine
WORKDIR /app
EXPOSE 5005
COPY shared/hospital-shared-js/ /shared/hospital-shared-js/
COPY services/NotificationService/package*.json ./
RUN npm install --production
COPY services/NotificationService/ .
ENV PORT=5005
CMD ["node", "src/server.js"]
```

---

### SearchService

#### Step 10: Project Init

```bash
mkdir -p services/SearchService
cd services/SearchService
npm init -y
npm install express @elastic/elasticsearch amqplib cors helmet dotenv
npm install --save-dev jest eslint nodemon
```

#### Step 11: Config

**`src/config/index.js`** (~20 lines)

```javascript
module.exports = {
  port: process.env.PORT || 5006,
  elasticsearch: {
    node: process.env.ELASTICSEARCH_URI || 'http://elasticsearch:9200',
  },
  rabbitmq: { ... },
  serviceName: 'search-service',
};
```

#### Step 12: Services

**`src/services/search-service.js`** (~50 lines)

```javascript
const { Client } = require('@elastic/elasticsearch');
const config = require('../config');

class SearchService {
  constructor() {
    this._client = new Client({ node: config.elasticsearch.node });
    this._logger = createLogger('search-service');
  }

  async search(query, type = 'patient', page = 1, pageSize = 50) {
    const index = type === 'patient' ? 'hospital-patients' : 'hospital-drugs';
    const from = (page - 1) * pageSize;

    const result = await this._client.search({
      index,
      from,
      size: pageSize,
      query: {
        multi_match: {
          query,
          fields: type === 'patient'
            ? ['firstName', 'lastName', 'email']
            : ['name', 'code'],
        },
      },
    });

    return {
      items: result.hits.hits.map(h => ({ id: h._id, ...h._source, score: h._score })),
      total: result.hits.total.value,
      page,
      pageSize,
    };
  }
}

module.exports = SearchService;
```

**`src/services/indexer-service.js`** (~40 lines)

```javascript
class IndexerService {
  constructor(esClient) {
    this._client = esClient;
  }

  async ensureIndices() {
    const indices = ['hospital-patients', 'hospital-drugs'];
    for (const index of indices) {
      const exists = await this._client.indices.exists({ index });
      if (!exists) {
        await this._client.indices.create({
          index,
          body: {
            settings: { number_of_shards: 1, number_of_replicas: 0 },
            mappings: {
              properties: index === 'hospital-patients'
                ? { firstName: { type: 'text' }, lastName: { type: 'text' }, email: { type: 'keyword' }, patientId: { type: 'keyword' } }
                : { name: { type: 'text' }, code: { type: 'keyword' }, dosage: { type: 'text' } },
            },
          },
        });
      }
    }
  }

  async indexPatient(patient) {
    await this._client.index({ index: 'hospital-patients', id: patient.patientId, body: patient });
  }

  async indexDrug(drug) {
    await this._client.index({ index: 'hospital-drugs', id: String(drug.id), body: drug });
  }
}

module.exports = IndexerService;
```

#### Step 13: Controller

**`src/controllers/search-controller.js`** (~25 lines)

```javascript
class SearchController {
  constructor(searchService) {
    this._searchService = searchService;
  }

  async search(req, res, next) {
    try {
      const { q, type, page, pageSize } = req.query;
      if (!q) return res.status(400).json({ error: { message: 'Query parameter "q" is required' } });
      const result = await this._searchService.search(q, type || 'patient', +page || 1, +pageSize || 50);
      res.json({ data: result.items, pagination: { total: result.total, page: result.page, pageSize: result.pageSize } });
    } catch (err) { next(err); }
  }
}

module.exports = SearchController;
```

#### Step 14: Routes

**`src/routes/search-routes.js`** (~10 lines)
- `GET /api/search` — search with query params `q`, `type`, `page`, `pageSize`

#### Step 15: RabbitMQ Consumer

**`src/queue/rabbitmq-consumer.js`** (~30 lines)

```javascript
async function startConsumers(indexerService) {
  const channel = await connectRabbitMQ();

  await subscribe(channel, EVENT_NAMES.PATIENT_CREATED, async (msg) => {
    await indexerService.indexPatient(msg);
  });

  await subscribe(channel, EVENT_NAMES.PATIENT_UPDATED, async (msg) => {
    await indexerService.indexPatient(msg);
  });
}

module.exports = { startConsumers };
```

#### Step 16: Server

**`src/server.js`** (~30 lines)

- Create Elasticsearch client
- `indexerService.ensureIndices()` on startup
- Wire search routes
- Start RabbitMQ consumers
- Listen on port 5006

#### Step 17: Dockerfile

Same pattern as NotificationService, EXPOSE 5006.

## Todo List

- [x] Create NotificationService package.json + deps
- [x] Implement config, email-service, sms-service stubs
- [x] Implement notification-service with Redis logging
- [x] Implement notification-template model (Redis hash)
- [x] Implement controller + routes
- [x] Implement rabbitmq-consumer with event handlers
- [x] Implement app.js + server.js
- [x] Create Dockerfile + .dockerignore + .eslintrc.json
- [x] Create test stub
- [x] Create SearchService package.json + deps
- [x] Implement search-service (Elasticsearch queries)
- [x] Implement indexer-service (index creation, document indexing)
- [x] Implement controller + routes
- [x] Implement rabbitmq-consumer for indexing events
- [x] Create SearchService Dockerfile
- [x] Verify both services start and respond to /health

## Success Criteria

- Both `npm install` complete without errors
- `npm test` passes for both services
- Dockerfiles build successfully
- NotificationService starts on port 5005, `GET /health` returns 200
- SearchService starts on port 5006, `GET /health` returns 200
- SearchService creates Elasticsearch indices on startup
- NotificationService logs to Redis on send

## Risk Assessment

| Risk | Impact | Mitigation |
|---|---|---|
| Elasticsearch client version mismatch | Medium | Pin @elastic/elasticsearch to match ES 8.x |
| Redis connection fails on startup | Low | Retry with backoff in server.js |
| Nodemailer SMTP not configured in dev | Low | Log email content instead of sending when no SMTP |
| Elasticsearch index creation race condition | Low | Use `if !exists` check, idempotent |
