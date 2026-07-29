using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using MovieRaterApi.Data;
using MovieRaterApi.Data.Entities;
using MovieRaterApi.Features.UserMovie.Services;
using MovieRaterApi.Infrastructure.Exceptions;
using MovieRaterApi.Infrastructure.Tmdb;
using MovieRaterApi.Infrastructure.Tmdb.Dtos.Responses;

namespace MovieRaterApi.Tests.Unit.Services;

public class UserMovieServiceTests
{
    private readonly ApplicationDbContext _db;
    private readonly Mock<ILogger<UserMovieService>> _loggerMock;
    private readonly Mock<ITmdbClient> _tmdbMock;
    private readonly IMemoryCache _cache;
    private readonly UserMovieService _sut;

    public UserMovieServiceTests()
    {
        _db = TestHelpers.CreateInMemoryDbContext();
        _loggerMock = new Mock<ILogger<UserMovieService>>();
        _tmdbMock = new Mock<ITmdbClient>();
        _cache = new MemoryCache(new MemoryCacheOptions());

        _tmdbMock
            .Setup(x => x.GetConfigurationAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TmdbConfiguration
            {
                Images = new TmdbImageConfig { SecureBaseUrl = "https://image.tmdb.org/t/p/" },
            });

        _sut = new UserMovieService(_db, _tmdbMock.Object, _cache, _loggerMock.Object);
    }

    [Fact]
    public async Task SetFavoriteAsync_SetsFavoriteTrue_WhenMovieExists()
    {
        var movieId = SeedMovie();
        var userId = Guid.NewGuid();

        var result = await _sut.SetFavoriteAsync(movieId, userId, true);

        result.IsFavorite.Should().BeTrue();
        result.IsInWatchlist.Should().BeFalse();
        result.MovieId.Should().Be(movieId);
        result.UserId.Should().Be(userId);

        var row = _db.UserMovies.Single();
        row.IsFavorite.Should().BeTrue();
        row.IsInWatchlist.Should().BeFalse();
    }

    [Fact]
    public async Task SetFavoriteAsync_Throws_WhenMovieNotFound()
    {
        await FluentActions
            .Awaiting(() => _sut.SetFavoriteAsync(Guid.NewGuid(), Guid.NewGuid(), true))
            .Should()
            .ThrowAsync<NotFoundException>()
            .WithMessage("Movie not found.");
    }

    [Fact]
    public async Task SetWatchlistAsync_SetsWatchlistTrue_WhenMovieExists()
    {
        var movieId = SeedMovie();
        var userId = Guid.NewGuid();

        var result = await _sut.SetWatchlistAsync(movieId, userId, true);

        result.IsInWatchlist.Should().BeTrue();
        result.IsFavorite.Should().BeFalse();
        result.MovieId.Should().Be(movieId);
        result.UserId.Should().Be(userId);

        var row = _db.UserMovies.Single();
        row.IsInWatchlist.Should().BeTrue();
        row.IsFavorite.Should().BeFalse();
    }

    [Fact]
    public async Task SetWatchlistAsync_Throws_WhenMovieNotFound()
    {
        await FluentActions
            .Awaiting(() => _sut.SetWatchlistAsync(Guid.NewGuid(), Guid.NewGuid(), true))
            .Should()
            .ThrowAsync<NotFoundException>()
            .WithMessage("Movie not found.");
    }

    [Fact]
    public async Task SetFavoriteAsync_ToggleOff_RemovesRow_WhenNoOtherFlag()
    {
        var movieId = SeedMovie();
        var userId = Guid.NewGuid();

        await _sut.SetFavoriteAsync(movieId, userId, true);
        _db.UserMovies.Count().Should().Be(1);

        var result = await _sut.SetFavoriteAsync(movieId, userId, false);

        result.IsFavorite.Should().BeFalse();
        result.IsInWatchlist.Should().BeFalse();
        _db.UserMovies.Count().Should().Be(0);
    }

