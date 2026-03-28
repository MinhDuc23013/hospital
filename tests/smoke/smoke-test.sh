#!/bin/bash
# Hospital HRM — Smoke Test
# Checks /health endpoints for all infrastructure and application services.
# Usage: bash tests/smoke/smoke-test.sh [BASE_URL]
# Default BASE_URL: http://localhost

set -euo pipefail

BASE_URL="${1:-http://localhost}"
PASS=0
FAIL=0

check_health() {
  local name="$1"
  local url="$2"
  local status

  status=$(curl -s -o /dev/null -w "%{http_code}" --max-time 5 "$url" 2>/dev/null || echo "000")

  if [ "$status" = "200" ]; then
    echo "[PASS] $name -> $status  ($url)"
    PASS=$((PASS + 1))
  else
    echo "[FAIL] $name -> $status  ($url)"
    FAIL=$((FAIL + 1))
  fi
}

echo "========================================"
echo "  Hospital HRM — Smoke Tests"
echo "  Base: $BASE_URL"
echo "========================================"
echo ""

echo "--- Infrastructure ---"
check_health "RabbitMQ Management UI" "$BASE_URL:15672"
check_health "Keycloak"               "$BASE_URL:8080"
check_health "Prometheus"             "$BASE_URL:9090/-/healthy"
check_health "Grafana"                "$BASE_URL:3000/api/health"
check_health "Seq"                    "$BASE_URL:5341"

echo ""
echo "--- Application Services ---"
check_health "Gateway (port 8000)"             "$BASE_URL:8000/health"
check_health "PatientService (port 5001)"      "$BASE_URL:5001/health"
check_health "AppointmentService (port 5002)"  "$BASE_URL:5002/health"
check_health "MedicalRecordService (port 5003)" "$BASE_URL:5003/health"
check_health "PharmacyService (port 5004)"     "$BASE_URL:5004/health"
check_health "NotificationService (port 5005)" "$BASE_URL:5005/health"
check_health "SearchService (port 5006)"       "$BASE_URL:5006/health"

echo ""
echo "========================================"
echo "  Results: $PASS passed, $FAIL failed"
echo "========================================"

if [ "$FAIL" -gt 0 ]; then
  exit 1
fi
