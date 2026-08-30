# Movie Rater API

A production-quality full-stack application for couples to track movies, rate them, write reviews, and unlock achievements.

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

## Tech stack

| Layer      | Technology            |
| ---------- | --------------------- |
| Framework  | ASP.NET 10            |
| ORM        | Entity Framework Core |
| Database   | PostgreSQL 17         |
| Logging    | Serilog               |
| Validation | FluentValidation      |
| API docs   | Scalar (OpenAPI)      |

# TODO

- password recovery
- improve invitation system
