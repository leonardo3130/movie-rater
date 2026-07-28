using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using MovieRaterApi.Data;
using MovieRaterApi.Data.Entities;
using MovieRaterApi.Features.Dashboard.Services;

namespace MovieRaterApi.Tests.Unit.Services;

public class DashboardServiceTests
{
    private readonly ApplicationDbContext _db;
    private readonly Mock<ILogger<DashboardService>> _loggerMock;
    private readonly DashboardService _sut;

    public DashboardServiceTests()
    {
        _db = TestHelpers.CreateInMemoryDbContext();
        _loggerMock = new Mock<ILogger<DashboardService>>();
        _sut = new DashboardService(_db, _loggerMock.Object);
    }

    [Fact]
    public async Task GetDashboardAsync_ReturnsZeroStats_WhenNoWatchSessions()
    {
        var coupleId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        SeedUser(userId, "user");
        SeedUser(Guid.NewGuid(), "partner");
        SeedCouple(coupleId, userId, Guid.NewGuid());

        var result = await _sut.GetDashboardAsync(coupleId);

        result.MoviesWatched.Should().Be(0);
        result.MoviesThisMonth.Should().Be(0);
        result.MoviesThisYear.Should().Be(0);
        result.AverageRating.Should().Be(0);
        result.FavoriteGenres.Should().BeEmpty();
        result.MostWatchedGenres.Should().BeEmpty();
        result.HighestRatedMovie.Should().BeNull();
        result.LowestRatedMovie.Should().BeNull();
        result.BiggestDisagreement.Should().BeNull();
        result.AverageDisagreement.Should().Be(0);
        result.RewatchCount.Should().Be(0);
        result.CurrentStreak.Should().Be(0);
        result.LongestStreak.Should().Be(0);
    }

    [Fact]
    public async Task GetDashboardAsync_ReturnsCorrectMoviesWatched()
    {
        var (coupleId, userId, partnerId) = SeedCoupleWithUsers();
        var movieId = SeedMovie(1, "Movie 1");
        var movie2Id = SeedMovie(2, "Movie 2");
        SeedSession(coupleId, movieId, userId, DateTime.UtcNow);
        SeedSession(coupleId, movie2Id, userId, DateTime.UtcNow);

        var result = await _sut.GetDashboardAsync(coupleId);

        result.MoviesWatched.Should().Be(2);
    }

    [Fact]
    public async Task GetDashboardAsync_FiltersThisMonth_ByDate()
    {
        var (coupleId, userId, partnerId) = SeedCoupleWithUsers();
        var movieId = SeedMovie(1, "M1");
        var movie2Id = SeedMovie(2, "M2");
        SeedSession(coupleId, movieId, userId, DateTime.UtcNow);
        SeedSession(coupleId, movie2Id, userId, DateTime.UtcNow.AddMonths(-2));

        var result = await _sut.GetDashboardAsync(coupleId);

        result.MoviesThisMonth.Should().Be(1);
    }

    [Fact]
    public async Task GetDashboardAsync_FiltersThisYear_ByDate()
    {
        var (coupleId, userId, partnerId) = SeedCoupleWithUsers();
        var movieId = SeedMovie(1, "M1");
        var movie2Id = SeedMovie(2, "M2");
        SeedSession(coupleId, movieId, userId, DateTime.UtcNow);
        SeedSession(coupleId, movie2Id, userId, DateTime.UtcNow.AddYears(-2));

        var result = await _sut.GetDashboardAsync(coupleId);

        result.MoviesThisYear.Should().Be(1);
    }

    [Fact]
    public async Task GetDashboardAsync_ComputesAverageRating()
    {
        var (coupleId, userId, partnerId) = SeedCoupleWithUsers();
        var movieId = SeedMovie(1, "M1");
        var sessionId = SeedSession(coupleId, movieId, userId, DateTime.UtcNow);
        SeedRating(sessionId, userId, 8);
        SeedRating(sessionId, partnerId, 6);

        var result = await _sut.GetDashboardAsync(coupleId);

        result.AverageRating.Should().Be(7.0);
    }

