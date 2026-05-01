#!/bin/bash
# PostgreSQL initialization script
# Creates additional schemas/roles if needed beyond the default POSTGRES_DB

set -e

psql -v ON_ERROR_STOP=1 --username "$POSTGRES_USER" --dbname "$POSTGRES_DB" <<-EOSQL
  -- Create extensions
  CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
  CREATE EXTENSION IF NOT EXISTS "pg_trgm";

  -- UUID v7 generator (time-sortable, better index performance than v4)
  CREATE OR REPLACE FUNCTION uuid_generate_v7()
  RETURNS uuid
  AS $$
  DECLARE
    unix_ts_ms bytea;
    uuid_bytes bytea;
  BEGIN
    unix_ts_ms = substring(int8send(floor(extract(epoch FROM clock_timestamp()) * 1000)::bigint) FROM 3);
    uuid_bytes = uuid_send(gen_random_uuid());
    uuid_bytes = overlay(uuid_bytes placing unix_ts_ms FROM 1 FOR 6);
    uuid_bytes = set_byte(uuid_bytes, 6, (b'0111' || get_byte(uuid_bytes, 6)::bit(4))::bit(8)::int);
    RETURN encode(uuid_bytes, 'hex')::uuid;
  END
  $$ LANGUAGE plpgsql VOLATILE;

  -- Grant privileges
  GRANT ALL PRIVILEGES ON DATABASE $POSTGRES_DB TO $POSTGRES_USER;

  -- Replication user for streaming replication (read replica)
  CREATE USER replicator WITH REPLICATION ENCRYPTED PASSWORD '${REPLICATION_PASSWORD:-replicator_pw_change_me}';
EOSQL

# Allow replication connections from Docker network (172.0.0.0/8)
echo "host replication replicator all md5" >> "$PGDATA/pg_hba.conf"

# Reload pg_hba.conf so the new replication entry takes effect
psql -v ON_ERROR_STOP=1 --username "$POSTGRES_USER" --dbname "$POSTGRES_DB" \
  -c "SELECT pg_reload_conf();"

echo "PostgreSQL initialized successfully"
