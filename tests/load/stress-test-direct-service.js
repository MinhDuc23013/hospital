import http from "k6/http";
import { check, sleep } from "k6";
import { Counter } from "k6/metrics";
import exec from "k6/execution";

// ── Same config as breaking point test, but hits service directly (no gateway)
const timeoutCount = new Counter("timeout_count");

export const options = {
  stages: [
    { duration: "30s", target: 10 },
    { duration: "1m",  target: 20 },
    { duration: "1m",  target: 40 },
    { duration: "1m",  target: 60 },
    { duration: "1m",  target: 80 },
    { duration: "1m",  target: 100 },
    { duration: "30s", target: 0 },
  ],
  summaryTrendStats: ["avg", "min", "med", "max", "p(90)", "p(95)", "p(99)", "count"],
};

const KEYCLOAK_URL     = __ENV.KEYCLOAK_URL || "http://keycloak:8080";
// Hit DoctorScheduleService directly, bypass gateway
const SERVICE_URL      = __ENV.SERVICE_URL  || "http://doctor-schedule-service:5007";
const TIMEOUT_MS       = 2000;
const MAX_TIMEOUTS     = 20;
let   timeoutCounter   = 0;

function getToken() {
  const res = http.post(`${KEYCLOAK_URL}/realms/hospital/protocol/openid-connect/token`, {
    grant_type: "password",
    client_id:  "hospital-frontend",
    username:   "admin01@hospital.test",
    password:   "Admin@123",
  });
  return JSON.parse(res.body).access_token;
}

export function setup() {
  const token = getToken();
  if (!token) throw new Error("Failed to get token from Keycloak");
  return { token };
}

const searchNames = ["Nguyễn", "Trần", "Lê", "Phạm", "Hoàng", "Phan", "Vũ", "Đặng"];

export default function (data) {
  const name = searchNames[Math.floor(Math.random() * searchNames.length)];
  const page = Math.floor(Math.random() * 10) + 1;
  const url  = `${SERVICE_URL}/api/doctors?searchName=${encodeURIComponent(name)}&page=${page}&pageSize=50&isActive=true`;

  const res = http.get(url, {
    headers: { Authorization: `Bearer ${data.token}` },
    timeout: "10s",
  });

  if (res.timings.duration > TIMEOUT_MS) {
    timeoutCount.add(1);
    timeoutCounter++;
    if (timeoutCounter >= MAX_TIMEOUTS) {
      console.error(`⛔ DIRECT SERVICE: ${MAX_TIMEOUTS} timeouts at ${exec.vu.idInTest} VUs — aborting`);
      exec.test.abort(`Breaking point (direct): ${MAX_TIMEOUTS} requests exceeded ${TIMEOUT_MS}ms`);
    }
  }

  check(res, {
    "status 200":       (r) => r.status === 200,
    "response < 500ms": (r) => r.timings.duration < 500,
    "response < 1s":    (r) => r.timings.duration < 1000,
    "response < 2s":    (r) => r.timings.duration < 2000,
  });

  sleep(0.5);
}

export function handleSummary(data) {
  const d = data.metrics.http_req_duration;
  const t = data.metrics.timeout_count;

  const report = `
===========================================
  DIRECT SERVICE TEST (bypass gateway)
===========================================

Total Requests : ${d.values.count}
Timeout (>2s)  : ${t ? t.values.count : 0}

Latency:
  avg  : ${d.values.avg.toFixed(0)}ms
  med  : ${d.values.med.toFixed(0)}ms
  p90  : ${d.values["p(90)"].toFixed(0)}ms
  p95  : ${d.values["p(95)"].toFixed(0)}ms
  p99  : ${d.values["p(99)"].toFixed(0)}ms
  max  : ${d.values.max.toFixed(0)}ms

Compare with gateway test to identify bottleneck:
  - Direct fast, gateway slow → Gateway is bottleneck
  - Direct also slow         → DB connection is bottleneck
===========================================
`;
  return { stdout: report };
}
