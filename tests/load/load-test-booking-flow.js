import http from "k6/http";
import { check, sleep, group } from "k6";
import { Counter, Trend } from "k6/metrics";
import exec from "k6/execution";

// ── Custom metrics ───────────────────────────────────────────────────────────
const bookDuration    = new Trend("booking_duration", true);
const confirmDuration = new Trend("confirm_payment_duration", true);
const completeDuration = new Trend("complete_duration", true);
const flowSuccess     = new Counter("flow_success");
const flowFailed      = new Counter("flow_failed");
const timeoutCount    = new Counter("timeout_count");

const TIMEOUT_MS   = 2000;
const MAX_TIMEOUTS = 20;
let timeoutCounter = 0;

// ── Config ───────────────────────────────────────────────────────────────────
// Stress test: ramp up until breaking point (20 timeouts > 2s).
// Each iteration ≈ 3 HTTP calls. ~6 req/s per VU.
export const options = {
  stages: [
    { duration: "20s", target: 20 },   // warm-up
    { duration: "30s", target: 50 },   // ~300 req/s
    { duration: "30s", target: 80 },   // ~480 req/s
    { duration: "30s", target: 120 },  // ~720 req/s
    { duration: "30s", target: 150 },  // ~900 req/s
    { duration: "20s", target: 0 },    // cool-down
  ],
  thresholds: {
    booking_duration:         [{ threshold: "p(95)<2000", abortOnFail: false }],
    confirm_payment_duration: [{ threshold: "p(95)<2000", abortOnFail: false }],
    complete_duration:        [{ threshold: "p(95)<2000", abortOnFail: false }],
    http_req_failed:          [{ threshold: "rate<0.05",  abortOnFail: false }],
  },
  summaryTrendStats: ["avg", "min", "med", "max", "p(90)", "p(95)", "p(99)", "count"],
};

// ── Keycloak ─────────────────────────────────────────────────────────────────
const KEYCLOAK_URL = __ENV.KEYCLOAK_URL || "http://keycloak:8080";
const GATEWAY_URL  = __ENV.GATEWAY_URL  || "http://hospital-gateway:5084";
const SCHEDULE_URL = __ENV.SCHEDULE_URL || "http://hospital-gateway:5084";

function getToken() {
  const res = http.post(`${KEYCLOAK_URL}/realms/hospital/protocol/openid-connect/token`, {
    grant_type: "password",
    client_id:  "hospital-frontend",
    username:   "admin01@hospital.test",
    password:   "Admin@123",
  });
  return JSON.parse(res.body).access_token;
}

// ── Setup: get token + prepare test data ─────────────────────────────────────
export function setup() {
  const token   = getToken();
  const headers = { Authorization: `Bearer ${token}`, "Content-Type": "application/json" };

  // Get existing patients
  const patientsRes = http.get(`${GATEWAY_URL}/api/patients?page=1&pageSize=10`, { headers });
  const patients = JSON.parse(patientsRes.body).data.map(p => p.id);

  // Doctor ID (use existing seeded doctor)
  const doctorId = "a1b2c3d4-1111-4000-8000-000000000001";

  // Create schedules with many slots for load test (1 schedule per day, 100 days ahead)
  const schedules = [];
  for (let i = 1; i <= 100; i++) {
    const date = new Date();
    date.setDate(date.getDate() + i);
    const dateStr = date.toISOString().split("T")[0] + "T00:00:00";

    const scheduleRes = http.post(`${SCHEDULE_URL}/api/doctor-schedules`, JSON.stringify({
      doctorId:            doctorId,
      doctorName:          "Load Test Doctor",
      date:                dateStr,
      startTime:           "06:00:00",
      endTime:             "22:00:00",
      slotDurationMinutes: 20,
    }), { headers });

    if (scheduleRes.status === 201 || scheduleRes.status === 200) {
      const schedule = JSON.parse(scheduleRes.body);

      // Fetch available slots via dedicated endpoint
      const slotsRes = http.get(`${SCHEDULE_URL}/api/doctor-schedules/${schedule.id}/slots/available`, { headers });
      if (slotsRes.status === 200) {
        const slots = JSON.parse(slotsRes.body);
        const availableSlots = slots.map(s => ({ slotId: s.id, startTime: s.startTime }));

        if (availableSlots.length > 0) {
          schedules.push({
            scheduleId: schedule.id,
            doctorId:   doctorId,
            date:       dateStr,
            slots:      availableSlots,
          });
        }
      }
    }
  }

  console.log(`Setup: ${patients.length} patients, ${schedules.length} schedules, ` +
    `${schedules.reduce((sum, s) => sum + s.slots.length, 0)} total slots`);

  return { token, patients, schedules };
}

