using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using MovieRaterApi.Data.Entities;
using MovieRaterApi.Features.Authentication.Infrastructure;
using MovieRaterApi.Features.Movies.DTOs;
using MovieRaterApi.Features.Movies.Services;
using MovieRaterApi.Infrastructure.Tmdb;
using MovieRaterApi.Infrastructure.Tmdb.Dtos.Requests;
using MovieRaterApi.Infrastructure.Tmdb.Dtos.Responses;

namespace MovieRaterApi.Tests.Unit.Services;

public class MovieServiceTests
{
    private readonly Mock<ITmdbClient> _tmdbMock;
    private readonly Mock<ICurrentUser> _currentUserMock;
    private readonly Mock<ILogger<MovieService>> _loggerMock;
    private readonly IMemoryCache _cache;
    private readonly MovieService _sut;
    private readonly TmdbImageConfig _imageConfig;

    public MovieServiceTests()
    {
        var db = TestHelpers.CreateInMemoryDbContext();
        _tmdbMock = new Mock<ITmdbClient>();
        _currentUserMock = new Mock<ICurrentUser>();
        _loggerMock = new Mock<ILogger<MovieService>>();
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
        _currentUserMock.Setup(u => u.CoupleId).Returns(Guid.NewGuid());

        _sut = new MovieService(
            _tmdbMock.Object,
            db,
            _currentUserMock.Object,
            _cache,
            _loggerMock.Object
        );
    }

