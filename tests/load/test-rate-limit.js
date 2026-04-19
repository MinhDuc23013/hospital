import http from "k6/http";
import { Counter } from "k6/metrics";

const ok = new Counter("status_200");
const limited = new Counter("status_429");
const other = new Counter("status_other");

const KEYCLOAK_URL = __ENV.KEYCLOAK_URL || "http://keycloak:8080";
const GATEWAY_URL  = __ENV.GATEWAY_URL  || "http://hospital-gateway:5084";

// 500 VUs hammering health endpoint (very fast, no DB) to saturate token bucket
export const options = { vus: 500, duration: "10s" };

export function setup() {
  const res = http.post(`${KEYCLOAK_URL}/realms/hospital/protocol/openid-connect/token`, {
    grant_type: "password", client_id: "hospital-frontend",
    username: "admin01@hospital.test", password: "Admin@123",
  });
  return { token: JSON.parse(res.body).access_token };
}

export default function(data) {
  const res = http.get(`${GATEWAY_URL}/api/patients?page=1&pageSize=1`, {
    headers: { Authorization: `Bearer ${data.token}` },
  });
  if (res.status === 200) ok.add(1);
  else if (res.status === 429) limited.add(1);
  else other.add(1);
}

export function handleSummary(data) {
  const o = data.metrics.status_200 ? data.metrics.status_200.values.count : 0;
  const r = data.metrics.status_429 ? data.metrics.status_429.values.count : 0;
  const e = data.metrics.status_other ? data.metrics.status_other.values.count : 0;
  const duration = 10;
  return { stdout: `
===== RATE LIMIT TEST (TokenBucket) =====
200 OK:           ${o}  (~${(o / duration).toFixed(0)} req/s passed)
429 Rate Limited: ${r}  (~${(r / duration).toFixed(0)} req/s rejected)
Other:            ${e}
Total:            ${o + r + e}  (~${((o + r + e) / duration).toFixed(0)} req/s total)
=========================================
` };
}
