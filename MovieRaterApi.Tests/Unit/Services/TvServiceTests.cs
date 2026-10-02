using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using MovieRaterApi.Data;
using MovieRaterApi.Data.Entities;
using MovieRaterApi.Features.Authentication.Infrastructure;
using MovieRaterApi.Features.Movies.DTOs;
using MovieRaterApi.Features.Shared.Services;
using MovieRaterApi.Features.TvShows.DTOs;
using MovieRaterApi.Features.TvShows.Services;
using MovieRaterApi.Infrastructure.Exceptions;
using MovieRaterApi.Infrastructure.Tmdb;
using MovieRaterApi.Infrastructure.Tmdb.Dtos.Requests;
using MovieRaterApi.Infrastructure.Tmdb.Dtos.Responses;

namespace MovieRaterApi.Tests.Unit.Services;

public class TvServiceTests
{
    private readonly Mock<ITmdbClient> _tmdbMock;
    private readonly Mock<ICurrentUser> _currentUserMock;
    private readonly Mock<ILogger<TvService>> _loggerMock;
    private readonly IMemoryCache _cache;
    private readonly TmdbImageConfig _imageConfig;

    public TvServiceTests()
    {
        _tmdbMock = new Mock<ITmdbClient>();
        _currentUserMock = new Mock<ICurrentUser>();
        _loggerMock = new Mock<ILogger<TvService>>();
        _cache = new MemoryCache(new MemoryCacheOptions());

        _imageConfig = new TmdbImageConfig
        {
            SecureBaseUrl = "https://image.tmdb.org/t/p/",
            PosterSizes = ["w92", "w154", "w342"],
            BackdropSizes = ["w300", "w780"],
        };

        _tmdbMock
            .Setup(t => t.GetConfigurationAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TmdbConfiguration { Images = _imageConfig });

        _currentUserMock.Setup(u => u.IsAuthenticated).Returns(true);
        _currentUserMock.Setup(u => u.UserId).Returns(Guid.NewGuid());
    }

    private TvService CreateSut(ApplicationDbContext db)
    {
        var enrichmentLogger = new Mock<ILogger<MediaEnrichmentService>>();
        var enrichment = new MediaEnrichmentService(
            db,
            _currentUserMock.Object,
            enrichmentLogger.Object
        );

        return new TvService(_tmdbMock.Object, db, _cache, _loggerMock.Object, enrichment);
    }

