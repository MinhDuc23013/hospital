import http from "k6/http";
import { check, sleep } from "k6";

// ── Config ───────────────────────────────────────────────────────────────────
// Ramp-up load test: tăng dần VUs để tìm giới hạn chịu tải
export const options = {
  stages: [
    { duration: "30s", target: 10 },   // warm-up: 0 → 50 users
    { duration: "1m",  target: 20 },   // ramp: 50 → 100
    { duration: "1m",  target: 40 },   // push: 100 → 200
    { duration: "1m",  target: 50 },   // stress: 200 → 500
    { duration: "30s", target: 0 },     // cool-down
  ],
  thresholds: {
    http_req_duration: ["p(95)<2000"],  // 95% requests < 2s
    http_req_failed: ["rate<0.05"],     // < 5% errors
  },
};

// ── Keycloak token ───────────────────────────────────────────────────────────
const KEYCLOAK_URL = __ENV.KEYCLOAK_URL || "http://keycloak:8080";
const GATEWAY_URL  = __ENV.GATEWAY_URL  || "http://hospital-gateway:5084";

function getToken() {
  const res = http.post(`${KEYCLOAK_URL}/realms/hospital/protocol/openid-connect/token`, {
    grant_type: "password",
    client_id:  "hospital-frontend",
    username:   "admin01@hospital.test",
    password:   "Admin@123",
  });
  return JSON.parse(res.body).access_token;
}

// Lấy token 1 lần cho mỗi VU
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
  });

  check(res, {
    "status 200":       (r) => r.status === 200,
    "has data":         (r) => JSON.parse(r.body).data !== undefined,
    "response < 2s":    (r) => r.timings.duration < 2000,
  });

  sleep(0.5); // simulate think time
}
