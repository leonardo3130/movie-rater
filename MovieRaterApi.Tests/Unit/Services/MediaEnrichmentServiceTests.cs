using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using MovieRaterApi.Data;
using MovieRaterApi.Data.Entities;
using MovieRaterApi.Features.Authentication.Infrastructure;
using MovieRaterApi.Features.Movies.DTOs;
using MovieRaterApi.Features.Shared.Interfaces;
using MovieRaterApi.Features.Shared.Services;
using MovieRaterApi.Features.TvShows.DTOs;

namespace MovieRaterApi.Tests.Unit.Services;

public class MediaEnrichmentServiceTests
{
    private readonly Mock<ICurrentUser> _currentUserMock;

    public MediaEnrichmentServiceTests()
    {
        _currentUserMock = new Mock<ICurrentUser>();
        _currentUserMock.Setup(u => u.IsAuthenticated).Returns(true);
        _currentUserMock.Setup(u => u.UserId).Returns(Guid.NewGuid());
    }

    private MediaEnrichmentService CreateSut(ApplicationDbContext db)
    {
        return new MediaEnrichmentService(
            db,
            _currentUserMock.Object,
            new Mock<ILogger<MediaEnrichmentService>>().Object
        );
    }

    [Fact]
    public async Task EnrichAsync_List_ShouldSetFavoriteWatchlistAndWatchedCount()
    {
        var userId = _currentUserMock.Object.UserId;
        var groupId = Guid.NewGuid();
        var db = TestHelpers.CreateInMemoryDbContext();
        SeedUser(db, userId);

        var movieId = Guid.NewGuid();
        db.Movies.Add(new Movie { Id = movieId, TmdbId = 550, Title = "Fight Club" });
        db.UserMedias.Add(
            new UserMedia
            {
                UserId = userId,
                MediaId = movieId,
                IsFavorite = true,
                IsInWatchlist = false,
            }
        );

        db.Groups.Add(new Group { Id = groupId, Name = "G" });
        db.UserGroups.Add(
            new UserGroup { Id = Guid.NewGuid(), GroupId = groupId, UserId = userId }
        );
        db.WatchSessions.AddRange(
            new WatchSession
            {
                Id = Guid.NewGuid(),
                MediaId = movieId,
                CreatedByUserId = userId,
                WatchedAt = DateTime.UtcNow,
            },
            new WatchSession
            {
                Id = Guid.NewGuid(),
                GroupId = groupId,
                MediaId = movieId,
                CreatedByUserId = userId,
                WatchedAt = DateTime.UtcNow,
            }
        );
        db.SaveChanges();

        var sut = CreateSut(db);
        var items = new List<MovieSummaryDto>
        {
            new() { TmdbId = 550 },
        };

        await sut.EnrichAsync(items);

        items[0].Id.Should().Be(movieId);
        items[0].IsFavorite.Should().BeTrue();
        items[0].IsInWatchlist.Should().BeFalse();
        items[0].WatchedCount.Should().Be(2);
    }

    [Fact]
    public async Task EnrichAsync_List_ShouldRespectMediaType()
    {
        var userId = _currentUserMock.Object.UserId;
        var db = TestHelpers.CreateInMemoryDbContext();
        SeedUser(db, userId);

        var movieId = Guid.NewGuid();
        var seriesId = Guid.NewGuid();
        db.Movies.Add(new Movie { Id = movieId, TmdbId = 550, Title = "Movie 550" });
        db.TvSeries.Add(new TvSeries { Id = seriesId, TmdbId = 550, Title = "Series 550" });
        db.SaveChanges();

        var sut = CreateSut(db);
        var items = new List<IEnrichableMediaDto>
        {
            new MovieSummaryDto { TmdbId = 550 },
            new TvShowSummaryDto { TmdbId = 550 },
        };

        await sut.EnrichAsync(items);

        items[0].Id.Should().Be(movieId);
        items[1].Id.Should().Be(seriesId);
    }

    [Fact]
    public async Task EnrichAsync_List_ShouldLeaveUncachedItemsUntouched()
    {
        var userId = _currentUserMock.Object.UserId;
        var db = TestHelpers.CreateInMemoryDbContext();
        SeedUser(db, userId);

        var sut = CreateSut(db);
        var items = new List<MovieSummaryDto> { new() { TmdbId = 999 } };

        await sut.EnrichAsync(items);

        items[0].Id.Should().Be(Guid.Empty);
        items[0].IsFavorite.Should().BeFalse();
        items[0].WatchedCount.Should().Be(0);
    }

    [Fact]
    public async Task EnrichAsync_Single_ShouldSetFields()
    {
        var userId = _currentUserMock.Object.UserId;
        var db = TestHelpers.CreateInMemoryDbContext();
        SeedUser(db, userId);

        var movieId = Guid.NewGuid();
        db.Movies.Add(new Movie { Id = movieId, TmdbId = 550, Title = "Fight Club" });
        db.UserMedias.Add(
            new UserMedia
            {
                UserId = userId,
                MediaId = movieId,
                IsFavorite = true,
                IsInWatchlist = true,
            }
        );
        db.WatchSessions.Add(
            new WatchSession
            {
                Id = Guid.NewGuid(),
                MediaId = movieId,
                CreatedByUserId = userId,
                WatchedAt = DateTime.UtcNow,
            }
        );
        db.SaveChanges();

        var sut = CreateSut(db);
        var item = new MovieDetailsResponseDto { TmdbId = 550 };

        await sut.EnrichAsync(item);

        item.Id.Should().Be(movieId);
        item.IsFavorite.Should().BeTrue();
        item.IsInWatchlist.Should().BeTrue();
        item.WatchedCount.Should().Be(1);
    }

    [Fact]
    public async Task EnrichAsync_Single_ShouldCountOnlyMatchingMedia()
    {
        var userId = _currentUserMock.Object.UserId;
        var db = TestHelpers.CreateInMemoryDbContext();
        SeedUser(db, userId);

        var movieId = Guid.NewGuid();
        db.Movies.Add(new Movie { Id = movieId, TmdbId = 550, Title = "Fight Club" });
        db.Movies.Add(new Movie { Id = Guid.NewGuid(), TmdbId = 680, Title = "Pulp Fiction" });
        db.WatchSessions.Add(
            new WatchSession
            {
                Id = Guid.NewGuid(),
                MediaId = movieId,
                CreatedByUserId = userId,
                WatchedAt = DateTime.UtcNow,
            }
        );
        db.SaveChanges();

        var sut = CreateSut(db);
        var item = new MovieDetailsResponseDto { TmdbId = 550 };

        await sut.EnrichAsync(item);

        item.WatchedCount.Should().Be(1);
    }

    [Fact]
    public async Task EnrichAsync_Unauthenticated_ShouldNotEnrich()
    {
        var userId = Guid.NewGuid();
        var db = TestHelpers.CreateInMemoryDbContext();
        SeedUser(db, userId);
        db.Movies.Add(new Movie { Id = Guid.NewGuid(), TmdbId = 550, Title = "Fight Club" });
        db.SaveChanges();

        _currentUserMock.Setup(u => u.IsAuthenticated).Returns(false);

        var sut = CreateSut(db);
        var item = new MovieDetailsResponseDto { TmdbId = 550 };

        await sut.EnrichAsync(item);

        item.Id.Should().Be(Guid.Empty);
        item.IsFavorite.Should().BeFalse();
    }

    private static void SeedUser(ApplicationDbContext db, Guid userId)
    {
        db.Users.Add(
            new User { Id = userId, Username = "user", Email = "user@test.com" }
        );
        db.SaveChanges();
    }
}