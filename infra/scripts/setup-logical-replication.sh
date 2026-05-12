#!/bin/sh
# Sets up cascading logical replication: Physical Replica → Logical Replica.
# Publication is created on the primary; subscription connects to physical replica (PG16+).
# Idempotent — safe to run multiple times.
set -e

# Primary host — used only to create the publication
PUB_HOST="${PUB_HOST:-postgres}"
PUB_PORT="${PUB_PORT:-5432}"

# Physical replica — logical replica subscribes from here (cascading)
SUB_HOST="${SUB_HOST:-postgres-replica}"
SUB_PORT="${SUB_PORT:-5432}"

PG_USER="${POSTGRES_USER:-hospital}"
PG_PASS="${POSTGRES_PASSWORD:-dev_password_change_me}"
PG_DB="${POSTGRES_DB:-hospital_db}"
PUB_NAME="hospital_read_pub"
SUB_NAME="hospital_read_sub"
TABLES="patients, appointments, doctors, lab_orders, prescriptions, payments"

export PGPASSWORD="$PG_PASS"

echo "[logical] waiting for primary $PUB_HOST:$PUB_PORT..."
until pg_isready -h "$PUB_HOST" -p "$PUB_PORT" -U "$PG_USER" > /dev/null 2>&1; do
  sleep 2
done

echo "[logical] waiting for physical replica $SUB_HOST:$SUB_PORT..."
until pg_isready -h "$SUB_HOST" -p "$SUB_PORT" -U "$PG_USER" > /dev/null 2>&1; do
  sleep 2
done

# Verify tables exist on primary before proceeding
echo "[logical] checking tables exist on primary..."
until psql -h "$PUB_HOST" -p "$PUB_PORT" -U "$PG_USER" -d "$PG_DB" \
  -c "SELECT 1 FROM patients LIMIT 1" > /dev/null 2>&1; do
  echo "[logical] tables not ready yet, retrying in 5s..."
  sleep 5
done

# ── Step 1: Create publication on primary ─────────────────────────────────
echo "[logical] creating publication '$PUB_NAME' on primary..."
psql -h "$PUB_HOST" -p "$PUB_PORT" -U "$PG_USER" -d "$PG_DB" << SQL
DO \$\$
BEGIN
  IF NOT EXISTS (SELECT 1 FROM pg_publication WHERE pubname = '$PUB_NAME') THEN
    CREATE PUBLICATION $PUB_NAME FOR TABLE $TABLES;
    RAISE NOTICE 'Publication $PUB_NAME created';
  ELSE
    RAISE NOTICE 'Publication $PUB_NAME already exists, skipping';
  END IF;
END
\$\$;
SQL

# ── Step 2: Import schema from primary ────────────────────────────────────
echo "[logical] importing table schema from primary..."
pg_dump \
  -h "$PUB_HOST" -p "$PUB_PORT" \
  -U "$PG_USER" -d "$PG_DB" \
  --schema-only --no-owner --no-acl \
  -t patients -t appointments -t doctors \
  -t lab_orders -t prescriptions -t payments \
  | psql -U "$PG_USER" -d "$PG_DB" --set ON_ERROR_STOP=off 2>&1 \
  | grep -v "already exists" || true

# ── Step 3: Create subscription pointing to physical replica (cascading) ──
# CREATE SUBSCRIPTION must be a top-level statement — cannot run inside DO block
SUB_EXISTS=$(psql -U "$PG_USER" -d "$PG_DB" -tAc \
  "SELECT 1 FROM pg_subscription WHERE subname = '$SUB_NAME'")

if [ "$SUB_EXISTS" = "1" ]; then
  echo "[logical] subscription '$SUB_NAME' already exists, skipping"
