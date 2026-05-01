#!/bin/bash
set -e

# --- Configuration ---
PGDATA="/var/lib/postgresql/data"
DB_NAME="uis_db"
DB_USER="postgres"
DB_PASS="password123"

# Ensure run directory exists for PG sockets
mkdir -p /run/postgresql
chown -R postgres:postgres /run/postgresql

# Cleanup leftover PG PID file if exists (prevents start failures after crash)
rm -f "$PGDATA/postmaster.pid"

# --- Start Redis ---
echo "Starting Redis..."
redis-server --daemonize yes

# --- Initialize PostgreSQL ---
if [ ! -s "$PGDATA/PG_VERSION" ]; then
    echo "Initializing PostgreSQL data directory..."
    mkdir -p "$PGDATA"
    chown -R postgres:postgres "$PGDATA"
    su postgres -c "initdb -D $PGDATA"
    
    # Allow local connections
    echo "host all all 0.0.0.0/0 md5" >> "$PGDATA/pg_hba.conf"
    echo "host all all 127.0.0.1/32 trust" >> "$PGDATA/pg_hba.conf"
    echo "listen_addresses='*'" >> "$PGDATA/postgresql.conf"
fi

echo "Starting PostgreSQL..."
# Start PG in background and redirect output to a file we can tail if it fails
su postgres -c "postgres -D $PGDATA" > /var/log/postgresql/postgresql.log 2>&1 &

# Wait for PG to start
for i in {1..30}; do
    if su postgres -c "pg_isready" ; then
        echo "PostgreSQL is ready!"
        break
    fi
    echo "Waiting for PostgreSQL to be ready... ($i)"
    sleep 2
    if [ $i -eq 30 ]; then
        echo "PostgreSQL failed to start. Logs:"
        cat /var/log/postgresql/postgresql.log
        exit 1
    fi
done

# --- Create Database and User ---
echo "Ensuring database and user exist..."
su postgres -c "psql -c \"CREATE DATABASE $DB_NAME;\"" || true
su postgres -c "psql -c \"ALTER USER postgres WITH PASSWORD '$DB_PASS';\"" || true

# --- Load Environment Variables from .env ---
if [ -f .env ]; then
    echo "Loading variables from .env..."
    # Use a while loop to handle values with spaces correctly
    while IFS='=' read -r key value; do
        # Skip comments and empty lines
        [[ $key =~ ^#.* ]] || [[ -z $key ]] && continue
        export "$key=$value"
    done < .env
fi

# --- Start ASP.NET Core App ---
echo "Starting Uis.Server on port ${PORT:-80}..."
# Override connection string to point to localhost
export ConnectionStrings__DefaultConnection="Host=localhost;Port=5432;Database=$DB_NAME;Username=postgres;Password=$DB_PASS"
export ASPNETCORE_URLS="http://+:${PORT:-80}"

exec dotnet Uis.Server.dll
