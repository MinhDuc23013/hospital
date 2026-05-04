import http from 'k6/http';
import { check, fail } from 'k6';
import { Rate, Trend, Counter } from 'k6/metrics';

const cacheHits  = new Counter('cache_hits');
const cacheMiss  = new Counter('cache_misses');
const p99Latency = new Trend('p99_latency', true);
const errorRate  = new Rate('error_rate');

const PATIENT_IDS = [
  '085a5ce6-c65e-4ffc-bfde-b8329a720453',
  'b46c9297-5982-46d1-9495-0ebd9ab77ecb',
  '7a1453f1-2f6b-4f4b-ad3a-bcb435e0519e',
  '1ed0d38b-3d0a-4ab6-a418-6d3211f82f67',
  'c2929a7e-6d1b-4c1d-9efe-39c6df7d320b',
];

const BASE_URL      = __ENV.BASE_URL       || 'http://localhost:5084';
// Use service name 'keycloak' (not 'hospital-keycloak') so token issuer matches gateway Keycloak:Authority
const KEYCLOAK_URL  = __ENV.KEYCLOAK_URL   || 'http://keycloak:8080';
const CLIENT_SECRET = __ENV.CLIENT_SECRET  || 'AwKekuhrL91DejQdQE4xGZPW56Q6XaVo';

export const options = {
  scenarios: {
    // Ramp up slowly to find the gateway's saturation point
    find_limit: {
      executor: 'ramping-arrival-rate',
      startRate: 100,
      timeUnit: '1s',
      preAllocatedVUs: 100,
      maxVUs: 3500,          // host has 32GB RAM — low preAlloc to avoid startup spike
      stages: [
        { duration: '20s', target: 500  },   // warm-up
        { duration: '20s', target: 1000 },   // light
        { duration: '20s', target: 1500 },   // moderate
        { duration: '20s', target: 2000 },   // medium
        { duration: '20s', target: 2500 },   // high
        { duration: '20s', target: 3000 },   // very high
        { duration: '20s', target: 3500 },   // extreme
        { duration: '20s', target: 4000 },   // target ceiling
        { duration: '20s', target: 4000 },   // hold to confirm
      ],
    },
  },
  // abortOnFail: false — run to completion even when thresholds breach
  thresholds: {
    http_req_failed:   [{ threshold: 'rate<0.05',    abortOnFail: false }],
    http_req_duration: [{ threshold: 'p(95)<500',    abortOnFail: false }],
  },
};

// Single shared token — prevents Keycloak from being overwhelmed by 2000 VUs
export function setup() {
  const res = http.post(
    `${KEYCLOAK_URL}/realms/hospital/protocol/openid-connect/token`,
    {
      grant_type:    'password',
      client_id:     'hospital-gateway',
      client_secret: CLIENT_SECRET,
      username:      'doctor-user',
      password:      '123456',
    }
  );

  if (res.status !== 200) fail(`Token fetch failed: ${res.status} ${res.body}`);
  const token = JSON.parse(res.body).access_token;
  if (!token) fail('No access_token in response');
  return { token };
}

export default function (data) {
  const patientId = PATIENT_IDS[Math.floor(Math.random() * PATIENT_IDS.length)];
  const url       = `${BASE_URL}/api/patients/${patientId}`;

  const res = http.get(url, {
    headers: { Authorization: `Bearer ${data.token}`, Accept: 'application/json' },
    tags: { type: 'patient_lookup' },
  });

  const ok = check(res, {
    'status 200': (r) => r.status === 200,
    'not 5xx':    (r) => r.status < 500,
  });

  errorRate.add(!ok);
  p99Latency.add(res.timings.duration);

  if (res.status === 200) {
    cacheHits.add(1);
  } else {
    cacheMiss.add(1);
  }
}