    [Fact]
    public async Task SearchMoviesAsync_ShouldCallTmdbAndMapResults()
    {
        var tmdbResponse = new TmdbPagedResponse<TmdbSearchMovieItem>
        {
            Page = 1,
            TotalPages = 1,
            TotalResults = 1,
            Results =
            [
                new TmdbSearchMovieItem
                {
                    Id = 550,
                    Title = "Fight Club",
                    Overview = "A movie.",
                    ReleaseDate = "1999-10-15",
                    PosterPath = "/poster.jpg",
                    BackdropPath = "/backdrop.jpg",
                    VoteAverage = 8.433,
                    VoteCount = 26279,
                    GenreIds = [18, 53],
                },
            ],
        };

        _tmdbMock
            .Setup(t =>
                t.SearchMoviesAsync(
                    It.Is<TmdbSearchMovieQuery>(q => q.Query == "fight club"),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(tmdbResponse);

        var request = new SearchMoviesRequestDto { Query = "fight club", Page = 1 };

        var result = await _sut.SearchMoviesAsync(request);

        result.Page.Should().Be(1);
        result.Results.Should().HaveCount(1);
        var movie = result.Results[0];
        movie.TmdbId.Should().Be(550);
        movie.Title.Should().Be("Fight Club");
        movie.PosterUrl.Should().Be("https://image.tmdb.org/t/p/w342/poster.jpg");
        movie.BackdropUrl.Should().Be("https://image.tmdb.org/t/p/w780/backdrop.jpg");
        movie.ReleaseDate.Should().Be("1999-10-15");
        movie.VoteAverage.Should().Be(8.433);
        movie.GenreIds.Should().BeEquivalentTo([18, 53]);
    }

    [Fact]
    public async Task SearchMoviesAsync_ShouldEnrichWithUserData()
    {
        var userId = Guid.NewGuid();
        var coupleId = Guid.NewGuid();
        _currentUserMock.Setup(u => u.UserId).Returns(userId);
        _currentUserMock.Setup(u => u.CoupleId).Returns(coupleId);

        var db = TestHelpers.CreateInMemoryDbContext();
        var movieId = Guid.NewGuid();
        db.Movies.Add(
            new Movie
            {
                Id = movieId,
                TmdbId = 550,
                Title = "Fight Club",
            }
        );
        db.UserMovies.Add(
            new UserMovie
            {
                UserId = userId,
                MovieId = movieId,
                IsFavorite = true,
                IsInWatchlist = false,
            }
        );
        db.WatchSessions.Add(
            new WatchSession
            {
                Id = Guid.NewGuid(),
                CoupleId = coupleId,
                MovieId = movieId,
                WatchedAt = DateTime.UtcNow,
            }
        );
        db.SaveChanges();

        var svc = new MovieService(
            _tmdbMock.Object,
            db,
            _currentUserMock.Object,
            _cache,
            _loggerMock.Object
        );

        var tmdbResponse = new TmdbPagedResponse<TmdbSearchMovieItem>
        {
            Page = 1,
            TotalPages = 1,
            TotalResults = 1,
            Results = [new TmdbSearchMovieItem { Id = 550, Title = "Fight Club" }],
        };

        _tmdbMock
            .Setup(t =>
                t.SearchMoviesAsync(It.IsAny<TmdbSearchMovieQuery>(), It.IsAny<CancellationToken>())
            )
            .ReturnsAsync(tmdbResponse);

        var request = new SearchMoviesRequestDto { Query = "fight club" };
        var result = await svc.SearchMoviesAsync(request);

        result.Results[0].IsFavorite.Should().BeTrue();
        result.Results[0].IsInWatchlist.Should().BeFalse();
        result.Results[0].WatchedCount.Should().Be(1);
    }

    [Fact]
    public async Task DiscoverMoviesAsync_ShouldMapGenreIdsAndSortBy()
    {
        _tmdbMock
            .Setup(t =>
                t.GetDiscoverMoviesAsync(
                    It.Is<TmdbDiscoverMovieQuery>(q =>
                        q.WithGenres == "28,12"
                        && q.SortBy == "vote_average.desc"
                        && q.VoteAverageGte == 7.0
                        && q.PrimaryReleaseYear == "2020"
                    ),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(
                new TmdbPagedResponse<TmdbSearchMovieItem>
                {
                    Page = 1,
                    TotalPages = 0,
                    TotalResults = 0,
                    Results = [],
                }
            );

        var request = new DiscoverMoviesRequestDto
        {
            GenreIds = "28,12",
            SortBy = "vote_average.desc",
            VoteAverageGte = 7.0,
            PrimaryReleaseYear = "2020",
        };

        var result = await _sut.DiscoverMoviesAsync(request);

        result.Should().NotBeNull();
        _tmdbMock.Verify(
            t =>
                t.GetDiscoverMoviesAsync(
                    It.IsAny<TmdbDiscoverMovieQuery>(),
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );
    }

    [Fact]
    public async Task GetPopularMoviesAsync_ShouldCallTmdbAndMap()
    {
        _tmdbMock
            .Setup(t =>
                t.GetPopularMoviesAsync(
                    It.IsAny<TmdbMovieListQuery>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(
                new TmdbPagedResponse<TmdbSearchMovieItem>
                {
                    Page = 1,
                    TotalPages = 1,
                    TotalResults = 2,
                    Results =
                    [
                        new TmdbSearchMovieItem
                        {
                            Id = 1,
                            Title = "Movie A",
                            PosterPath = "/a.jpg",
                        },
                        new TmdbSearchMovieItem
                        {
                            Id = 2,
                            Title = "Movie B",
                            PosterPath = "/b.jpg",
                        },
                    ],
                }
            );

        var request = new MovieListRequestDto { Page = 1 };
        var result = await _sut.GetPopularMoviesAsync(request);

        result.Results.Should().HaveCount(2);
        result.Results[0].PosterUrl.Should().Be("https://image.tmdb.org/t/p/w342/a.jpg");
    }

    [Fact]
    public async Task GetNowPlayingMoviesAsync_ShouldCallTmdb()
    {
        _tmdbMock
            .Setup(t =>
                t.GetNowPlayingMoviesAsync(
                    It.IsAny<TmdbMovieListQuery>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(
                new TmdbPagedResponse<TmdbSearchMovieItem>
                {
                    Page = 1,
                    TotalPages = 1,
                    TotalResults = 0,
                    Results = [],
                }
            );

        var result = await _sut.GetNowPlayingMoviesAsync(new MovieListRequestDto());

        result.Should().NotBeNull();
        _tmdbMock.Verify(
            t =>
                t.GetNowPlayingMoviesAsync(
                    It.IsAny<TmdbMovieListQuery>(),
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );
    }

    [Fact]
    public async Task GetTopRatedMoviesAsync_ShouldCallTmdb()
    {
        _tmdbMock
            .Setup(t =>
                t.GetTopRatedMoviesAsync(
                    It.IsAny<TmdbMovieListQuery>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(
                new TmdbPagedResponse<TmdbSearchMovieItem>
                {
                    Page = 1,
                    TotalPages = 1,
                    TotalResults = 0,
                    Results = [],
                }
            );

        var result = await _sut.GetTopRatedMoviesAsync(new MovieListRequestDto());

        result.Should().NotBeNull();
        _tmdbMock.Verify(
            t =>
                t.GetTopRatedMoviesAsync(
                    It.IsAny<TmdbMovieListQuery>(),
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );
    }

    [Fact]
    public async Task GetMovieDetailsAsync_ShouldReturnFullDetails()
    {
        var tmdbDetails = new TmdbMovieDetails
        {
            Id = 550,
            Title = "Fight Club",
            Overview = "A movie.",
            ReleaseDate = "1999-10-15",
            Runtime = 139,
            PosterPath = "/poster.jpg",
            BackdropPath = "/backdrop.jpg",
            VoteAverage = 8.433,
            VoteCount = 26279,
            Tagline = "Mischief. Mayhem. Soap.",
            Status = "Released",
            Budget = 63000000,
            Revenue = 100853753,
            Genres =
            [
                new TmdbGenre { Id = 18, Name = "Drama" },
                new TmdbGenre { Id = 53, Name = "Thriller" },
            ],
        };

        _tmdbMock
            .Setup(t =>
                t.GetMovieDetailsAsync(
                    It.Is<TmdbMovieDetailsQuery>(q => q.MovieId == 550),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(tmdbDetails);

        _tmdbMock
            .Setup(t =>
                t.GetMovieCreditsAsync(
                    It.IsAny<TmdbMovieCreditsQuery>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(
                new TmdbCreditsResponse
                {
                    Id = 550,
                    Cast =
                    [
                        new TmdbCastMember
                        {
                            Id = 819,
                            Name = "Edward Norton",
                            Character = "The Narrator",
                            Order = 0,
                            ProfilePath = "/profile.jpg",
                        },
                    ],
                    Crew =
                    [
                        new TmdbCrewMember
                        {
                            Id = 376,
                            Name = "David Fincher",
                            Department = "Directing",
                            Job = "Director",
                        },
                    ],
                }
            );

        _tmdbMock
            .Setup(t =>
                t.GetMovieVideosAsync(
                    It.IsAny<TmdbMovieVideosQuery>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(
                new TmdbVideosResponse
                {
                    Id = 550,
                    Results =
                    [
                        new TmdbVideo
                        {
                            Key = "O-b2VfmmbyA",
                            Site = "YouTube",
                            Type = "Trailer",
                            Name = "Fight Club Trailer",
                            Official = true,
                        },
                    ],
                }
            );

        var result = await _sut.GetMovieDetailsAsync(550, null);

        result.TmdbId.Should().Be(550);
        result.Title.Should().Be("Fight Club");
        result.PosterUrl.Should().Be("https://image.tmdb.org/t/p/w342/poster.jpg");
        result.BackdropUrl.Should().Be("https://image.tmdb.org/t/p/w780/backdrop.jpg");
        result.Runtime.Should().Be(139);
        result.Tagline.Should().Be("Mischief. Mayhem. Soap.");
        result.Genres.Should().HaveCount(2);
        result.Genres[0].Name.Should().Be("Drama");
        result.Cast.Should().HaveCount(1);
        result.Cast[0].Name.Should().Be("Edward Norton");
        result.Cast[0].Character.Should().Be("The Narrator");
        result.Crew.Should().HaveCount(1);
        result.Crew[0].Name.Should().Be("David Fincher");
        result.Videos.Should().HaveCount(1);
        result.Videos[0].Key.Should().Be("O-b2VfmmbyA");
    }

    [Fact]
    public async Task GetMovieDetailsAsync_ShouldUpsertMovieInDb()
    {
        var db = TestHelpers.CreateInMemoryDbContext();
        var svc = new MovieService(
            _tmdbMock.Object,
            db,
            _currentUserMock.Object,
            _cache,
            _loggerMock.Object
        );

        var tmdbDetails = new TmdbMovieDetails
        {
            Id = 550,
            Title = "Fight Club",
            ReleaseDate = "1999-10-15",
            Runtime = 139,
            PosterPath = "/poster.jpg",
            VoteAverage = 8.433,
            Genres = [new TmdbGenre { Id = 18, Name = "Drama" }],
        };

        _tmdbMock
            .Setup(t =>
                t.GetMovieDetailsAsync(
                    It.Is<TmdbMovieDetailsQuery>(q => q.MovieId == 550),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(tmdbDetails);

        _tmdbMock
            .Setup(t =>
                t.GetMovieCreditsAsync(
                    It.IsAny<TmdbMovieCreditsQuery>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(
                new TmdbCreditsResponse
                {
                    Id = 550,
                    Cast = [],
                    Crew = [],
                }
            );

        _tmdbMock
            .Setup(t =>
                t.GetMovieVideosAsync(
                    It.IsAny<TmdbMovieVideosQuery>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(new TmdbVideosResponse { Id = 550, Results = [] });

        await svc.GetMovieDetailsAsync(550, null);

        db.Movies.Should().Contain(m => m.TmdbId == 550 && m.Title == "Fight Club");
        db.Genres.Should().Contain(g => g.TmdbId == 18 && g.Name == "Drama");
        db.MovieGenres.Should().Contain(mg => mg.Movie.TmdbId == 550 && mg.Genre.TmdbId == 18);
    }

    [Fact]
    public async Task GetMovieDetailsAsync_ShouldUpdateExistingMovie()
    {
        var db = TestHelpers.CreateInMemoryDbContext();
        var movieId = Guid.NewGuid();
        var genreId = Guid.NewGuid();
        db.Movies.Add(
            new Movie
            {
                Id = movieId,
                TmdbId = 550,
                Title = "Old Title",
                AverageTmdbRating = 5.0,
            }
        );
        db.Genres.Add(
            new Genre
            {
                Id = genreId,
                TmdbId = 18,
                Name = "Old Genre",
            }
        );
        db.MovieGenres.Add(new MovieGenre { MovieId = movieId, GenreId = genreId });
        db.SaveChanges();

        var svc = new MovieService(
            _tmdbMock.Object,
            db,
            _currentUserMock.Object,
            _cache,
            _loggerMock.Object
        );

        var tmdbDetails = new TmdbMovieDetails
        {
            Id = 550,
            Title = "Fight Club",
            ReleaseDate = "1999-10-15",
            Runtime = 139,
            PosterPath = "/poster.jpg",
            VoteAverage = 8.433,
            Genres =
            [
                new TmdbGenre { Id = 18, Name = "Drama" },
                new TmdbGenre { Id = 53, Name = "Thriller" },
            ],
        };

        _tmdbMock
            .Setup(t =>
                t.GetMovieDetailsAsync(
                    It.Is<TmdbMovieDetailsQuery>(q => q.MovieId == 550),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(tmdbDetails);

        _tmdbMock
            .Setup(t =>
                t.GetMovieCreditsAsync(
                    It.IsAny<TmdbMovieCreditsQuery>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(
                new TmdbCreditsResponse
                {
                    Id = 550,
                    Cast = [],
                    Crew = [],
                }
            );

        _tmdbMock
            .Setup(t =>
                t.GetMovieVideosAsync(
                    It.IsAny<TmdbMovieVideosQuery>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(new TmdbVideosResponse { Id = 550, Results = [] });

        await svc.GetMovieDetailsAsync(550, null);

        var updated = db.Movies.Single(m => m.TmdbId == 550);
        updated.Title.Should().Be("Fight Club");
        updated.AverageTmdbRating.Should().Be(8.433);

        db.Genres.Should().Contain(g => g.TmdbId == 53 && g.Name == "Thriller");
        db.MovieGenres.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetMovieDetailsAsync_ShouldNotDuplicateGenreLinks()
    {
        var db = TestHelpers.CreateInMemoryDbContext();
        var svc = new MovieService(
            _tmdbMock.Object,
            db,
            _currentUserMock.Object,
            _cache,
            _loggerMock.Object
        );

        var tmdbDetails = new TmdbMovieDetails
        {
            Id = 550,
            Title = "Fight Club",
            Runtime = 139,
            VoteAverage = 8.433,
            Genres = [new TmdbGenre { Id = 18, Name = "Drama" }],
        };

        _tmdbMock
            .Setup(t =>
                t.GetMovieDetailsAsync(
                    It.Is<TmdbMovieDetailsQuery>(q => q.MovieId == 550),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(tmdbDetails);

        _tmdbMock
            .Setup(t =>
                t.GetMovieCreditsAsync(
                    It.IsAny<TmdbMovieCreditsQuery>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(
                new TmdbCreditsResponse
                {
                    Id = 550,
                    Cast = [],
                    Crew = [],
                }
            );

        _tmdbMock
            .Setup(t =>
                t.GetMovieVideosAsync(
                    It.IsAny<TmdbMovieVideosQuery>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(new TmdbVideosResponse { Id = 550, Results = [] });

        await svc.GetMovieDetailsAsync(550, null);
        await svc.GetMovieDetailsAsync(550, null);

        db.MovieGenres.Where(mg => mg.Movie.TmdbId == 550).Should().HaveCount(1);
    }

    [Fact]
    public async Task GetMovieRecommendationsAsync_ShouldCallTmdbWithCorrectMovieId()
    {
        _tmdbMock
            .Setup(t =>
                t.GetMovieRecommendationsAsync(
                    It.Is<TmdbMovieRecommendationsQuery>(q => q.MovieId == 550),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(
                new TmdbPagedResponse<TmdbMovieDetails>
                {
                    Page = 1,
                    TotalPages = 1,
                    TotalResults = 1,
                    Results =
                    [
                        new TmdbMovieDetails
                        {
                            Id = 680,
                            Title = "Pulp Fiction",
                            PosterPath = "/pf.jpg",
                        },
                    ],
                }
            );

        var result = await _sut.GetMovieRecommendationsAsync(550, 1, null);

        result.Results.Should().HaveCount(1);
        result.Results[0].Title.Should().Be("Pulp Fiction");
        result.Results[0].PosterUrl.Should().Be("https://image.tmdb.org/t/p/w342/pf.jpg");
    }

    [Fact]
    public async Task GetGenresAsync_ShouldCallTmdbAndMap()
    {
        _tmdbMock
            .Setup(t =>
                t.GetMovieGenresAsync(It.IsAny<TmdbGenreListQuery>(), It.IsAny<CancellationToken>())
            )
            .ReturnsAsync(
                new TmdbGenreListResponse
                {
                    Genres =
                    [
                        new TmdbGenre { Id = 28, Name = "Action" },
                        new TmdbGenre { Id = 12, Name = "Adventure" },
                    ],
                }
            );

        var result = await _sut.GetGenresAsync(null);

        result.Genres.Should().HaveCount(2);
        result.Genres[0].TmdbId.Should().Be(28);
        result.Genres[0].Name.Should().Be("Action");
    }

    [Fact]
    public async Task ImageConfig_ShouldBeCachedAcrossCalls()
    {
        _tmdbMock
            .Setup(t =>
                t.SearchMoviesAsync(It.IsAny<TmdbSearchMovieQuery>(), It.IsAny<CancellationToken>())
            )
            .ReturnsAsync(
                new TmdbPagedResponse<TmdbSearchMovieItem>
                {
                    Page = 1,
                    TotalPages = 1,
                    TotalResults = 0,
                    Results = [],
                }
            );

        await _sut.SearchMoviesAsync(new SearchMoviesRequestDto { Query = "a" });
        await _sut.SearchMoviesAsync(new SearchMoviesRequestDto { Query = "b" });
        await _sut.SearchMoviesAsync(new SearchMoviesRequestDto { Query = "c" });

        _tmdbMock.Verify(t => t.GetConfigurationAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SearchMoviesAsync_ShouldHandleEmptyResults()
    {
        _tmdbMock
            .Setup(t =>
                t.SearchMoviesAsync(It.IsAny<TmdbSearchMovieQuery>(), It.IsAny<CancellationToken>())
            )
            .ReturnsAsync(
                new TmdbPagedResponse<TmdbSearchMovieItem>
                {
                    Page = 1,
                    TotalPages = 0,
                    TotalResults = 0,
                    Results = [],
                }
            );

        var result = await _sut.SearchMoviesAsync(
            new SearchMoviesRequestDto { Query = "nonexistent" }
        );

        result.Results.Should().BeEmpty();
        result.TotalResults.Should().Be(0);
    }

    [Fact]
    public async Task GetPopularMoviesAsync_ShouldHandleNullPosterPath()
    {
        _tmdbMock
            .Setup(t =>
                t.GetPopularMoviesAsync(
                    It.IsAny<TmdbMovieListQuery>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(
                new TmdbPagedResponse<TmdbSearchMovieItem>
                {
                    Page = 1,
                    TotalPages = 1,
                    TotalResults = 1,
                    Results =
                    [
                        new TmdbSearchMovieItem
                        {
                            Id = 1,
                            Title = "No Poster",
                            PosterPath = null,
                        },
                    ],
                }
            );

        var result = await _sut.GetPopularMoviesAsync(new MovieListRequestDto());

        result.Results[0].PosterUrl.Should().BeNull();
    }

    [Fact]
    public async Task GetMovieDetailsAsync_ShouldEnrichWithUserData()
    {
        var userId = Guid.NewGuid();
        var coupleId = Guid.NewGuid();
        _currentUserMock.Setup(u => u.UserId).Returns(userId);
        _currentUserMock.Setup(u => u.CoupleId).Returns(coupleId);

        var db = TestHelpers.CreateInMemoryDbContext();
        var movieId = Guid.NewGuid();
        db.Movies.Add(
            new Movie
            {
                Id = movieId,
                TmdbId = 550,
                Title = "Fight Club",
            }
        );
        db.UserMovies.Add(
            new UserMovie
            {
                UserId = userId,
                MovieId = movieId,
                IsFavorite = true,
                IsInWatchlist = false,
            }
        );
        db.WatchSessions.Add(
            new WatchSession
            {
                Id = Guid.NewGuid(),
                CoupleId = coupleId,
                MovieId = movieId,
                WatchedAt = DateTime.UtcNow,
            }
        );
        db.SaveChanges();

        var svc = new MovieService(
            _tmdbMock.Object,
            db,
            _currentUserMock.Object,
            _cache,
            _loggerMock.Object
        );

        var tmdbDetails = new TmdbMovieDetails
        {
            Id = 550,
            Title = "Fight Club",
            VoteAverage = 8.433,
            Genres = [],
        };

        _tmdbMock
            .Setup(t =>
                t.GetMovieDetailsAsync(
                    It.Is<TmdbMovieDetailsQuery>(q => q.MovieId == 550),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(tmdbDetails);

        _tmdbMock
            .Setup(t =>
                t.GetMovieCreditsAsync(
                    It.IsAny<TmdbMovieCreditsQuery>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(
                new TmdbCreditsResponse
                {
                    Id = 550,
                    Cast = [],
                    Crew = [],
                }
            );

        _tmdbMock
            .Setup(t =>
                t.GetMovieVideosAsync(
                    It.IsAny<TmdbMovieVideosQuery>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(new TmdbVideosResponse { Id = 550, Results = [] });

        var result = await svc.GetMovieDetailsAsync(550, null);

        result.IsFavorite.Should().BeTrue();
        result.IsInWatchlist.Should().BeFalse();
        result.WatchedCount.Should().Be(1);
    }
}
