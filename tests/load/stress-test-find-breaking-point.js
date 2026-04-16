import http from "k6/http";
import { check, sleep } from "k6";
import { Trend, Counter } from "k6/metrics";
import exec from "k6/execution";

// ── Custom metrics to track per-stage latency ────────────────────────────────
const latencyByStage = new Trend("latency_by_stage", true);
const timeoutCount   = new Counter("timeout_count");

// ── Config: ramp từ 10 → 200 VUs, mỗi bậc giữ 1m để đo ổn định ─────────
export const options = {
  stages: [
    { duration: "30s", target: 10 },   // 10 CCU
    { duration: "1m",  target: 20 },   // 20 CCU
    { duration: "1m",  target: 40 },   // 40 CCU
    { duration: "1m",  target: 60 },   // 60 CCU
    { duration: "1m",  target: 80 },   // 80 CCU
    { duration: "1m",  target: 100 },  // 100 CCU
    { duration: "1m",  target: 150 },  // 150 CCU
    { duration: "1m",  target: 200 },  // 200 CCU
    { duration: "30s", target: 0 },    // cool-down
  ],
  thresholds: {
    http_req_duration: [
      { threshold: "p(95)<2000", abortOnFail: false },
      { threshold: "p(99)<5000", abortOnFail: false },
    ],
    http_req_failed: [{ threshold: "rate<0.10", abortOnFail: false }],
  },
  // Output summary with all percentiles
  summaryTrendStats: ["avg", "min", "med", "max", "p(90)", "p(95)", "p(99)", "count"],
};

// ── Keycloak token ───────────────────────────────────────────────────────────
const KEYCLOAK_URL = __ENV.KEYCLOAK_URL || "http://keycloak:8080";
const GATEWAY_URL  = __ENV.GATEWAY_URL  || "http://hospital-gateway:5084";
const TIMEOUT_MS       = 2000; // Coi response > 2s là "timeout lâu"
const MAX_TIMEOUTS     = 20;  // Dừng test sau 20 lần > 2s
let   timeoutCounter   = 0;   // Shared counter (per-VU, nhưng abort là global)

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

// ── Test scenario ────────────────────────────────────────────────────────────
const searchNames = ["Nguyễn", "Trần", "Lê", "Phạm", "Hoàng", "Phan", "Vũ", "Đặng"];

export default function (data) {
  const name = searchNames[Math.floor(Math.random() * searchNames.length)];
  const page = Math.floor(Math.random() * 10) + 1;
  const url  = `${GATEWAY_URL}/api/doctors?searchName=${encodeURIComponent(name)}&page=${page}&pageSize=50&isActive=true`;

  const res = http.get(url, {
    headers: { Authorization: `Bearer ${data.token}` },
    timeout: "10s",
  });

  // Track latency with current VU count as tag
  latencyByStage.add(res.timings.duration, { vus: `${__VU}` });

  // Count requests exceeding timeout threshold — abort after MAX_TIMEOUTS
  if (res.timings.duration > TIMEOUT_MS) {
    timeoutCount.add(1);
    timeoutCounter++;
    if (timeoutCounter >= MAX_TIMEOUTS) {
      console.error(`⛔ Reached ${MAX_TIMEOUTS} timeouts (>2s) at ${exec.vu.idInTest} VUs — aborting test`);
      exec.test.abort(`Breaking point reached: ${MAX_TIMEOUTS} requests exceeded ${TIMEOUT_MS}ms`);
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

// ── Summary: in ra bảng CCU vs latency ───────────────────────────────────────
export function handleSummary(data) {
  const duration = data.metrics.http_req_duration;
  const timeouts = data.metrics.timeout_count;
  const failed   = data.metrics.http_req_failed;
  const checks   = data.metrics.checks;

  const report = `
=====================================
  STRESS TEST - BREAKING POINT REPORT
=====================================

Total Requests : ${duration.values.count}
Timeout (>2s)  : ${timeouts ? timeouts.values.count : 0}
Failed Rate    : ${failed ? (failed.values.rate * 100).toFixed(2) : 0}%
Check Pass Rate: ${checks ? (checks.values.rate * 100).toFixed(2) : 0}%

Latency Distribution:
  avg  : ${duration.values.avg.toFixed(0)}ms
  med  : ${duration.values.med.toFixed(0)}ms
  p90  : ${duration.values["p(90)"].toFixed(0)}ms
  p95  : ${duration.values["p(95)"].toFixed(0)}ms
  p99  : ${duration.values["p(99)"].toFixed(0)}ms
  max  : ${duration.values.max.toFixed(0)}ms

Thresholds:
  p95 < 2000ms : ${duration.values["p(95)"] < 2000 ? "PASS" : "FAIL (" + duration.values["p(95)"].toFixed(0) + "ms)"}
  p99 < 5000ms : ${duration.values["p(99)"] < 5000 ? "PASS" : "FAIL (" + duration.values["p(99)"].toFixed(0) + "ms)"}
  error < 10%  : ${!failed || failed.values.rate < 0.10 ? "PASS" : "FAIL (" + (failed.values.rate * 100).toFixed(2) + "%)"}

TIP: Xem chi tiết từng mốc CCU trên Grafana k6 dashboard
     hoặc chạy với --out json=result.json rồi analyze.
=====================================
`;

  return {
    stdout: report,
    "tests/load/stress-test-result.json": JSON.stringify(data, null, 2),
  };
}
