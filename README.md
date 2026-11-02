# Movie Rater API

A production-quality full-stack application for couples to track movies, rate them, and write reviews.

## Prerequisites

- [Docker](https://docs.docker.com/get-docker/) and [Docker Compose](https://docs.docker.com/compose/install/)
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) (optional, for running migrations locally)
- [Entity Framework CLI](https://learn.microsoft.com/ef/core/cli/dotnet) (optional, for running migrations locally)

## Running the project

```bash
docker compose up --build
```

This starts two services:

| Service | Container name    | Ports              |
| ------- | ----------------- | ------------------ |
| **API** | `movie-rater-api` | `5056:8080` (HTTP) |
| **DB**  | `movie-rater-db`  | `127.0.0.1:5432:5432` (loopback only) |

The API is available at `http://localhost:5056`.

The API health endpoint is at `http://localhost:5056/api/health`.

## Running tests

### Unit tests

Unit tests use an in-memory EF Core database and mocked dependencies. No external services required.

```bash
dotnet test MovieRaterApi.Tests --filter "FullyQualifiedName~Unit"
```

### Integration tests

Integration tests use [Testcontainers](https://testcontainers.com/) to spin up a real PostgreSQL 17 container. Docker must be installed and running.

```bash
dotnet test MovieRaterApi.Tests --filter "FullyQualifiedName~Integration"
```

### All tests

```bash
dotnet test MovieRaterApi.Tests
```

> **Note:** Integration tests require Docker and may take ~30s to complete due to container startup. The test container is automatically cleaned up after execution.

## Running database migrations

Migrations are **not** applied automatically on startup. You must apply them manually.

### Option 1 — Run migrations inside the API container

```bash
# Start the services
docker compose up -d

# Create the initial migration (if not already created)
docker compose run --rm api dotnet ef migrations add InitialCreate

# Apply migrations to the database
docker compose run --rm api dotnet ef database update
```

### Option 2 — Run migrations locally against the Docker database

```bash
# Start only the database
docker compose up -d db

# Create the initial migration
dotnet ef migrations add InitialCreate --project MovieRaterApi

# Apply migrations
dotnet ef database update --project MovieRaterApi \
  --connection-string "Host=localhost;Port=5432;Database=movierater;Username=postgres;Password=postgres"
```

### Full workflow (first time setup)

```bash
# 1. Start the database
docker compose up -d db

# 2. Create the initial migration
dotnet ef migrations add InitialCreate --project MovieRaterApi

# 3. Apply migrations
dotnet ef database update --project MovieRaterApi \
  --connection-string "Host=localhost;Port=5432;Database=movierater;Username=postgres;Password=postgres"

# 4. Start the full stack
docker compose up --build
```

## Creating new migrations

After modifying entity classes or the `DbContext`:

```bash
docker compose run --rm api dotnet ef migrations add MigrationName
```

Or locally:

```bash
dotnet ef migrations add MigrationName --project MovieRaterApi
```

Then apply them:

```bash
docker compose run --rm api dotnet ef database update
```

## Production configuration and security

Production runs with `docker compose -f docker-compose.yml -f docker-compose.tunnel.yml` (see `deploy.sh`).
The tunnel overlay sets `ASPNETCORE_ENVIRONMENT=Production` and **requires** these values in `.env`:

| Variable                  | Purpose                                                                                     |
| ------------------------- | ------------------------------------------------------------------------------------------- |
| `JWT_SIGNING_KEY`         | HMAC key that signs access tokens. Generate one with `openssl rand -base64 48`.             |
| `CLOUDFLARE_TUNNEL_TOKEN` | Cloudflare tunnel credentials.                                                              |

The API refuses to start outside Development when `Jwt:SigningKey` is missing, shorter than 32 bytes, or
equal to the public development placeholder in `appsettings.Development.json`. Rotating the key signs every
user out of their current access token; refresh tokens keep working, so sessions renew transparently.

Other security-relevant settings (`appsettings.json`):

| Setting                         | Default                            | Purpose                                                                                     |
| ------------------------------- | ---------------------------------- | ------------------------------------------------------------------------------------------- |
| `Cors:AllowedOrigins`           | `https://movie-rater.leopo.dev`    | Origins allowed to call the API with credentials. Development adds `http://localhost:5173`. |
| `TrustedProxies:KnownNetworks`  | `172.16.0.0/12` (Docker bridges)   | Proxies whose `X-Forwarded-For` is trusted. Rate limits key on the resulting client IP.     |

Authentication and authorization rules:

- The refresh token is only ever sent as the HttpOnly, `Secure`, `SameSite=Lax`, host-only `mr_refresh` cookie; it
  never appears in a response body. Refresh tokens rotate on every use. Presenting an already-rotated token more
  than 30 seconds after its rotation is treated as theft and revokes every session of that user. Within 30 seconds
  it is treated as two tabs refreshing at once and only rejected.
- Resetting a password revokes all refresh tokens and all outstanding reset links of that user.
- `register`, `login` and `reset-password` are limited to 10 requests per minute per client IP.
  `forgot-password` has its own per-IP, per-email and global limits (`RateLimiting` section).
- Groups, and everything inside them (watch sessions, ratings, dashboard, heatmap), are only reachable by
  group members. Personal watch sessions are only reachable by their creator. Fetching, renaming or deleting a
  group, a watch session or its ratings that you cannot see answers `404`, so ids cannot be probed; acting on a
  group you do not belong to (create or rate a session, dashboard, heatmap) answers `403`.

The frontend nginx adds a strict Content-Security-Policy and other security headers
(`movie-rater-fe/nginx/security-headers.conf`) and takes the client IP from Cloudflare's `CF-Connecting-IP`
only when the request comes from the Docker network.

## Admin panel

A read-only admin panel is available at `/admin` for the user IDs listed in the `Admin:UserIds` configuration.
Everyone else gets `403` from the admin endpoints and never sees the sidebar entry.

To grant yourself access, put your user ID (the `id` returned by `GET /api/auth/me`) in `.env`:

```bash
ADMIN_USER_ID=00000000-0000-0000-0000-000000000000
```

`docker compose` passes it to the API as `Admin__UserIds__0`. Add more admins with `Admin__UserIds__1`, `Admin__UserIds__2`, …

| Endpoint                     | Description                                                                                                         |
| ---------------------------- | ------------------------------------------------------------------------------------------------------------------- |
| `GET /api/admin/access`      | `{ isAdmin }` for the current user. Open to every signed-in user.                                                   |
| `GET /api/admin/overview`    | Users, active users (24h / 7d / 30d), groups, cached media, watch sessions, ratings, invitation totals.             |
| `GET /api/admin/activity`    | Daily signups, active users, watch sessions and ratings. `?days=7..365` (default 30).                               |
| `GET /api/admin/groups`      | Every group with members, session and rating counts, last activity and pending invitations.                         |
| `GET /api/admin/invitations` | Invitations, newest first. `?status=Pending\|Accepted\|Expired\|Revoked`, `?search=` (email, username, group), `?limit=1..500`. |

Notes:

- `overview` and `activity` are cached in memory for 5 minutes. Pass `?refresh=true` to recompute them.
- "Active users" counts distinct users who logged in or refreshed a token in the window, since every token refresh stores a row.
- Invitations have an `effectiveStatus`: a `Pending` invitation past its `expiresAt` is reported as `Expired`.
  Each row also lists `issues` that explain why an invite might not work:
  `ExpiredButPending`, `GroupMissing` (the group was deleted), `InviteeAlreadyMember` and `DuplicatePending`.

## Installing the web app (PWA)

The frontend is a Progressive Web App, so it can be installed from the browser and launched like a native app.
Installation requires the site to be served over HTTPS (or `localhost`).

- **Android (Chrome, Edge, Samsung Internet):** use the **Install app** button in the sidebar, or the browser menu → *Install app* / *Add to Home screen*.
- **iOS / iPadOS (Safari):** tap **Share** → **Add to Home Screen**. The **Install app** button in the sidebar shows these steps.
- **Desktop (Chrome, Edge):** use the **Install app** button in the sidebar, or the install icon in the address bar.

How it works:

- `vite-plugin-pwa` generates `manifest.webmanifest` and a Workbox service worker (`sw.js`) at build time.
  The service worker is only built for production (`pnpm build` / the Docker image), not in `pnpm dev`.
- The app shell (JS, CSS, fonts, icons) is precached. TMDB posters are cached on first view. `/api` requests always go to the network.
- When a new version is deployed, a toast offers to reload instead of reloading on its own, so unsaved forms are not lost.
- nginx serves `sw.js` and the manifest with `Cache-Control: no-cache`, so browsers pick up new deploys.
- App icons in `movie-rater-fe/public` are rendered from the SVG sources in `movie-rater-fe/pwa-assets`
  (`icon.svg` for the regular icons and the favicon, `icon-maskable.svg` for the maskable and Apple touch icons).

## Tech stack

| Layer      | Technology            |
| ---------- | --------------------- |
| Framework  | ASP.NET 10            |
| ORM        | Entity Framework Core |
| Database   | PostgreSQL 17         |
| Logging    | Serilog               |
| Validation | FluentValidation      |
