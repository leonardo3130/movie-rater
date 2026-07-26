using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using MovieRaterApi.Data;
using MovieRaterApi.Data.Entities;
using MovieRaterApi.Features.Ratings.DTOs;
using MovieRaterApi.Features.Ratings.Services;

namespace MovieRaterApi.Tests.Unit.Services;

public class RatingServiceTests
{
    private readonly ApplicationDbContext _db;
    private readonly Mock<ILogger<RatingService>> _loggerMock;
    private readonly RatingService _sut;

    public RatingServiceTests()
    {
        _db = TestHelpers.CreateInMemoryDbContext();
        _loggerMock = new Mock<ILogger<RatingService>>();
        _sut = new RatingService(_db, _loggerMock.Object);
    }

    [Fact]
    public async Task CreateAsync_CreatesRating_WhenValid()
    {
        var coupleId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var partnerId = Guid.NewGuid();
        var movieId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        SeedUser(userId, "user1");
        SeedUser(partnerId, "user2");
        SeedMovie(movieId, 1, "Inception");
        SeedCouple(coupleId, userId, partnerId);
        SeedWatchSession(sessionId, coupleId, movieId, userId);

        var request = new CreateRatingRequestDto { RatingValue = 8, Review = "Great movie!" };

        var result = await _sut.CreateAsync(sessionId, request, userId);

        result.RatingValue.Should().Be(8);
        result.Review.Should().Be("Great movie!");
        result.UserId.Should().Be(userId);
        result.Username.Should().Be("user1");
        _db.Ratings.Count().Should().Be(1);
    }

    [Fact]
    public async Task CreateAsync_Throws_WhenSessionNotFound()
    {
        var request = new CreateRatingRequestDto { RatingValue = 8 };

        await FluentActions
            .Awaiting(() => _sut.CreateAsync(Guid.NewGuid(), request, Guid.NewGuid()))
            .Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage("Watch session not found.");
    }

    [Fact]
    public async Task CreateAsync_Throws_WhenUserNotInCouple()
    {
        var sessionId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var strangerId = Guid.NewGuid();
        var partnerId = Guid.NewGuid();
        var coupleId = Guid.NewGuid();
        var movieId = Guid.NewGuid();
        SeedUser(userId, "owner");
        SeedUser(strangerId, "stranger");
        SeedUser(partnerId, "partner");
        SeedMovie(movieId, 1, "Inception");
        SeedCouple(coupleId, userId, partnerId);
        SeedWatchSession(sessionId, coupleId, movieId, userId);

        var request = new CreateRatingRequestDto { RatingValue = 8 };

        await FluentActions
            .Awaiting(() => _sut.CreateAsync(sessionId, request, strangerId))
            .Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage("You are not part of the couple for this watch session.");
    }

    [Fact]
    public async Task CreateAsync_Throws_WhenAlreadyRated()
    {
        var coupleId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var partnerId = Guid.NewGuid();
        var movieId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        SeedUser(userId, "user1");
        SeedUser(partnerId, "user2");
        SeedMovie(movieId, 1, "Inception");
        SeedCouple(coupleId, userId, partnerId);
        SeedWatchSession(sessionId, coupleId, movieId, userId);
        SeedRating(sessionId, userId, 8);

        var request = new CreateRatingRequestDto { RatingValue = 9 };

        await FluentActions
            .Awaiting(() => _sut.CreateAsync(sessionId, request, userId))
            .Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage("You have already rated this watch session.");
    }

    [Fact]
    public async Task UpdateAsync_UpdatesRatingAndReview()
    {
        var coupleId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var partnerId = Guid.NewGuid();
        var movieId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        SeedUser(userId, "user1");
        SeedUser(partnerId, "user2");
        SeedMovie(movieId, 1, "Inception");
        SeedCouple(coupleId, userId, partnerId);
        SeedWatchSession(sessionId, coupleId, movieId, userId);
        SeedRating(sessionId, userId, 5);

        var request = new UpdateRatingRequestDto { RatingValue = 9, Review = "Actually amazing!" };

        var result = await _sut.UpdateAsync(sessionId, request, userId);

        result.RatingValue.Should().Be(9);
        result.Review.Should().Be("Actually amazing!");
    }

    [Fact]
    public async Task GetBySessionAsync_ReturnsBothRatings()
    {
        var coupleId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var partnerId = Guid.NewGuid();
        var movieId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        SeedUser(userId, "alice");
        SeedUser(partnerId, "bob");
        SeedMovie(movieId, 1, "Inception");
        SeedCouple(coupleId, userId, partnerId);
        SeedWatchSession(sessionId, coupleId, movieId, userId);
        SeedRating(sessionId, userId, 8);
        SeedRating(sessionId, partnerId, 7);

        var result = await _sut.GetBySessionAsync(sessionId, coupleId);

        result.Ratings.Should().HaveCount(2);
        result.Ratings.Should().Contain(r => r.Username == "alice" && r.RatingValue == 8);
        result.Ratings.Should().Contain(r => r.Username == "bob" && r.RatingValue == 7);
    }

    private void SeedUser(Guid userId, string username)
    {
        _db.Users.Add(
            new User
            {
                Id = userId,
                Username = username,
                Email = $"{username}@test.com",
                PasswordHash = "h",
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

    private void SeedCouple(Guid coupleId, Guid user1Id, Guid user2Id)
    {
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

    private void SeedWatchSession(Guid sessionId, Guid coupleId, Guid movieId, Guid createdByUserId)
    {
        _db.WatchSessions.Add(
            new WatchSession
            {
                Id = sessionId,
                CoupleId = coupleId,
                MovieId = movieId,
                WatchedAt = DateTime.UtcNow,
                CreatedByUserId = createdByUserId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            }
        );
        _db.SaveChanges();
    }

    private void SeedRating(Guid watchSessionId, Guid userId, int ratingValue)
    {
        _db.Ratings.Add(
            new Rating
            {
                Id = Guid.NewGuid(),
                WatchSessionId = watchSessionId,
                UserId = userId,
                RatingValue = ratingValue,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            }
        );
        _db.SaveChanges();
    }
}
