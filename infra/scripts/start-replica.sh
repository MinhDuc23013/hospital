#!/bin/sh
# Starts PostgreSQL read replica via streaming replication from primary.
# On first boot: clones primary with pg_basebackup (-R flag auto-creates standby.signal
# and primary_conninfo). Subsequent boots reuse existing PGDATA.
set -e

PRIMARY_HOST="${PRIMARY_HOST:-postgres}"
PRIMARY_PORT="${PRIMARY_PORT:-5432}"
REPLICATION_USER="${REPLICATION_USER:-replicator}"
REPLICATION_PASSWORD="${REPLICATION_PASSWORD:-replicator_pw_change_me}"
PGDATA="${PGDATA:-/var/lib/postgresql/data}"

# Fix permissions — Docker volume may be owned by root on first run
mkdir -p "$PGDATA"
chmod 700 "$PGDATA"
chown -R postgres:postgres "$PGDATA" 2>/dev/null || true

# Wait for primary to accept connections
echo "[replica] waiting for primary $PRIMARY_HOST:$PRIMARY_PORT..."
until pg_isready -h "$PRIMARY_HOST" -p "$PRIMARY_PORT" > /dev/null 2>&1; do
  sleep 2
done
echo "[replica] primary is ready"

# Clone from primary on first boot only
if [ ! -f "$PGDATA/PG_VERSION" ]; then
  echo "[replica] cloning primary via pg_basebackup..."
  PGPASSWORD="$REPLICATION_PASSWORD" pg_basebackup \
    -h "$PRIMARY_HOST" \
    -p "$PRIMARY_PORT" \
    -U "$REPLICATION_USER" \
    -D "$PGDATA" \
    -Fp \
    -Xs \
    -R \
    -P \
    --checkpoint=fast
  echo "[replica] clone complete — standby.signal and primary_conninfo set automatically by -R flag"
fi

echo "[replica] starting PostgreSQL in hot standby mode..."
exec su-exec postgres postgres \
  -c max_connections=200 \
  -c shared_buffers=256MB \
  -c hot_standby=on \
  -c hot_standby_feedback=on \
  -c wal_level=logical
