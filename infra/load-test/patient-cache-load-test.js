import http from 'k6/http';
import { check } from 'k6';
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

// AppointmentService port 5002 — triggers PatientServiceClient FusionCache (L1→L2→HTTP)
const BASE_URL       = __ENV.BASE_URL        || 'http://localhost:5002';
const KEYCLOAK_URL   = __ENV.KEYCLOAK_URL    || 'http://localhost:8080';
const CLIENT_SECRET  = __ENV.CLIENT_SECRET   || 'AwKekuhrL91DejQdQE4xGZPW56Q6XaVo';

export const options = {
  scenarios: {
    spike: {
      executor: 'ramping-arrival-rate',
      startRate: 50,
      timeUnit: '1s',
      preAllocatedVUs: 200,
      maxVUs: 500,
      stages: [
        { duration: '10s', target: 200  },
        { duration: '10s', target: 1000 },
        { duration: '30s', target: 1000 },
        { duration: '10s', target: 0    },
      ],
    },
  },
  thresholds: {
    http_req_failed:   ['rate<0.05'],
    http_req_duration: ['p(95)<500'],
  },
};

let _token = null;
let _tokenExpiry = 0;

function getToken() {
  const now = Date.now() / 1000;
  if (_token && now < _tokenExpiry - 30) return _token;

  const res = http.post(
    `${KEYCLOAK_URL}/realms/hospital/protocol/openid-connect/token`,
    {
      grant_type:    'password',
      client_id:     'hospital-gateway',
      client_secret: CLIENT_SECRET,
      username:      'doctor-user',
      password:      '123456',
    },
    { tags: { type: 'auth' } }
  );

  const body = JSON.parse(res.body);
  _token       = body.access_token;
  _tokenExpiry = now + (body.expires_in || 300);
  return _token;
}

export default function () {
  const token     = getToken();
  const patientId = PATIENT_IDS[Math.floor(Math.random() * PATIENT_IDS.length)];
  // Calls AppointmentService → PatientServiceClient (FusionCache L1→L2→HTTP)
  const url       = `${BASE_URL}/api/appointments/patients/${patientId}/exists`;

  const res = http.get(url, {
    headers: { Authorization: `Bearer ${token}`, Accept: 'application/json' },
    tags: { type: 'patient_cache_probe' },
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
