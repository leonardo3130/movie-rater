# AGENTS.md

## Project

**Working title:** Movie Rater

A production-quality full-stack application that allows couples to track movies
they've watched together, rate them independently, write reviews, unlock
achievements, visualize statistics, and receive AI-generated summaries of their
opinions.

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
- Zustand
- React Router
- Framer Motion

## Backend

- ASP.NET 9
- Entity Framework Core
- PostgreSQL
- JWT Authentication
- FluentValidation
- TMDB API
- OpenAI API
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

IAchievementService
AchievementService
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

## Movie

```
Id

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

Only cache TMDB fields that are actually required.

---

## Genre

```
Id

TmdbId

Name
```

---

## MovieGenre

Composite PK

```
MovieId
GenreId
```

---

## WatchSession

Represents one movie night.

```
Id

CoupleId
MovieId

WatchedAt

Location
Notes

CreatedByUserId

CreatedAt
UpdatedAt
```

Watching the same movie twice creates multiple sessions.

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

## UserMovie

Represents a user's relationship with a movie.

```
UserId
MovieId

IsFavorite
IsInWatchlist

CreatedAt
UpdatedAt
```

Composite PK

```
(UserId, MovieId)
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

## Achievement

```
Id

Name
Description

Icon

Points
```

---

## UserAchievement

Composite PK

```
UserId
AchievementId

UnlockedAt
```

---

## AiSummary

```
Id

WatchSessionId

Summary

GeneratedAt
```

Generated only after both reviews exist.

Only regenerated if one of the reviews changes.

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

## Watch Sessions

- Mark movie as watched
- Watch date
- Location
- Notes

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

# Achievements

Examples

- First Movie Together
- 10 Movies
- 50 Movies
- 100 Movies
- Weekend Warrior
- Horror Fan
- Sci-Fi Lover
- Romance Month
- Movie Marathon
- Anniversary Movie

Achievements should be computed automatically.

---

# AI Features

After both users submit reviews, generate

- similarities
- disagreements
- overall opinion

Store the generated text.

Never regenerate unless reviews change.

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

- Achievement calculation
- Dashboard statistics
- Rating compatibility
- AI prompt generation
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
- Unlocking achievements
- Dashboard statistics
- AI summary generation

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
- AI requests
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
├── WatchSessions
├── Ratings
├── Dashboard
├── Achievements
├── UserMovie
└── AI
```

Each feature should be self-contained.

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
- AI recommendations
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