// ── Main flow: Book → Confirm Payment → Complete ─────────────────────────────
export default function (data) {
  const headers = {
    Authorization:  `Bearer ${data.token}`,
    "Content-Type": "application/json",
  };

  // Pick random patient + schedule + slot
  const patientId = data.patients[Math.floor(Math.random() * data.patients.length)];
  const schedule  = data.schedules[Math.floor(Math.random() * data.schedules.length)];
  if (!schedule || schedule.slots.length === 0) {
    flowFailed.add(1);
    sleep(0.5);
    return;
  }
  const slot = schedule.slots[Math.floor(Math.random() * schedule.slots.length)];

  // Build scheduledTime from schedule date + slot startTime
  const baseDate = schedule.date.split("T")[0];
  const scheduledTime = `${baseDate}T${slot.startTime}`;

  let sagaId, appointmentId, paymentId;

  // ── Step 1: Book appointment ─────────────────────────────────────────────
  group("1. Book Appointment", function () {
    const payload = JSON.stringify({
      patientId:       patientId,
      doctorId:        schedule.doctorId,
      scheduleId:      schedule.scheduleId,
      slotId:          slot.slotId,
      scheduledTime:   scheduledTime,
      durationMinutes: 20,
      paymentAmount:   200000,
      paymentMethod:   "BankTransfer",
      currency:        "VND",
      notes:           `Load test VU-${__VU}`,
    });

    const res = http.post(`${GATEWAY_URL}/api/appointments/book`, payload, {
      headers, timeout: "30s",
    });

    bookDuration.add(res.timings.duration);
    if (res.timings.duration > TIMEOUT_MS) {
      timeoutCount.add(1);
      timeoutCounter++;
    }

    const ok = check(res, {
      "book: status 201":   (r) => r.status === 201,
      "book: has sagaId":   (r) => { try { return !!JSON.parse(r.body).sagaId; } catch { return false; } },
    });

    if (ok) {
      const body = JSON.parse(res.body);
      sagaId        = body.sagaId;
      appointmentId = body.appointmentId;
      paymentId     = body.paymentId;
    }
  });

  if (!paymentId) {
    flowFailed.add(1);
    sleep(0.5);
    return;
  }

  sleep(0.3); // Simulate user reviewing before paying

  // ── Step 2: Confirm payment ──────────────────────────────────────────────
  group("2. Confirm Payment", function () {
    const res = http.post(
      `${GATEWAY_URL}/api/appointments/confirm-payment/${paymentId}`,
      null, { headers, timeout: "15s" }
    );

    confirmDuration.add(res.timings.duration);
    if (res.timings.duration > TIMEOUT_MS) {
      timeoutCount.add(1);
      timeoutCounter++;
    }

    check(res, {
      "confirm: status 200": (r) => r.status === 200,
    });
  });

  sleep(0.3); // Simulate consultation time

  // ── Step 3: Complete appointment ─────────────────────────────────────────
  group("3. Complete Appointment", function () {
    const res = http.post(
      `${GATEWAY_URL}/api/appointments/${appointmentId}/complete`,
      null, { headers, timeout: "15s" }
    );

    completeDuration.add(res.timings.duration);
    if (res.timings.duration > TIMEOUT_MS) {
      timeoutCount.add(1);
      timeoutCounter++;
    }

    check(res, {
      "complete: status 200": (r) => r.status === 200,
    });
  });

  flowSuccess.add(1);

  // Abort if too many timeouts
  if (timeoutCounter >= MAX_TIMEOUTS) {
    console.error(` ${MAX_TIMEOUTS} timeouts at ${exec.vu.idInTest} VUs — aborting`);
    exec.test.abort(`Breaking point: ${MAX_TIMEOUTS} requests > ${TIMEOUT_MS}ms`);
  }

  sleep(0.5);
}

// ── Summary ──────────────────────────────────────────────────────────────────
export function handleSummary(data) {
  const book    = data.metrics.booking_duration;
  const confirm = data.metrics.confirm_payment_duration;
  const complete = data.metrics.complete_duration;
  const success = data.metrics.flow_success;
  const failed  = data.metrics.flow_failed;
  const timeout = data.metrics.timeout_count;

  const report = `
=============================================
  BOOKING FLOW STRESS TEST REPORT
=============================================

Flow Results:
  Success : ${success ? success.values.count : 0}
  Failed  : ${failed ? failed.values.count : 0}
  Timeout : ${timeout ? timeout.values.count : 0}

Step 1 — Book Appointment:
  avg: ${book ? book.values.avg.toFixed(0) : '-'}ms | p95: ${book ? book.values["p(95)"].toFixed(0) : '-'}ms | max: ${book ? book.values.max.toFixed(0) : '-'}ms

Step 2 — Confirm Payment:
  avg: ${confirm ? confirm.values.avg.toFixed(0) : '-'}ms | p95: ${confirm ? confirm.values["p(95)"].toFixed(0) : '-'}ms | max: ${confirm ? confirm.values.max.toFixed(0) : '-'}ms

Step 3 — Complete Appointment:
  avg: ${complete ? complete.values.avg.toFixed(0) : '-'}ms | p95: ${complete ? complete.values["p(95)"].toFixed(0) : '-'}ms | max: ${complete ? complete.values.max.toFixed(0) : '-'}ms
=============================================
`;
  return { stdout: report };
}
