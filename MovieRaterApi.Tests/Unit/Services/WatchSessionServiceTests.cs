using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using MovieRaterApi.Data;
using MovieRaterApi.Data.Entities;
using MovieRaterApi.Features.WatchSessions.DTOs;
using MovieRaterApi.Features.WatchSessions.Services;

namespace MovieRaterApi.Tests.Unit.Services;

public class WatchSessionServiceTests
{
    private readonly ApplicationDbContext _db;
    private readonly Mock<ILogger<WatchSessionService>> _loggerMock;
    private readonly WatchSessionService _sut;

    public WatchSessionServiceTests()
    {
        _db = TestHelpers.CreateInMemoryDbContext();
        _loggerMock = new Mock<ILogger<WatchSessionService>>();
        _sut = new WatchSessionService(_db, _loggerMock.Object);
    }

    [Fact]
    public async Task CreateAsync_ReturnsSession_WhenValid()
    {
        var coupleId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var movieId = Guid.NewGuid();
        SeedCouple(coupleId, userId, Guid.NewGuid());
        SeedMovie(movieId, 1, "Inception");

        var request = new CreateWatchSessionRequestDto
        {
            MovieId = movieId,
            WatchedAt = new DateTime(2026, 1, 15, 20, 0, 0, DateTimeKind.Utc),
            Location = "Home",
            Notes = "Great movie!",
        };

        var result = await _sut.CreateAsync(request, userId, coupleId);

        result.MovieId.Should().Be(movieId);
        result.MovieTitle.Should().Be("Inception");
        result.Location.Should().Be("Home");
        result.Notes.Should().Be("Great movie!");
        result.CreatedByUserId.Should().Be(userId);
        _db.WatchSessions.Count().Should().Be(1);
    }

    [Fact]
    public async Task CreateAsync_Throws_WhenMovieNotFound()
    {
        var request = new CreateWatchSessionRequestDto
        {
            MovieId = Guid.NewGuid(),
            WatchedAt = DateTime.UtcNow,
        };

        await FluentActions
            .Awaiting(() => _sut.CreateAsync(request, Guid.NewGuid(), Guid.NewGuid()))
            .Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage("Movie not found.");
    }

