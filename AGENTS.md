# AGENTS.md

## Project

**Working title:** Movie Rater

A production-quality full-stack application that allows couples to track movies
they've watched together, rate them independently, write reviews, and visualize
statistics together.

The goal is to build software that resembles what would be developed inside a
professional engineering team rather than simply completing a portfolio project.

---

# Tech Stack

## Frontend

- React
- TypeScript
- Vite
- TailwindCSS
- shadcn/ui
- TanStack Query
- Axios
- Zustand
- React Router
- Framer Motion
- Zod

## Backend

- ASP.NET 9
- Entity Framework Core
- PostgreSQL
- JWT Authentication
- FluentValidation
- TMDB API
- Serilog

## Infrastructure

- Docker
- GitHub Actions
- Azure App Service
- Azure Blob Storage (future)

---

# Backend Architecture

The backend follows the **MVC pattern**.

```
Controllers
      │
      ▼
Service Interfaces
      │
      ▼
Service Implementations
      │
      ▼
DbContext
      │
      ▼
PostgreSQL
```

## Responsibilities

### Controllers

Responsible only for

- routing
- validation
- authentication
- converting HTTP requests/responses

Controllers must never contain business logic.

---

### Services

Contain all business logic.

Services communicate directly with the EF Core DbContext.

Repository classes are intentionally **not used** since Entity Framework Core
already implements Repository + Unit of Work patterns.

---

### Interfaces

Every service must expose an interface.

Example

```
IMovieService
MovieService

IRatingService
RatingService

IDashboardService
DashboardService
```

This keeps the application easy to mock and unit test.

---

# Database

## User

```
Id
Username
Email
PasswordHash
ProfilePictureUrl

CreatedAt
UpdatedAt
```

---

## Couple

```
Id

User1Id
User2Id

CreatedAt
```

Exactly two users.

---

## Media

Single table (TPH) shared by all watchable/collectable items.

```
Id
MediaType            (Movie, TvSeries, TvSeason, TvEpisode)

TmdbId

Title

PosterUrl
BackdropUrl

Overview

ReleaseDate
Runtime

AverageTmdbRating

CreatedAt
UpdatedAt
```

Unique constraint

```
(MediaType, TmdbId)
```

Only cache TMDB fields that are actually required.

### Movie

Subclass of `Media`. No extra fields today.

### TvSeries

Subclass of `Media`. `ReleaseDate` holds the first air date.

```
NumberOfSeasons
NumberOfEpisodes
LastAirDate
Status
Type
```

### TvSeason

Subclass of `Media`. `Title` holds "Season N".

```
SeasonNumber
SeriesId
```

### TvEpisode

Subclass of `Media`. `PosterUrl` holds the episode still.

```
EpisodeNumber
SeasonNumber
SeasonId
```

Episodes reach their series through `Season.SeriesId`.

---

## Genre

```
Id

TmdbId

Name
```

---

## MediaGenre

Composite PK

```
MediaId
GenreId
```

---

## WatchSession

Represents one movie night (or one episode watched).

```
Id

GroupId
MediaId

WatchedAt

Location
Notes

CreatedByUserId

CreatedAt
UpdatedAt
```

Watching the same title twice creates multiple sessions.

---

## Rating

```
Id

WatchSessionId

UserId

Rating
Review

CreatedAt
UpdatedAt
```

Unique constraint

```
(WatchSessionId, UserId)
```

Each user can review a watch session only once.

---

## UserMedia

Represents a user's relationship with a media item (movie, series, season or episode).

```
UserId
MediaId

IsFavorite
IsInWatchlist

CreatedAt
UpdatedAt
```

Composite PK

```
(UserId, MediaId)
```

This design allows

- favorite
- watchlist

simultaneously.

Future flags may include

- Hidden
- Recommended
- Ignored

---

# Core Features

## Authentication

- Register
- Login
- JWT
- Refresh Tokens
- Invite Partner

---

## Movies

- Search via TMDB
- View details
- Genres
- Posters
- Runtime

---

## TV Shows

- Search via TMDB (series)
- View series details (seasons)
- View season details (episodes)
- View episode details
- TV genres

Series are cached with their genres. Seasons and episodes are cached when
visited. `TvSeries.Runtime` is intentionally left unset (runtimes live per
episode). Genres live only on the series row; Dashboard genre stats resolve
watched seasons/episodes up to their series.

---

## Watch Sessions

- Mark a movie or episode as watched
- Watch date
- Location
- Notes

Sessions accept only `Movie` and `TvEpisode` media (`MediaType` is validated on
create; seasons and series are reserved for a later phase). Responses carry
`MediaType` plus `SeriesTitle` / `SeasonNumber` / `EpisodeNumber` when the
session refers to a TV episode.

---

## Ratings

