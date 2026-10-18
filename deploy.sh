#!/bin/bash
set -Eeuo pipefail

APP_DIR="/home/leonardo/movie-rater"
FRONTEND_PORT=5173
COMPOSE=(docker compose -f docker-compose.yml -f docker-compose.tunnel.yml)

cd "$APP_DIR"

# Prevent concurrent deployments
exec 9>/tmp/movie-rater-deploy.lock
flock -n 9 || {
  echo "Deployment already running"
  exit 1
}

echo "==> Stopping containers"
"${COMPOSE[@]}" down

echo "==> Building containers"
"${COMPOSE[@]}" build --no-cache

# The frontend used to run as a host Vite dev server; it must not hold the port the container binds.
# lsof exits 1 when nothing is listening, hence || true under set -e.
LEGACY_PIDS=$(lsof -t -i:"$FRONTEND_PORT" -sTCP:LISTEN || true)
if [ -n "$LEGACY_PIDS" ]; then
  echo "==> Stopping host process on port $FRONTEND_PORT: $LEGACY_PIDS"
  kill $LEGACY_PIDS || true
  sleep 2
fi

echo "==> Starting containers"
"${COMPOSE[@]}" up -d --force-recreate

echo "==> Waiting for database"

until "${COMPOSE[@]}" exec -T db pg_isready -U postgres -d movierater; do
  sleep 2
done

cd "$APP_DIR/MovieRaterApi"

echo "==> Applying migrations"
# 9>&- keeps lingering MSBuild/compiler server processes from holding the deploy lock
dotnet ef database update \
  --connection "Host=localhost;Port=5432;Database=movierater;Username=postgres;Password=postgres" 9>&-

cd "$APP_DIR"

echo "==> Waiting for frontend on port $FRONTEND_PORT"

for _ in $(seq 1 30); do
  curl -fsS -o /dev/null "http://localhost:$FRONTEND_PORT/" && break
  sleep 1
done

if ! curl -fsS -o /dev/null "http://localhost:$FRONTEND_PORT/"; then
  echo "Frontend failed to start, last log lines:"
  "${COMPOSE[@]}" logs --tail 30 frontend
  exit 1
fi

echo "==> Deployment complete"

exec 9>&-