    [Fact]
    public async Task SearchTvShowsAsync_ShouldCallTmdbAndMapResults()
    {
        var db = TestHelpers.CreateInMemoryDbContext();
        var sut = CreateSut(db);

        var tmdbResponse = new TmdbPagedResponse<TmdbSearchTvItem>
        {
            Page = 1,
            TotalPages = 1,
            TotalResults = 1,
            Results =
            [
                new TmdbSearchTvItem
                {
                    Id = 1396,
                    Name = "Breaking Bad",
                    Overview = "A chemistry teacher turns to crime.",
                    FirstAirDate = "2008-01-20",
                    PosterPath = "/poster.jpg",
                    BackdropPath = "/backdrop.jpg",
                    VoteAverage = 8.9,
                    VoteCount = 10000,
                    GenreIds = [18, 80],
                },
            ],
        };

        _tmdbMock
            .Setup(t =>
                t.SearchTvShowsAsync(
                    It.Is<TmdbSearchTvQuery>(q => q.Query == "breaking bad"),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(tmdbResponse);

        var request = new SearchTvShowsRequestDto { Query = "breaking bad", Page = 1 };

        var result = await sut.SearchTvShowsAsync(request);

        result.Page.Should().Be(1);
        result.Results.Should().HaveCount(1);
        var show = result.Results[0];
        show.TmdbId.Should().Be(1396);
        show.Title.Should().Be("Breaking Bad");
        show.PosterUrl.Should().Be("https://image.tmdb.org/t/p/w342/poster.jpg");
        show.BackdropUrl.Should().Be("https://image.tmdb.org/t/p/w780/backdrop.jpg");
        show.FirstAirDate.Should().Be("2008-01-20");
        show.VoteAverage.Should().Be(8.9);
        show.GenreIds.Should().BeEquivalentTo([18, 80]);
    }

    [Fact]
    public async Task SearchTvShowsAsync_ShouldPassFilterParamsToTmdb()
    {
        var db = TestHelpers.CreateInMemoryDbContext();
        var sut = CreateSut(db);

        _tmdbMock
            .Setup(t =>
                t.SearchTvShowsAsync(It.IsAny<TmdbSearchTvQuery>(), It.IsAny<CancellationToken>())
            )
            .ReturnsAsync(
                new TmdbPagedResponse<TmdbSearchTvItem>
                {
                    Page = 1,
                    TotalPages = 0,
                    TotalResults = 0,
                    Results = [],
                }
            );

        var request = new SearchTvShowsRequestDto
        {
            Query = "friends",
            FirstAirDateYear = "1994",
            Year = "2008",
        };

        await sut.SearchTvShowsAsync(request);

        _tmdbMock.Verify(
            t =>
                t.SearchTvShowsAsync(
                    It.Is<TmdbSearchTvQuery>(q =>
                        q.Query == "friends"
                        && q.FirstAirDateYear == "1994"
                        && q.Year == "2008"
                    ),
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );
    }

    [Fact]
    public async Task SearchTvShowsAsync_ShouldEnrichWithUserData()
    {
        var userId = Guid.NewGuid();
        var db = TestHelpers.CreateInMemoryDbContext();
        db.Users.Add(
            new User { Id = userId, Username = "user", Email = "user@test.com" }
        );

        var seriesId = Guid.NewGuid();
        db.TvSeries.Add(
            new TvSeries { Id = seriesId, TmdbId = 1396, Title = "Breaking Bad" }
        );
        db.UserMedias.Add(
            new UserMedia
            {
                UserId = userId,
                MediaId = seriesId,
                IsFavorite = true,
                IsInWatchlist = false,
            }
        );
        db.WatchSessions.Add(
            new WatchSession
            {
                Id = Guid.NewGuid(),
                MediaId = seriesId,
                WatchedAt = DateTime.UtcNow,
                CreatedByUserId = userId,
            }
        );
        db.SaveChanges();

        _currentUserMock.Setup(u => u.UserId).Returns(userId);

        var sut = CreateSut(db);

        _tmdbMock
            .Setup(t =>
                t.SearchTvShowsAsync(It.IsAny<TmdbSearchTvQuery>(), It.IsAny<CancellationToken>())
            )
            .ReturnsAsync(
                new TmdbPagedResponse<TmdbSearchTvItem>
                {
                    Page = 1,
                    TotalPages = 1,
                    TotalResults = 1,
                    Results = [new TmdbSearchTvItem { Id = 1396, Name = "Breaking Bad" }],
                }
            );

        var result = await sut.SearchTvShowsAsync(
            new SearchTvShowsRequestDto { Query = "breaking bad" }
        );

        result.Results[0].Id.Should().Be(seriesId);
        result.Results[0].IsFavorite.Should().BeTrue();
        result.Results[0].IsInWatchlist.Should().BeFalse();
        result.Results[0].WatchedCount.Should().Be(1);
    }

    [Fact]
    public async Task GetTvShowDetailsAsync_ShouldReturnFullDetails()
    {
        var db = TestHelpers.CreateInMemoryDbContext();
        var sut = CreateSut(db);

        var details = new TmdbTvShowDetails
        {
            Id = 1396,
            Name = "Breaking Bad",
            Overview = "A chemistry teacher turns to crime.",
            FirstAirDate = "2008-01-20",
            LastAirDate = "2013-09-29",
            NumberOfSeasons = 5,
            NumberOfEpisodes = 62,
            Status = "Ended",
            Type = "Scripted",
            VoteAverage = 8.9,
            VoteCount = 10000,
            PosterPath = "/poster.jpg",
            BackdropPath = "/backdrop.jpg",
            Genres =
            [
                new TmdbGenre { Id = 18, Name = "Drama" },
                new TmdbGenre { Id = 80, Name = "Crime" },
            ],
            Seasons =
            [
                new TmdbTvSeasonSummary
                {
                    Id = 3578,
                    SeasonNumber = 1,
                    EpisodeCount = 7,
                    AirDate = "2008-01-20",
                    PosterPath = "/s1.jpg",
                },
            ],
        };

        _tmdbMock
            .Setup(t =>
                t.GetTvShowDetailsAsync(
                    It.Is<TmdbTvDetailsQuery>(q => q.SeriesId == 1396),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(details);

        var result = await sut.GetTvShowDetailsAsync(1396, null);

        result.TmdbId.Should().Be(1396);
        result.Name.Should().Be("Breaking Bad");
        result.NumberOfSeasons.Should().Be(5);
        result.Status.Should().Be("Ended");
        result.PosterUrl.Should().Be("https://image.tmdb.org/t/p/w342/poster.jpg");
        result.Genres.Should().HaveCount(2);
        result.Seasons.Should().HaveCount(1);
        result.Seasons[0].EpisodeCount.Should().Be(7);
        result.Seasons[0].PosterUrl.Should().Be("https://image.tmdb.org/t/p/w342/s1.jpg");
    }

    [Fact]
    public async Task GetTvShowDetailsAsync_ShouldInsertSeriesIntoDatabase()
    {
        var db = TestHelpers.CreateInMemoryDbContext();
        var sut = CreateSut(db);

        var details = new TmdbTvShowDetails
        {
            Id = 1396,
            Name = "Breaking Bad",
            FirstAirDate = "2008-01-20",
            LastAirDate = "2013-09-29",
            NumberOfSeasons = 5,
            NumberOfEpisodes = 62,
            Status = "Ended",
            Type = "Scripted",
            VoteAverage = 8.9,
            PosterPath = "/poster.jpg",
            Genres = [new TmdbGenre { Id = 18, Name = "Drama" }],
            Seasons = [],
        };

        _tmdbMock
            .Setup(t =>
                t.GetTvShowDetailsAsync(It.IsAny<TmdbTvDetailsQuery>(), It.IsAny<CancellationToken>())
            )
            .ReturnsAsync(details);

        await sut.GetTvShowDetailsAsync(1396, null);

        var series = db.TvSeries.Single(s => s.TmdbId == 1396);
        series.Title.Should().Be("Breaking Bad");
        series.ReleaseDate.Should().Be(new DateOnly(2008, 1, 20));
        series.LastAirDate.Should().Be(new DateOnly(2013, 9, 29));
        series.NumberOfSeasons.Should().Be(5);
        series.Runtime.Should().BeNull();
        db.Genres.Should().Contain(g => g.TmdbId == 18 && g.Name == "Drama");
        db.MediaGenres.Should().Contain(mg => mg.Media.TmdbId == 1396 && mg.Genre.TmdbId == 18);
    }

    [Fact]
    public async Task GetTvShowDetailsAsync_ShouldUpdateExistingSeries()
    {
        var db = TestHelpers.CreateInMemoryDbContext();
        db.TvSeries.Add(
            new TvSeries
            {
                Id = Guid.NewGuid(),
                TmdbId = 1396,
                Title = "Old Title",
                AverageTmdbRating = 5.0,
            }
        );
        db.SaveChanges();

        var sut = CreateSut(db);

        var details = new TmdbTvShowDetails
        {
            Id = 1396,
            Name = "Breaking Bad",
            NumberOfSeasons = 5,
            NumberOfEpisodes = 62,
            VoteAverage = 8.9,
            Genres = [],
            Seasons = [],
        };

        _tmdbMock
            .Setup(t =>
                t.GetTvShowDetailsAsync(It.IsAny<TmdbTvDetailsQuery>(), It.IsAny<CancellationToken>())
            )
            .ReturnsAsync(details);

        await sut.GetTvShowDetailsAsync(1396, null);

        var updated = db.TvSeries.Single(s => s.TmdbId == 1396);
        updated.Title.Should().Be("Breaking Bad");
        updated.AverageTmdbRating.Should().Be(8.9);
        updated.NumberOfSeasons.Should().Be(5);
    }

    [Fact]
    public async Task GetTvShowDetailsAsync_ShouldNotDuplicateGenreLinks()
    {
        var db = TestHelpers.CreateInMemoryDbContext();
        var sut = CreateSut(db);

        var details = new TmdbTvShowDetails
        {
            Id = 1396,
            Name = "Breaking Bad",
            VoteAverage = 8.9,
            Genres = [new TmdbGenre { Id = 18, Name = "Drama" }],
            Seasons = [],
        };

        _tmdbMock
            .Setup(t =>
                t.GetTvShowDetailsAsync(It.IsAny<TmdbTvDetailsQuery>(), It.IsAny<CancellationToken>())
            )
            .ReturnsAsync(details);

        await sut.GetTvShowDetailsAsync(1396, null);
        await sut.GetTvShowDetailsAsync(1396, null);

        db.MediaGenres.Where(mg => mg.Media.TmdbId == 1396).Should().HaveCount(1);
    }

    [Fact]
    public async Task GetTvSeasonAsync_ShouldUpsertSeasonAndEpisodes()
    {
        var db = TestHelpers.CreateInMemoryDbContext();
        var seriesId = Guid.NewGuid();
        db.TvSeries.Add(
            new TvSeries { Id = seriesId, TmdbId = 1396, Title = "Breaking Bad" }
        );
        db.SaveChanges();

        var sut = CreateSut(db);

        var season = new TmdbTvSeasonDetails
        {
            Id = 3578,
            Name = "Season 1",
            SeasonNumber = 1,
            AirDate = "2008-01-20",
            Overview = "First season.",
            PosterPath = "/s1.jpg",
            VoteAverage = 8.5,
            Episodes =
            [
                new TmdbTvEpisodeSummary
                {
                    Id = 62085,
                    EpisodeNumber = 1,
                    Name = "Pilot",
                    Overview = "Walt turns to crime.",
                    AirDate = "2008-01-20",
                    Runtime = 58,
                    StillPath = "/still1.jpg",
                    VoteAverage = 8.7,
                },
            ],
        };

        _tmdbMock
            .Setup(t =>
                t.GetTvSeasonAsync(
                    It.Is<TmdbTvSeasonQuery>(q => q.SeriesId == 1396 && q.SeasonNumber == 1),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(season);

        var result = await sut.GetTvSeasonAsync(1396, 1, null);

        result.SeasonNumber.Should().Be(1);
        result.SeriesTmdbId.Should().Be(1396);
        result.Episodes.Should().HaveCount(1);
        result.Episodes[0].Name.Should().Be("Pilot");
        result.Episodes[0].StillUrl.Should().Be("https://image.tmdb.org/t/p/w300/still1.jpg");

        var seasonEntity = db.TvSeasons.Single(s => s.SeriesId == seriesId);
        seasonEntity.TmdbId.Should().Be(3578);
        seasonEntity.Title.Should().Be("Season 1");
        seasonEntity.SeasonNumber.Should().Be(1);

        var episodeEntity = db.TvEpisodes.Single(e => e.SeasonId == seasonEntity.Id);
        episodeEntity.EpisodeNumber.Should().Be(1);
        episodeEntity.Title.Should().Be("Pilot");
        episodeEntity.SeasonNumber.Should().Be(1);
        episodeEntity.Runtime.Should().Be(58);
        episodeEntity.PosterUrl.Should().Be("https://image.tmdb.org/t/p/w300/still1.jpg");
    }

    [Fact]
    public async Task GetTvSeasonAsync_ShouldThrow_WhenSeriesNotCached()
    {
        var db = TestHelpers.CreateInMemoryDbContext();
        var sut = CreateSut(db);

        var act = async () => await sut.GetTvSeasonAsync(1396, 1, null);

        await act.Should().ThrowAsync<NotFoundException>();
        _tmdbMock.Verify(
            t => t.GetTvSeasonAsync(It.IsAny<TmdbTvSeasonQuery>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
    }

    [Fact]
    public async Task GetTvEpisodeAsync_ShouldUpsertEpisode()
    {
        var db = TestHelpers.CreateInMemoryDbContext();
        var seriesId = Guid.NewGuid();
        db.TvSeries.Add(
            new TvSeries { Id = seriesId, TmdbId = 1396, Title = "Breaking Bad" }
        );
        var seasonId = Guid.NewGuid();
        db.TvSeasons.Add(
            new TvSeason
            {
                Id = seasonId,
                SeriesId = seriesId,
                TmdbId = 3578,
                SeasonNumber = 1,
                Title = "Season 1",
            }
        );
        db.SaveChanges();

        var sut = CreateSut(db);

        var episode = new TmdbTvEpisodeDetails
        {
            Id = 62085,
            EpisodeNumber = 1,
            SeasonNumber = 1,
            Name = "Pilot",
            Overview = "Walt turns to crime.",
            AirDate = "2008-01-20",
            Runtime = 58,
            StillPath = "/still1.jpg",
            VoteAverage = 8.7,
        };

        _tmdbMock
            .Setup(t =>
                t.GetTvEpisodeAsync(
                    It.Is<TmdbTvEpisodeQuery>(q =>
                        q.SeriesId == 1396 && q.SeasonNumber == 1 && q.EpisodeNumber == 1
                    ),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(episode);

        var result = await sut.GetTvEpisodeAsync(1396, 1, 1, null);

        result.SeriesTmdbId.Should().Be(1396);
        result.SeriesTitle.Should().Be("Breaking Bad");
        result.Name.Should().Be("Pilot");
        result.EpisodeNumber.Should().Be(1);
        result.Runtime.Should().Be(58);
        result.StillUrl.Should().Be("https://image.tmdb.org/t/p/w300/still1.jpg");

        var episodeEntity = db.TvEpisodes.Single(e => e.SeasonId == seasonId);
        episodeEntity.TmdbId.Should().Be(62085);
        episodeEntity.Title.Should().Be("Pilot");
    }

    [Fact]
    public async Task GetTvEpisodeAsync_ShouldThrow_WhenSeasonNotCached()
    {
        var db = TestHelpers.CreateInMemoryDbContext();
        db.TvSeries.Add(
            new TvSeries { Id = Guid.NewGuid(), TmdbId = 1396, Title = "Breaking Bad" }
        );
        db.SaveChanges();

        var sut = CreateSut(db);

        var act = async () => await sut.GetTvEpisodeAsync(1396, 1, 1, null);

        await act.Should().ThrowAsync<NotFoundException>();
        _tmdbMock.Verify(
            t => t.GetTvEpisodeAsync(It.IsAny<TmdbTvEpisodeQuery>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
    }

    [Fact]
    public async Task GetGenresAsync_ShouldCallTmdbAndMap()
    {
        var db = TestHelpers.CreateInMemoryDbContext();
        var sut = CreateSut(db);

        _tmdbMock
            .Setup(t => t.GetTvGenresAsync(It.IsAny<TmdbGenreListQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                new TmdbGenreListResponse
                {
                    Genres =
                    [
                        new TmdbGenre { Id = 10759, Name = "Action & Adventure" },
                        new TmdbGenre { Id = 18, Name = "Drama" },
                    ],
                }
            );

        var result = await sut.GetGenresAsync(null);

        result.Should().BeOfType<GenresResponseDto>();
        result.Genres.Should().HaveCount(2);
        result.Genres[0].TmdbId.Should().Be(10759);
        result.Genres[0].Name.Should().Be("Action & Adventure");
    }

    [Fact]
    public async Task SearchTvShowsAsync_ShouldHandleEmptyResults()
    {
        var db = TestHelpers.CreateInMemoryDbContext();
        var sut = CreateSut(db);

        _tmdbMock
            .Setup(t => t.SearchTvShowsAsync(It.IsAny<TmdbSearchTvQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                new TmdbPagedResponse<TmdbSearchTvItem>
                {
                    Page = 1,
                    TotalPages = 0,
                    TotalResults = 0,
                    Results = [],
                }
            );

        var result = await sut.SearchTvShowsAsync(
            new SearchTvShowsRequestDto { Query = "nonexistent" }
        );

        result.Results.Should().BeEmpty();
        result.TotalResults.Should().Be(0);
    }
}