else
  echo "[logical] creating subscription '$SUB_NAME' → $SUB_HOST (cascading via physical replica)..."
  psql -U "$PG_USER" -d "$PG_DB" -c \
    "CREATE SUBSCRIPTION $SUB_NAME
     CONNECTION 'host=$SUB_HOST port=$SUB_PORT dbname=$PG_DB user=$PG_USER password=$PG_PASS'
     PUBLICATION $PUB_NAME;"
  echo "[logical] subscription created — initial data copy started"
fi

# ── Step 4: Read-optimized indexes (not present on primary or physical replica) ──
echo "[logical] creating read-optimized indexes..."
psql -U "$PG_USER" -d "$PG_DB" << 'SQL'

-- patients: phone lookup, name prefix search, active filter
CREATE INDEX IF NOT EXISTS idx_read_patients_phone
  ON patients ("PhoneNumber");
CREATE INDEX IF NOT EXISTS idx_read_patients_active_created
  ON patients ("IsActive", "CreatedAt" DESC);
CREATE INDEX IF NOT EXISTS idx_read_patients_firstname
  ON patients (lower("FirstName") text_pattern_ops);
CREATE INDEX IF NOT EXISTS idx_read_patients_lastname
  ON patients (lower("LastName") text_pattern_ops);

-- appointments: list by patient/doctor + time range, filter by status
CREATE INDEX IF NOT EXISTS idx_read_appt_patient_time
  ON appointments ("PatientId", "ScheduledTime" DESC);
CREATE INDEX IF NOT EXISTS idx_read_appt_doctor_time
  ON appointments ("DoctorId", "ScheduledTime" DESC);
CREATE INDEX IF NOT EXISTS idx_read_appt_status_time
  ON appointments ("Status", "ScheduledTime" DESC);
CREATE INDEX IF NOT EXISTS idx_read_appt_time
  ON appointments ("ScheduledTime" DESC);

-- doctors: active doctor list by specialty
CREATE INDEX IF NOT EXISTS idx_read_doctors_active_specialty
  ON doctors ("IsActive", "Specialty");
CREATE INDEX IF NOT EXISTS idx_read_doctors_fullname
  ON doctors (lower("FullName") text_pattern_ops);

-- lab_orders: history by patient, filter by status (columns are lowercase)
CREATE INDEX IF NOT EXISTS idx_read_laborders_patient_date
  ON lab_orders (patientid, orderedat DESC);
CREATE INDEX IF NOT EXISTS idx_read_laborders_status_date
  ON lab_orders (status, orderedat DESC);
CREATE INDEX IF NOT EXISTS idx_read_laborders_appointment
  ON lab_orders (appointmentid);

-- prescriptions: history by patient/doctor, filter by status
CREATE INDEX IF NOT EXISTS idx_read_rx_patient_date
  ON prescriptions ("PatientId", "CreatedAt" DESC);
CREATE INDEX IF NOT EXISTS idx_read_rx_doctor_date
  ON prescriptions ("DoctorId", "CreatedAt" DESC);
CREATE INDEX IF NOT EXISTS idx_read_rx_status
  ON prescriptions ("Status");
CREATE INDEX IF NOT EXISTS idx_read_rx_appointment
  ON prescriptions ("AppointmentId") WHERE "AppointmentId" IS NOT NULL;

-- payments: history by patient, filter by status, paid timestamp
CREATE INDEX IF NOT EXISTS idx_read_pay_patient_date
  ON payments ("PatientId", "CreatedAt" DESC);
CREATE INDEX IF NOT EXISTS idx_read_pay_status_date
  ON payments ("Status", "CreatedAt" DESC);
CREATE INDEX IF NOT EXISTS idx_read_pay_appointment
  ON payments ("AppointmentId");
CREATE INDEX IF NOT EXISTS idx_read_pay_paid_at
  ON payments ("PaidAt" DESC) WHERE "PaidAt" IS NOT NULL;

SQL

echo "[logical] setup complete — cascading logical replica ready on port ${POSTGRES_LOGICAL_PORT:-5436}"
echo "[logical] topology: primary → physical replica → logical replica"