    [Fact]
    public async Task GetDashboardAsync_ReturnsHighestAndLowestRatedMovie()
    {
        var (coupleId, userId, partnerId) = SeedCoupleWithUsers();
        var m1 = SeedMovie(1, "High");
        var m2 = SeedMovie(2, "Low");
        var s1 = SeedSession(coupleId, m1, userId, DateTime.UtcNow);
        var s2 = SeedSession(coupleId, m2, userId, DateTime.UtcNow);
        SeedRating(s1, userId, 9);
        SeedRating(s1, partnerId, 9);
        SeedRating(s2, userId, 2);
        SeedRating(s2, partnerId, 2);

        var result = await _sut.GetDashboardAsync(coupleId);

        result.HighestRatedMovie.Should().NotBeNull();
        result.HighestRatedMovie!.Title.Should().Be("High");
        result.HighestRatedMovie.AverageRating.Should().Be(9.0);

        result.LowestRatedMovie.Should().NotBeNull();
        result.LowestRatedMovie!.Title.Should().Be("Low");
        result.LowestRatedMovie.AverageRating.Should().Be(2.0);
    }

    [Fact]
    public async Task GetDashboardAsync_ComputesRewatchCount()
    {
        var (coupleId, userId, partnerId) = SeedCoupleWithUsers();
        var movieId = SeedMovie(1, "Repeat");
        SeedSession(coupleId, movieId, userId, DateTime.UtcNow);
        SeedSession(coupleId, movieId, userId, DateTime.UtcNow.AddDays(-7));

        var result = await _sut.GetDashboardAsync(coupleId);

        result.RewatchCount.Should().Be(1);
    }

    [Fact]
    public async Task GetDashboardAsync_ComputesDisagreement()
    {
        var (coupleId, userId, partnerId) = SeedCoupleWithUsers();
        var movieId = SeedMovie(1, "Disagree");
        var sessionId = SeedSession(coupleId, movieId, userId, DateTime.UtcNow);
        SeedRating(sessionId, userId, 10);
        SeedRating(sessionId, partnerId, 1);

        var result = await _sut.GetDashboardAsync(coupleId);

        result.BiggestDisagreement.Should().NotBeNull();
        result.BiggestDisagreement!.Title.Should().Be("Disagree");
        result.AverageDisagreement.Should().Be(9.0);
    }

    [Fact]
    public async Task GetDashboardAsync_ComputesStreaks()
    {
        var (coupleId, userId, partnerId) = SeedCoupleWithUsers();
        var movieId = SeedMovie(1, "S1");
        var movie2Id = SeedMovie(2, "S2");
        var movie3Id = SeedMovie(3, "S3");

        var thisWeek = GetMondayOfWeek(DateTime.UtcNow);
        var lastWeek = thisWeek.AddDays(-7);
        var twoWeeksAgo = thisWeek.AddDays(-14);
        var longAgo = thisWeek.AddDays(-100);

        SeedSession(coupleId, movieId, userId, thisWeek);
        SeedSession(coupleId, movie2Id, userId, lastWeek);
        SeedSession(coupleId, movie3Id, userId, twoWeeksAgo);
        SeedSession(coupleId, movieId, userId, longAgo);

        var result = await _sut.GetDashboardAsync(coupleId);

        result.CurrentStreak.Should().Be(3);
        result.LongestStreak.Should().Be(3);
    }

    private static DateTime GetMondayOfWeek(DateTime date)
    {
        var diff = (7 + (date.DayOfWeek - DayOfWeek.Monday)) % 7;
        return date.AddDays(-diff).Date;
    }

    private (Guid coupleId, Guid userId, Guid partnerId) SeedCoupleWithUsers()
    {
        var userId = Guid.NewGuid();
        var partnerId = Guid.NewGuid();
        var coupleId = Guid.NewGuid();
        SeedUser(userId, "alice");
        SeedUser(partnerId, "bob");
        SeedCouple(coupleId, userId, partnerId);
        return (coupleId, userId, partnerId);
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

    private Guid SeedMovie(int tmdbId, string title)
    {
        var id = Guid.NewGuid();
        _db.Movies.Add(
            new Movie
            {
                Id = id,
                TmdbId = tmdbId,
                Title = title,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            }
        );
        _db.SaveChanges();
        return id;
    }

    private Guid SeedSession(Guid coupleId, Guid movieId, Guid createdByUserId, DateTime watchedAt)
    {
        var id = Guid.NewGuid();
        _db.WatchSessions.Add(
            new WatchSession
            {
                Id = id,
                CoupleId = coupleId,
                MovieId = movieId,
                WatchedAt = watchedAt,
                CreatedByUserId = createdByUserId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            }
        );
        _db.SaveChanges();
        return id;
    }

    private void SeedRating(Guid sessionId, Guid userId, int ratingValue)
    {
        _db.Ratings.Add(
            new Rating
            {
                Id = Guid.NewGuid(),
                WatchSessionId = sessionId,
                UserId = userId,
                RatingValue = ratingValue,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            }
        );
        _db.SaveChanges();
    }
}
