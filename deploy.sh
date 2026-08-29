#!/bin/bash
set -Eeuo pipefail

APP_DIR="/home/leonardo/movie-rater"
cd "$APP_DIR"

# Prevent concurrent deployments
exec 9>/tmp/movie-rater-deploy.lock
flock -n 9 || {
  echo "Deployment already running"
  exit 1
}

echo "==> Stopping containers"
docker compose -f docker-compose.yml -f docker-compose.tunnel.yml down

echo "==> Building containers"
docker compose -f docker-compose.yml -f docker-compose.tunnel.yml build --no-cache

echo "==> Starting containers"
docker compose -f docker-compose.yml -f docker-compose.tunnel.yml up -d --force-recreate

echo "==> Waiting for database"

until docker compose \
  -f docker-compose.yml \
  -f docker-compose.tunnel.yml \
  exec -T db pg_isready -U postgres -d movierater; do
  sleep 2
done

cd "$APP_DIR/MovieRaterApi"

echo "==> Applying migrations"
dotnet ef database update \
  --connection "Host=localhost;Port=5432;Database=movierater;Username=postgres;Password=postgres"

echo "==> Restarting frontend"

PIDS=$(lsof -t -i:5173)

if [ -n "$PIDS" ]; then
  echo "Stopping existing frontend processes: $PIDS"
  kill $PIDS
  sleep 1
fi

cd "$APP_DIR/movie-rater-fe"

nohup pnpm dev --host 0.0.0.0 >frontend.log 2>&1 &

echo "==> Frontend started"
echo "==> Deployment complete"