    [Fact]
    public async Task GetAllAsync_ReturnsCoupleSessions()
    {
        var coupleId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var movieId = Guid.NewGuid();
        SeedCouple(coupleId, userId, Guid.NewGuid());
        SeedMovie(movieId, 1, "Inception");
        SeedWatchSession(
            Guid.NewGuid(),
            coupleId,
            movieId,
            userId,
            new DateTime(2026, 2, 1, 20, 0, 0, DateTimeKind.Utc)
        );
        SeedWatchSession(
            Guid.NewGuid(),
            coupleId,
            movieId,
            userId,
            new DateTime(2026, 1, 15, 20, 0, 0, DateTimeKind.Utc)
        );

        var query = new WatchSessionQueryDto { Page = 1, PageSize = 20 };
        var result = await _sut.GetAllAsync(query, coupleId);

        result.TotalCount.Should().Be(2);
        result.Items.Should().HaveCount(2);
        result
            .Items.First()
            .WatchedAt.Should()
            .Be(new DateTime(2026, 2, 1, 20, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsSessionWithMovieAndRatings()
    {
        var coupleId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var partnerId = Guid.NewGuid();
        var movieId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        SeedCouple(coupleId, userId, partnerId);
        SeedMovie(movieId, 1, "Inception");
        SeedWatchSession(sessionId, coupleId, movieId, userId, DateTime.UtcNow);
        SeedRating(sessionId, userId, 8, "Great");
        SeedRating(sessionId, partnerId, 7, "Good");

        var result = await _sut.GetByIdAsync(sessionId, coupleId);

        result.MovieTitle.Should().Be("Inception");
        result.Ratings.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetByIdAsync_Throws_WhenNotInCouple()
    {
        var sessionId = Guid.NewGuid();
        var otherCoupleId = Guid.NewGuid();
        SeedWatchSession(sessionId, otherCoupleId, Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow);

        await FluentActions
            .Awaiting(() => _sut.GetByIdAsync(sessionId, Guid.NewGuid()))
            .Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage("Watch session not found.");
    }

    [Fact]
    public async Task DeleteAsync_DeletesSession_WhenUserIsCreator()
    {
        var userId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        SeedWatchSession(sessionId, Guid.NewGuid(), Guid.NewGuid(), userId, DateTime.UtcNow);

        await _sut.DeleteAsync(sessionId, userId);

        _db.WatchSessions.Count().Should().Be(0);
    }

    [Fact]
    public async Task DeleteAsync_Throws_WhenUserIsNotCreator()
    {
        var sessionId = Guid.NewGuid();
        SeedWatchSession(
            sessionId,
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            DateTime.UtcNow
        );

        await FluentActions
            .Awaiting(() => _sut.DeleteAsync(sessionId, Guid.NewGuid()))
            .Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage("You can only delete your own watch sessions.");
    }

    [Fact]
    public async Task GetHeatmapAsync_ReturnsDailyCounts()
    {
        var coupleId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var movieId = Guid.NewGuid();
        SeedCouple(coupleId, userId, Guid.NewGuid());
        SeedMovie(movieId, 1, "Inception");
        SeedWatchSession(
            Guid.NewGuid(),
            coupleId,
            movieId,
            userId,
            new DateTime(2026, 7, 1, 20, 0, 0, DateTimeKind.Utc)
        );
        SeedWatchSession(
            Guid.NewGuid(),
            coupleId,
            movieId,
            userId,
            new DateTime(2026, 7, 1, 22, 0, 0, DateTimeKind.Utc)
        );
        SeedWatchSession(
            Guid.NewGuid(),
            coupleId,
            movieId,
            userId,
            new DateTime(2026, 7, 2, 20, 0, 0, DateTimeKind.Utc)
        );

        var result = await _sut.GetHeatmapAsync(30, coupleId);

        result.DailyCounts["2026-07-01"].Should().Be(2);
        result.DailyCounts["2026-07-02"].Should().Be(1);
        result.DailyCounts.Count.Should().Be(2);
    }

    private void SeedCouple(Guid coupleId, Guid user1Id, Guid user2Id)
    {
        if (!_db.Users.Any(u => u.Id == user1Id))
        {
            _db.Users.Add(
                new User
                {
                    Id = user1Id,
                    Username = "user1",
                    Email = "u1@test.com",
                    PasswordHash = "h",
                }
            );
        }
        if (!_db.Users.Any(u => u.Id == user2Id))
        {
            _db.Users.Add(
                new User
                {
                    Id = user2Id,
                    Username = "user2",
                    Email = "u2@test.com",
                    PasswordHash = "h",
                }
            );
        }
        _db.Couples.Add(
            new Couple
            {
                Id = coupleId,
                User1Id = user1Id,
                User2Id = user2Id,
                CreatedAt = DateTime.UtcNow,
            }
        );
        _db.SaveChanges();
    }

    private void SeedMovie(Guid movieId, int tmdbId, string title)
    {
        _db.Movies.Add(
            new Movie
            {
                Id = movieId,
                TmdbId = tmdbId,
                Title = title,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            }
        );
        _db.SaveChanges();
    }

    private void SeedWatchSession(
        Guid sessionId,
        Guid coupleId,
        Guid movieId,
        Guid createdByUserId,
        DateTime watchedAt
    )
    {
        _db.WatchSessions.Add(
            new WatchSession
            {
                Id = sessionId,
                CoupleId = coupleId,
                MovieId = movieId,
                WatchedAt = watchedAt,
                CreatedByUserId = createdByUserId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            }
        );
        _db.SaveChanges();
    }

    private void SeedRating(Guid watchSessionId, Guid userId, int ratingValue, string review)
    {
        if (!_db.Users.Any(u => u.Id == userId))
        {
            _db.Users.Add(
                new User
                {
                    Id = userId,
                    Username = $"user_{userId:N}",
                    Email = $"{userId:N}@test.com",
                    PasswordHash = "h",
                }
            );
            _db.SaveChanges();
        }
        _db.Ratings.Add(
            new Rating
            {
                Id = Guid.NewGuid(),
                WatchSessionId = watchSessionId,
                UserId = userId,
                RatingValue = ratingValue,
                Review = review,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            }
        );
        _db.SaveChanges();
    }
}
