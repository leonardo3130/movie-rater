using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using MovieRaterApi.Data;
using MovieRaterApi.Data.Entities;
using MovieRaterApi.Features.Authentication.Infrastructure;
using MovieRaterApi.Features.WatchSessions.DTOs;
using MovieRaterApi.Features.WatchSessions.Services;
using MovieRaterApi.Infrastructure.Exceptions;

namespace MovieRaterApi.Tests.Unit.Services;

public class WatchSessionServiceTests
{
    private readonly ApplicationDbContext _db;
    private readonly Mock<ILogger<WatchSessionService>> _loggerMock;
    private readonly Mock<ICurrentUser> _currentUserMock;
    private readonly WatchSessionService _sut;

    public WatchSessionServiceTests()
    {
        _db = TestHelpers.CreateInMemoryDbContext();
        _loggerMock = new Mock<ILogger<WatchSessionService>>();
        _currentUserMock = new Mock<ICurrentUser>();
        _currentUserMock.Setup(u => u.IsAuthenticated).Returns(true);
        _currentUserMock.Setup(u => u.UserId).Returns(Guid.NewGuid());
        _sut = new WatchSessionService(_db, _currentUserMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task CreateAsync_ReturnsSession_WhenValid()
    {
        var movieId = Guid.NewGuid();
        var ids = SeedGroupWithUsers(3, "member");
        var groupId = ids[0];
        var userId = ids[1];
        SeedMovie(movieId, 1, "Inception");

        var request = new CreateWatchSessionRequestDto
        {
            MediaId = movieId,
            WatchedAt = new DateTime(2026, 1, 15, 20, 0, 0, DateTimeKind.Utc),
            Location = "Home",
            Notes = "Great movie!",
        };

        var result = await _sut.CreateAsync(request, userId, groupId);

        result.MediaId.Should().Be(movieId);
        result.Title.Should().Be("Inception");
        result.MediaType.Should().Be(MediaType.Movie);
        result.SeriesTitle.Should().BeNull();
        result.SeasonNumber.Should().BeNull();
        result.EpisodeNumber.Should().BeNull();
        result.Location.Should().Be("Home");
        result.Notes.Should().Be("Great movie!");
        result.CreatedByUserId.Should().Be(userId);
        _db.WatchSessions.Count().Should().Be(1);
    }

    [Fact]
    public async Task CreateAsync_Throws_WhenMediaNotFound()
    {
        var request = new CreateWatchSessionRequestDto
        {
            MediaId = Guid.NewGuid(),
            WatchedAt = DateTime.UtcNow,
        };

        await FluentActions
            .Awaiting(() => _sut.CreateAsync(request, Guid.NewGuid(), Guid.NewGuid()))
            .Should()
            .ThrowAsync<NotFoundException>()
            .WithMessage("Media not found.");
    }

    [Fact]
    public async Task CreateAsync_ReturnsEpisodeSession_WithTvInfo()
    {
        var ids = SeedGroupWithUsers(3, "member");
        var userId = ids[1];
        var groupId = ids[0];
        var seriesId = Guid.NewGuid();
        var seasonId = Guid.NewGuid();
        var episodeId = Guid.NewGuid();
        SeedSeries(seriesId, 100, "Breaking Bad");
        SeedSeason(seasonId, seriesId, 3578, 5);
        SeedEpisode(episodeId, seasonId, 62085, 5, 3, "Fly");

        var request = new CreateWatchSessionRequestDto
        {
            MediaId = episodeId,
            WatchedAt = DateTime.UtcNow,
        };

        var result = await _sut.CreateAsync(request, userId, groupId);

        result.MediaId.Should().Be(episodeId);
        result.Title.Should().Be("Fly");
        result.MediaType.Should().Be(MediaType.TvEpisode);
        result.SeriesTitle.Should().Be("Breaking Bad");
        result.SeasonNumber.Should().Be(5);
        result.EpisodeNumber.Should().Be(3);
    }

    [Fact]
    public async Task CreateAsync_Throws_WhenMediaIsSeries()
    {
        var seriesId = Guid.NewGuid();
        SeedSeries(seriesId, 100, "Breaking Bad");

        var request = new CreateWatchSessionRequestDto
        {
            MediaId = seriesId,
            WatchedAt = DateTime.UtcNow,
        };

        await FluentActions
            .Awaiting(() => _sut.CreateAsync(request, Guid.NewGuid(), Guid.NewGuid()))
            .Should()
            .ThrowAsync<BadRequestException>()
            .WithMessage("Watch sessions can only be created for movies or episodes.");
    }

    [Fact]
    public async Task CreateAsync_Throws_WhenMediaIsSeason()
    {
        var seriesId = Guid.NewGuid();
        var seasonId = Guid.NewGuid();
        SeedSeries(seriesId, 100, "Breaking Bad");
        SeedSeason(seasonId, seriesId, 3578, 1);

        var request = new CreateWatchSessionRequestDto
        {
            MediaId = seasonId,
            WatchedAt = DateTime.UtcNow,
        };

        await FluentActions
            .Awaiting(() => _sut.CreateAsync(request, Guid.NewGuid(), Guid.NewGuid()))
            .Should()
            .ThrowAsync<BadRequestException>()
            .WithMessage("Watch sessions can only be created for movies or episodes.");
    }

    [Fact]
    public async Task GetAllAsync_ReturnsTvInfo_ForEpisodeSessions()
    {
        var userId = _currentUserMock.Object.UserId;
        var groupId = Guid.NewGuid();
        SeedUser(userId, "Leo");
        SeedGroup(groupId);
        SeedUserGroup(groupId, userId);

        var seriesId = Guid.NewGuid();
        var seasonId = Guid.NewGuid();
        var episodeId = Guid.NewGuid();
        SeedSeries(seriesId, 100, "Breaking Bad");
        SeedSeason(seasonId, seriesId, 3578, 1);
        SeedEpisode(episodeId, seasonId, 62085, 1, 4, "Cancer Man");

        _db.WatchSessions.Add(
            new WatchSession
            {
                Id = Guid.NewGuid(),
                GroupId = groupId,
                MediaId = episodeId,
                WatchedAt = DateTime.UtcNow,
                CreatedByUserId = userId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            }
        );
        _db.SaveChanges();

        var query = new WatchSessionQueryDto { Page = 1, PageSize = 20, GroupId = groupId };

        var result = await _sut.GetAllAsync(query);

        result.TotalCount.Should().Be(1);
        var item = result.Items.Single();
        item.MediaId.Should().Be(episodeId);
        item.MediaType.Should().Be(MediaType.TvEpisode);
        item.Title.Should().Be("Cancer Man");
        item.SeriesTitle.Should().Be("Breaking Bad");
        item.SeasonNumber.Should().Be(1);
        item.EpisodeNumber.Should().Be(4);
    }

    [Fact]
    public async Task GetAllAsync_ReturnWatchSessions()
    {
        var movieId = Guid.NewGuid();
        var groupId = Guid.NewGuid();
        var groupId2 = Guid.NewGuid();
        var wsId1 = Guid.NewGuid();
        var wsId2 = Guid.NewGuid();
        var wsId3 = Guid.NewGuid();
        var userId = _currentUserMock.Object.UserId;
        SeedUser(userId, "Leo");
        SeedGroup(groupId);
        SeedGroup(groupId2);
        SeedUserGroup(groupId, userId);
        SeedUserGroup(groupId2, userId);
        SeedMovie(movieId, 1, "Inception");
        SeedWatchSession(
            wsId1,
            groupId,
            movieId,
            userId,
            new DateTime(2026, 2, 1, 20, 0, 0, DateTimeKind.Utc)
        );
        SeedWatchSession(
            wsId2,
            groupId,
            movieId,
            userId,
            new DateTime(2026, 1, 15, 20, 0, 0, DateTimeKind.Utc)
        );
        SeedWatchSession(
            wsId3,
            groupId2,
            movieId,
            userId,
            new DateTime(2026, 1, 15, 21, 0, 0, DateTimeKind.Utc)
        );

        await _db.SaveChangesAsync();

        var query = new WatchSessionQueryDto
        {
            Page = 1,
            PageSize = 20,
            GroupId = groupId,
        };

        var result = await _sut.GetAllAsync(query);

        var logger = _loggerMock.Object;

        result.TotalCount.Should().Be(2);
        result.Items.Count.Should().Be(2);
        result.Items.Should().NotContain(ws => ws.GroupId == groupId2);
        result
            .Items.Should()
            .AllSatisfy(ws =>
            {
                ws.GroupId.Should().Be(groupId);
            });
    }

    [Fact]
    public async Task GetAllAsync_Throws_WhenNotInGroup()
    {
        var movieId = Guid.NewGuid();
        var ids = SeedGroupWithUsers(3, "member");
        var groupId = ids[0];
        var userId = ids[1];
        SeedMovie(movieId, 1, "Inception");
        SeedWatchSession(
            Guid.NewGuid(),
            groupId,
            movieId,
            userId,
            new DateTime(2026, 2, 1, 20, 0, 0, DateTimeKind.Utc)
        );
        SeedWatchSession(
            Guid.NewGuid(),
            groupId,
            movieId,
            userId,
            new DateTime(2026, 1, 15, 20, 0, 0, DateTimeKind.Utc)
        );

        var query = new WatchSessionQueryDto
        {
            Page = 1,
            PageSize = 20,
            GroupId = groupId,
        };

        await FluentActions
            .Awaiting(() => _sut.GetAllAsync(query))
            .Should()
            .ThrowAsync<ForbiddenException>()
            .WithMessage("You are not part of the group");
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsSessionWithMovieAndRatings()
    {
        var partnerId = Guid.NewGuid();
        var movieId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var ids = SeedGroupWithUsers(3, "member");
        var groupId = ids[0];
        var userId = ids[1];
        SeedMovie(movieId, 1, "Inception");
        SeedWatchSession(sessionId, groupId, movieId, userId, DateTime.UtcNow);
        SeedRating(sessionId, userId, 8, "Great");
        SeedRating(sessionId, partnerId, 7, "Good");

        var result = await _sut.GetByIdAsync(sessionId);

        result.Title.Should().Be("Inception");
        result.Ratings.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetByIdAsync_Throws_WhenNotInCouple()
    {
        var sessionId = Guid.NewGuid();
        var otherCoupleId = Guid.NewGuid();
        SeedWatchSession(sessionId, otherCoupleId, Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow);

        await FluentActions
            .Awaiting(() => _sut.GetByIdAsync(sessionId))
            .Should()
            .ThrowAsync<NotFoundException>()
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
            .ThrowAsync<ForbiddenException>()
            .WithMessage("You can only delete your own watch sessions.");
    }

    [Fact]
    public async Task UpdateAsync_UpdatesDateAndDetails_WhenUserIsCreator()
    {
        var userId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var groupId = Guid.NewGuid();
        var movieId = Guid.NewGuid();
        SeedUser(userId, "Leo");
        SeedGroup(groupId);
        SeedMovie(movieId, 1, "Inception");
        SeedWatchSession(
            sessionId,
            groupId,
            movieId,
            userId,
            new DateTime(2026, 1, 15, 20, 0, 0, DateTimeKind.Utc)
        );

        var request = new UpdateWatchSessionRequestDto
        {
            WatchedAt = new DateTime(2026, 3, 1, 21, 0, 0, DateTimeKind.Utc),
            Location = "Cinema",
            Notes = "Rewatch",
        };

        var result = await _sut.UpdateAsync(sessionId, request, userId);

        result.WatchedAt.Should().Be(request.WatchedAt);
        result.Location.Should().Be("Cinema");
        result.Notes.Should().Be("Rewatch");

        var updated = _db.WatchSessions.First(ws => ws.Id == sessionId);
        updated.WatchedAt.Should().Be(request.WatchedAt);
        updated.Location.Should().Be("Cinema");
        updated.Notes.Should().Be("Rewatch");
        updated.UpdatedAt.Should().BeAfter(DateTime.UtcNow.AddMinutes(-1));
    }

    [Fact]
    public async Task UpdateAsync_Throws_WhenNotFound()
    {
        var request = new UpdateWatchSessionRequestDto { WatchedAt = DateTime.UtcNow };

        await FluentActions
            .Awaiting(() => _sut.UpdateAsync(Guid.NewGuid(), request, Guid.NewGuid()))
            .Should()
            .ThrowAsync<NotFoundException>()
            .WithMessage("Watch session not found.");
    }

    [Fact]
    public async Task UpdateAsync_Throws_WhenUserIsNotCreator()
    {
        var creatorId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var groupId = Guid.NewGuid();
        var movieId = Guid.NewGuid();
        SeedUser(creatorId, "Creator");
        SeedGroup(groupId);
        SeedMovie(movieId, 1, "Inception");
        SeedWatchSession(
            sessionId,
            groupId,
            movieId,
            creatorId,
            new DateTime(2026, 1, 15, 20, 0, 0, DateTimeKind.Utc)
        );

        var request = new UpdateWatchSessionRequestDto { WatchedAt = DateTime.UtcNow };

        await FluentActions
            .Awaiting(() => _sut.UpdateAsync(sessionId, request, Guid.NewGuid()))
            .Should()
            .ThrowAsync<ForbiddenException>()
            .WithMessage("You can only edit your own watch sessions.");
    }

    [Fact]
    public async Task GetHeatmapAsync_ReturnsDailyCounts()
    {
        var movieId = Guid.NewGuid();
        var firstDate = new DateTime(2026, 7, 1, 20, 0, 0, DateTimeKind.Utc);
        var ids = SeedGroupWithUsers(3, "member");
        var groupId = ids[0];
        var userId = ids[1];
        SeedMovie(movieId, 1, "Inception");
        SeedWatchSession(Guid.NewGuid(), groupId, movieId, userId, firstDate);
        SeedWatchSession(
            Guid.NewGuid(),
            groupId,
            movieId,
            userId,
            new DateTime(2026, 7, 1, 22, 0, 0, DateTimeKind.Utc)
        );
        SeedWatchSession(
            Guid.NewGuid(),
            groupId,
            movieId,
            userId,
            new DateTime(2026, 7, 2, 20, 0, 0, DateTimeKind.Utc)
        );

        var result = await _sut.GetHeatmapAsync(
            (DateTime.UtcNow - firstDate).Days + 1,
            userId,
            groupId
        );

        result.DailyCounts.Count.Should().Be(2);
        result.DailyCounts["2026-07-01"].Should().Be(2);
        result.DailyCounts["2026-07-02"].Should().Be(1);
    }

    private void SeedGroup(Guid groupId)
    {
        _db.Groups.Add(new Group { Id = groupId, CreatedAt = DateTime.UtcNow });
        _db.SaveChanges();
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

    private void SeedSeries(Guid seriesId, int tmdbId, string title)
    {
        _db.TvSeries.Add(
            new TvSeries
            {
                Id = seriesId,
                TmdbId = tmdbId,
                Title = title,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            }
        );
        _db.SaveChanges();
    }

    private void SeedSeason(Guid seasonId, Guid seriesId, int tmdbId, int seasonNumber)
    {
        _db.TvSeasons.Add(
            new TvSeason
            {
                Id = seasonId,
                SeriesId = seriesId,
                TmdbId = tmdbId,
                SeasonNumber = seasonNumber,
                Title = $"Season {seasonNumber}",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            }
        );
        _db.SaveChanges();
    }

    private void SeedEpisode(
        Guid episodeId,
        Guid seasonId,
        int tmdbId,
        int seasonNumber,
        int episodeNumber,
        string title
    )
    {
        _db.TvEpisodes.Add(
            new TvEpisode
            {
                Id = episodeId,
                SeasonId = seasonId,
                TmdbId = tmdbId,
                SeasonNumber = seasonNumber,
                EpisodeNumber = episodeNumber,
                Title = title,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            }
        );
        _db.SaveChanges();
    }

    private void SeedWatchSession(
        Guid sessionId,
        Guid groupId,
        Guid movieId,
        Guid createdByUserId,
        DateTime watchedAt
    )
    {
        _db.WatchSessions.Add(
            new WatchSession
            {
                Id = sessionId,
                GroupId = groupId,
                MediaId = movieId,
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
