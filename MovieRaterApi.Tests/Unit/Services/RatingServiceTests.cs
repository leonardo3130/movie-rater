using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using MovieRaterApi.Data;
using MovieRaterApi.Data.Entities;
using MovieRaterApi.Features.Ratings.DTOs;
using MovieRaterApi.Features.Ratings.Services;
using MovieRaterApi.Infrastructure.Exceptions;

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
        var movieId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        SeedMovie(movieId, 1, "Inception");
        var ids = SeedGroupWithUsers(3, "user");
        var groupId = ids[0];
        var userId = ids[1];
        var partnerId = ids[2];
        SeedWatchSession(sessionId, groupId, movieId, userId);

        var request = new CreateRatingRequestDto { RatingValue = 8, Review = "Great movie!" };

        var result = await _sut.CreateAsync(sessionId, request, userId);

        result.RatingValue.Should().Be(8);
        result.Review.Should().Be("Great movie!");
        result.UserId.Should().Be(userId);
        result.Username.Should().Be("user 0");
        _db.Ratings.Count().Should().Be(1);
    }

    [Fact]
    public async Task CreateAsync_Throws_WhenSessionNotFound()
    {
        var request = new CreateRatingRequestDto { RatingValue = 8 };

        await FluentActions
            .Awaiting(() => _sut.CreateAsync(Guid.NewGuid(), request, Guid.NewGuid()))
            .Should()
            .ThrowAsync<NotFoundException>()
            .WithMessage("Watch session not found.");
    }

    [Fact]
    public async Task CreateAsync_Throws_WhenUserNotInGroup()
    {
        var sessionId = Guid.NewGuid();
        var movieId = Guid.NewGuid();
        SeedMovie(movieId, 1, "Inception");
        var ids = SeedGroupWithUsers(3, "member");
        var groupId = ids[0];
        var userId = ids[1];
        var partnerId = ids[2];
        var strangerId = Guid.NewGuid();
        SeedWatchSession(sessionId, groupId, movieId, userId);

        var request = new CreateRatingRequestDto { RatingValue = 8 };

        await FluentActions
            .Awaiting(() => _sut.CreateAsync(sessionId, request, strangerId))
            .Should()
            .ThrowAsync<ForbiddenException>()
            .WithMessage("You are not part of the group for this watch session.");
    }

    [Fact]
    public async Task CreateAsync_Throws_WhenAlreadyRated()
    {
        var movieId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        SeedMovie(movieId, 1, "Inception");
        var ids = SeedGroupWithUsers(3, "member");
        var groupId = ids[0];
        var userId = ids[1];
        var partnerId = ids[2];
        SeedWatchSession(sessionId, groupId, movieId, userId);
        SeedRating(sessionId, userId, 8);

        var request = new CreateRatingRequestDto { RatingValue = 9 };

        await FluentActions
            .Awaiting(() => _sut.CreateAsync(sessionId, request, userId))
            .Should()
            .ThrowAsync<ConflictException>()
            .WithMessage("You have already rated this watch session.");
    }

    [Fact]
    public async Task UpdateAsync_UpdatesRatingAndReview()
    {
        var movieId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        SeedMovie(movieId, 1, "Inception");
        var ids = SeedGroupWithUsers(3, "member");
        var groupId = ids[0];
        var userId = ids[1];
        var partnerId = ids[2];
        SeedWatchSession(sessionId, groupId, movieId, userId);
        SeedRating(sessionId, userId, 5);

        var request = new UpdateRatingRequestDto { RatingValue = 9, Review = "Actually amazing!" };

        var result = await _sut.UpdateAsync(sessionId, request, userId);

        result.RatingValue.Should().Be(9);
        result.Review.Should().Be("Actually amazing!");
    }

    [Fact]
    public async Task GetBySessionAsync_ReturnsBothRatings()
    {
        var movieId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var ids = SeedGroupWithUsers(3, "member");
        var groupId = ids[0];
        var userId = ids[1];
        var partnerId = ids[2];
        SeedWatchSession(sessionId, groupId, movieId, userId);
        SeedRating(sessionId, userId, 8);
        SeedRating(sessionId, partnerId, 7);

        var result = await _sut.GetBySessionAsync(sessionId);

        result.Ratings.Should().HaveCount(2);
        result.Ratings.Should().Contain(r => r.Username == "member 0" && r.RatingValue == 8);
        result.Ratings.Should().Contain(r => r.Username == "member 1" && r.RatingValue == 7);
    }

    private void SeedUserGroup(Guid groupId, Guid userId)
    {
        _db.UserGroups.Add(
            new UserGroup
            {
                Id = Guid.NewGuid(),
                GroupId = groupId,
                UserId = userId,
            }
        );
        _db.SaveChanges();
    }

    private void SeedUser(Guid id, string username)
    {
        _db.Users.Add(
            new User
            {
                Id = id,
                Username = username,
                Email = $"{username}@test.com",
                PasswordHash = "h",
            }
        );
        _db.SaveChanges();
    }

    private void SeedGroup(Guid groupId)
    {
        _db.Groups.Add(new Group { Id = groupId, CreatedAt = DateTime.UtcNow });
        _db.SaveChanges();
    }

    private List<Guid> SeedGroupWithUsers(int userCount, string prefix)
    {
        var groupId = Guid.NewGuid();
        SeedGroup(groupId);
        var ids = new List<Guid>() { groupId };
        for (int i = 0; i < userCount; i++)
        {
            var username = $"{prefix} {i}";
            var userId = Guid.NewGuid();
            SeedUser(userId, username);
            SeedUserGroup(groupId, userId);

            ids.Add(userId);
        }

        return ids;
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

    private void SeedWatchSession(Guid sessionId, Guid groupId, Guid movieId, Guid createdByUserId)
    {
        _db.WatchSessions.Add(
            new WatchSession
            {
                Id = sessionId,
                GroupId = groupId,
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
