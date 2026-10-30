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
| **DB**  | `movie-rater-db`  | `5432:5432`        |

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
