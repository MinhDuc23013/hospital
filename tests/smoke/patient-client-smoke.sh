#!/bin/bash
# Smoke test script for the patient client app running on port 3100.
# Asserts HTTP status codes and exits non-zero if any check fails.
set -e

FAILURES=0

check() {
  local desc=$1
  local url=$2
  local expected=$3
  local actual
  actual=$(curl -s -o /dev/null -w "%{http_code}" "$url")
  if [ "$actual" = "$expected" ]; then
    echo "PASS: $desc ($actual)"
  else
    echo "FAIL: $desc — expected $expected, got $actual"
    FAILURES=$((FAILURES + 1))
  fi
}

echo "=== Patient Client Smoke Test ==="

check "Home page accessible"           "http://localhost:3100"                         200
check "Dashboard redirects to login"   "http://localhost:3100/dashboard"              307
check "Login page accessible"          "http://localhost:3100/login"                  200
check "Proxy blocked without auth"     "http://localhost:3100/api/proxy/appointments" 401
check "Proxy allowlist enforced"       "http://localhost:3100/api/proxy/admin/users"  403

if [ "$FAILURES" -gt 0 ]; then
  echo ""
  echo "$FAILURES test(s) FAILED"
  exit 1
fi

echo "=== All smoke tests passed ==="