Each partner can

- rate
- edit rating
- edit review

---

## UserMovie

Users can

- add/remove favorite
- add/remove watchlist

---

# Dashboard

Statistics

- Movies watched
- Movies this month
- Movies this year
- Average rating
- Favorite genres
- Most watched genres
- Highest rated movie
- Lowest rated movie
- Biggest disagreement
- Average disagreement
- Rewatch count
- Current streak
- Longest streak

---

# Heatmap

GitHub-style activity heatmap.

Generated from

```
WatchSession.WatchedAt
```

---

# Development Guidelines

## General

- Keep code simple.
- Prefer readability.
- Build vertical slices.
- Avoid premature optimization.
- Follow SOLID principles.
- Favor composition over inheritance.

---

## Entity Framework

- Use EF Core directly.
- Do **not** implement repositories.
- Always use LINQ.
- Always use asynchronous APIs.
- Always create database migrations.
- Never manually modify the database schema.

---

## Dependency Injection

Every service must expose an interface.

Bad

```
MovieController
    ↓
MovieService
```

Good

```
MovieController
    ↓
IMovieService
    ↓
MovieService
```

---

## Testing

### Unit Tests

Every **non-trivial method** must have unit tests.

Examples

- Dashboard statistics
- Rating compatibility
- Validation logic

Mock every dependency.

---

### Integration Tests

Every **non-trivial flow** must have integration tests.

Examples

- User registration
- Login
- Couple invitation
- Rating a movie
- Creating a watch session
- Dashboard statistics

Integration tests should execute the complete HTTP request pipeline whenever practical.

---

## Logging

Use **Serilog**.

Outputs

- Console
- Rolling log files

Every important action should be logged.

Examples

- Login
- Registration
- API errors
- Exceptions
- External API failures

Use structured logging.

Good

```
User {UserId} rated movie {MovieId} with {Rating}
```

Bad

```
User rated movie
```

---

## Configuration

Use

```
appsettings.json
appsettings.Development.json
```

Never hardcode

- API keys
- connection strings
- JWT secrets

Use the Options pattern for configuration classes.

Do not call external TMDB configuration or API endpoints when response data does
not require them (for example, no poster/backdrop paths); use fallback behavior
and ensure tests/CI either mock TMDB or rely on fallback paths.

---

## Docker

Development must work identically on every platform.

Provide Docker Compose for

- API
- PostgreSQL

Running the project should require only

```
docker compose up
```

---

# Architecture Rules

## Vertical Slice Architecture

Organize the application by **feature**, not by technical layer.

Preferred structure:

```
Features
│
├── Authentication
├── Movies
├── TvShows
├── WatchSessions
├── Ratings
├── Dashboard
└── UserMovie
```

Each feature should be self-contained.

Shared cross-feature helpers live under `Features/Shared` (for example
`IMediaEnrichmentService`, which applies favorite / watchlist / watched-count
data to any media DTO implementing `IEnrichableMediaDto`).

Example:

```
Features
└── Ratings
    ├── Controllers
    ├── Services
    ├── Interfaces
    ├── DTOs
    ├── Validators
    └── Mapping
```

A developer working on a feature should rarely need to leave its folder.

---

# API Design

## DTOs

Never expose Entity Framework entities through the API.

Every endpoint must use DTOs for both request and response models.

Examples

```
CreateRatingRequestDto

CreateRatingResponseDto

MovieDetailsResponseDto

DashboardResponseDto
```

Entities remain internal to the domain.

---

## Validation

Every incoming DTO must be validated.

Use **FluentValidation**.

Validation must include:

- Required fields
- String lengths
- Numeric ranges
- Enum validation
- Date validation
- Business-rule validation where appropriate

Controllers should never contain validation logic.

Invalid requests must return appropriate HTTP validation responses.

---

# Development Standards

Every new feature should include:

- DTOs
- Validators
- Service interface
- Service implementation
- Controller
- Unit tests
- Integration tests
- Structured logging where appropriate

---

## Frontend

- Use TanStack Query for server state.
- Zustand only for client state.
- Manage forms and validation using Zod
- Components should remain small.
- Pages should orchestrate components.
- Avoid prop drilling.

---

# UI

Dark-first.

Inspirations

- Letterboxd
- Spotify
- GitHub

Animations should be subtle and smooth.

Movie posters should be the primary visual element.

---

# Future Features

- Streaming providers
- Movie recommendations
- Timeline memories
- Movie night photos
- Collections
- Public profiles
- Friend groups
- Year recap
- Mobile app

---

# Definition of Done

A feature is complete only if:

- Business logic is implemented
- Unit tests exist
- Integration tests exist
- Logging is added
- Validation is implemented
- Database migration is created (if needed)
- API documentation is updated
- Code follows project conventions