    [Fact]
    public async Task SetWatchlistAsync_ToggleOff_RemovesRow_WhenNoOtherFlag()
    {
        var movieId = SeedMovie();
        var userId = Guid.NewGuid();

        await _sut.SetWatchlistAsync(movieId, userId, true);
        _db.UserMovies.Count().Should().Be(1);

        var result = await _sut.SetWatchlistAsync(movieId, userId, false);

        result.IsInWatchlist.Should().BeFalse();
        result.IsFavorite.Should().BeFalse();
        _db.UserMovies.Count().Should().Be(0);
    }

    [Fact]
    public async Task SetFavoriteAsync_ToggleOff_PreservesRow_WhenWatchlistIsTrue()
    {
        var movieId = SeedMovie();
        var userId = Guid.NewGuid();

        await _sut.SetWatchlistAsync(movieId, userId, true);
        await _sut.SetFavoriteAsync(movieId, userId, true);
        _db.UserMovies.Count().Should().Be(1);

        var result = await _sut.SetFavoriteAsync(movieId, userId, false);

        result.IsFavorite.Should().BeFalse();
        result.IsInWatchlist.Should().BeTrue();
        _db.UserMovies.Count().Should().Be(1);

        var row = _db.UserMovies.Single();
        row.IsFavorite.Should().BeFalse();
        row.IsInWatchlist.Should().BeTrue();
    }

    [Fact]
    public async Task SetWatchlistAsync_ToggleOff_PreservesRow_WhenFavoriteIsTrue()
    {
        var movieId = SeedMovie();
        var userId = Guid.NewGuid();

        await _sut.SetFavoriteAsync(movieId, userId, true);
        await _sut.SetWatchlistAsync(movieId, userId, true);
        _db.UserMovies.Count().Should().Be(1);

        var result = await _sut.SetWatchlistAsync(movieId, userId, false);

        result.IsInWatchlist.Should().BeFalse();
        result.IsFavorite.Should().BeTrue();
        _db.UserMovies.Count().Should().Be(1);

        var row = _db.UserMovies.Single();
        row.IsInWatchlist.Should().BeFalse();
        row.IsFavorite.Should().BeTrue();
    }

    [Fact]
    public async Task SetFavoriteAsync_ToggleOff_NoRow_ReturnsDefault()
    {
        var movieId = SeedMovie();
        var userId = Guid.NewGuid();

        var result = await _sut.SetFavoriteAsync(movieId, userId, false);

        result.IsFavorite.Should().BeFalse();
        result.IsInWatchlist.Should().BeFalse();
        _db.UserMovies.Count().Should().Be(0);
    }

    [Fact]
    public async Task SetWatchlistAsync_ToggleOff_NoRow_ReturnsDefault()
    {
        var movieId = SeedMovie();
        var userId = Guid.NewGuid();

        var result = await _sut.SetWatchlistAsync(movieId, userId, false);

        result.IsInWatchlist.Should().BeFalse();
        result.IsFavorite.Should().BeFalse();
        _db.UserMovies.Count().Should().Be(0);
    }

    [Fact]
    public async Task GetAsync_ReturnsCurrentState()
    {
        var movieId = SeedMovie();
        var userId = Guid.NewGuid();

        await _sut.SetFavoriteAsync(movieId, userId, true);

        var result = await _sut.GetAsync(movieId, userId);

        result.IsFavorite.Should().BeTrue();
        result.IsInWatchlist.Should().BeFalse();
        result.MovieId.Should().Be(movieId);
        result.UserId.Should().Be(userId);
    }

    [Fact]
    public async Task GetAsync_ReturnsDefaults_WhenNoRelationship()
    {
        var movieId = SeedMovie();
        var userId = Guid.NewGuid();

        var result = await _sut.GetAsync(movieId, userId);

        result.IsFavorite.Should().BeFalse();
        result.IsInWatchlist.Should().BeFalse();
    }

    private Guid SeedMovie()
    {
        var movieId = Guid.NewGuid();
        _db.Movies.Add(
            new Movie
            {
                Id = movieId,
                TmdbId = new Random().Next(10000, 99999),
                Title = "Test Movie",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            }
        );
        _db.SaveChanges();
        return movieId;
    }
}
