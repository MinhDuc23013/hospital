### 1. Lựa chọn API Gateway

**Dùng YARP (Yet Another Reverse Proxy)** — thư viện của Microsoft, chạy trên .NET, phù hợp nhất khi team đã có .NET:

- Tích hợp native với ASP.NET middleware (auth, rate limiting, logging)
- Cấu hình qua `appsettings.json` hoặc code
- Hỗ trợ load balancing, health check, circuit breaker
- Dễ tích hợp với **Keycloak** hoặc **IdentityServer** để xác thực JWT

Nếu muốn giải pháp cloud-native hơn thì có thể dùng **Ocelot**, nhưng YARP được Microsoft duy trì tích cực hơn hiện tại.

---

### 2. Cấu trúc thư mục Project

```
hospital-microservices/
├── docker-compose.yml
├── docker-compose.override.yml
├── .env
│
├── gateway/
│   └── HospitalGateway/          (.NET 8, YARP)
│
├── services/
│   ├── PatientService/           (.NET 8, PostgreSQL)
│   ├── AppointmentService/       (.NET 8, PostgreSQL)
│   ├── MedicalRecordService/     (Node.js, MongoDB)
│   ├── PharmacyService/          (Node.js, SQL Server)
│   └── NotificationService/      (Node.js, Redis Pub/Sub)
│
├── shared/
│   ├── HospitalShared/           (DTOs, Events, Contracts - .NET)
│   └── hospital-shared-js/       (shared types, utils - Node.js)
│
└── infra/
    ├── keycloak/
    ├── prometheus/
    └── grafana/
```

---

### 3. Phân chia Services & Database

| Service | Tech | Database | Chức năng |
|---|---|---|---|
| **Patient Service** | .NET 8 | PostgreSQL | CRUD bệnh nhân, hồ sơ |
| **Appointment Service** | .NET 8 | PostgreSQL | Đặt lịch, lịch khám |
| **Medical Record Service** | Node.js | MongoDB | Bệnh án, kết quả xét nghiệm |
| **Pharmacy Service** | Node.js | SQL Server | Thuốc, đơn thuốc, tồn kho |
| **Notification Service** | Node.js | Redis | Email/SMS/Push notification |
| **Auth Service** | .NET 8 | SQL Server | Đăng nhập, phân quyền (Keycloak) |
| **Search Service** | Node.js | Elasticsearch | Tìm kiếm bệnh nhân, thuốc |

**Redis** dùng cho: cache session, rate limiting, pub/sub events realtime.

---

### 4. Các bước Build từng Phase

**Phase 1 — Infrastructure (Tuần 1)**
- Viết `docker-compose.yml` cho tất cả DB + RabbitMQ + Redis + Seq
- Cấu hình Keycloak realm, client cho gateway
- Setup shared network và volume

**Phase 2 — Gateway + Auth (Tuần 2)**
- Build YARP Gateway: routing, JWT validation middleware
- Config rate limiting per route
- Health check endpoint `/health`

**Phase 3 — Core Services .NET (Tuần 3–4)**
- Patient Service: EF Core + PostgreSQL, clean architecture
- Appointment Service: EF Core + PostgreSQL, gọi Patient Service qua HTTP
- Publish events qua MassTransit/RabbitMQ khi có appointment mới

**Phase 4 — Node.js Services (Tuần 5–6)**
- Medical Record Service: Express + Mongoose
- Pharmacy Service: Express + `mssql` hoặc Sequelize
- Subscribe events từ RabbitMQ (`amqplib`)

**Phase 5 — Notification + Search (Tuần 7)**
- Notification Service: nhận event → gửi email (Nodemailer) hoặc SMS
- Search Service: index data vào Elasticsearch, expose search API

**Phase 6 — Observability + Polish (Tuần 8)**
- Serilog → Seq cho .NET services
- Winston → Seq hoặc ELK cho Node.js
- Prometheus + Grafana dashboard
- OpenTelemetry distributed tracing

---

### 5. Giao tiếp giữa Services

- **Synchronous**: HTTP/REST qua YARP gateway (hoặc gRPC giữa internal services nếu cần hiệu năng)
- **Asynchronous**: RabbitMQ qua MassTransit (.NET) / `amqplib` (Node.js)

Ví dụ flow đặt lịch:
```
Client → Gateway → AppointmentService (tạo lịch)
                         ↓ publish event "AppointmentCreated"
              NotificationService (gửi SMS nhắc lịch)
              MedicalRecordService (tạo hồ sơ trống)
```
