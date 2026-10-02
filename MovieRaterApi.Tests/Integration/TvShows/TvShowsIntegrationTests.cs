using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using MovieRaterApi.Data;
using MovieRaterApi.Data.Entities;
using MovieRaterApi.Features.Authentication.DTOs;
using MovieRaterApi.Features.Movies.DTOs;
using MovieRaterApi.Features.TvShows.DTOs;
using MovieRaterApi.Infrastructure.Tmdb;
using MovieRaterApi.Infrastructure.Tmdb.Dtos.Requests;
using MovieRaterApi.Infrastructure.Tmdb.Dtos.Responses;
using Testcontainers.PostgreSql;

namespace MovieRaterApi.Tests.Integration.TvShows;

public class TvShowsIntegrationTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgresContainer;
    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _client = null!;
    private ApplicationDbContext _db = null!;
    private Mock<ITmdbClient> _tmdbMock = null!;

    public TvShowsIntegrationTests()
    {
        _postgresContainer = new PostgreSqlBuilder("postgres:17")
            .WithCleanUp(true)
            .WithDatabase("movierater_test")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .Build();
    }

    public async Task InitializeAsync()
    {
        await _postgresContainer.StartAsync();

        _tmdbMock = new Mock<ITmdbClient>();
        _tmdbMock
            .Setup(t => t.GetConfigurationAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                new TmdbConfiguration
                {
                    Images = new TmdbImageConfig { SecureBaseUrl = "https://image.tmdb.org/t/p/" },
                }
            );

        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseSetting(
                "ConnectionStrings:DefaultConnection",
                _postgresContainer.GetConnectionString()
            );
            builder.UseSetting("Jwt:Issuer", "MovieRaterApi");
            builder.UseSetting("Jwt:Audience", "MovieRaterWeb");
            builder.UseSetting("Jwt:AccessTokenMinutes", "15");
            builder.UseSetting("Jwt:RefreshTokenDays", "30");
            builder.UseSetting(
                "Jwt:SigningKey",
                "test-signing-key-that-is-at-least-32-characters-long-for-testing"
            );

            builder.ConfigureServices(services =>
            {
                var dbDescriptor = services.SingleOrDefault(d =>
                    d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>)
                );
                if (dbDescriptor is not null)
                    services.Remove(dbDescriptor);

                services.AddDbContext<ApplicationDbContext>(options =>
                    options.UseNpgsql(_postgresContainer.GetConnectionString())
                );

                var tmdbDescriptor = services.SingleOrDefault(d =>
                    d.ServiceType == typeof(ITmdbClient)
                );
                if (tmdbDescriptor is not null)
                    services.Remove(tmdbDescriptor);

                services.AddSingleton(_tmdbMock.Object);
            });
        });

        _client = _factory.CreateClient(
            new WebApplicationFactoryClientOptions
            {
                HandleCookies = true,
                BaseAddress = new Uri("https://localhost"),
            }
        );

        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        optionsBuilder.UseNpgsql(_postgresContainer.GetConnectionString());
        _db = new ApplicationDbContext(optionsBuilder.Options);
        _db.Database.EnsureCreated();
    }

    public async Task DisposeAsync()
    {
        await _db.DisposeAsync();
        _client.Dispose();
        _factory.Dispose();
        await _postgresContainer.DisposeAsync();
    }

    [Fact]
    public async Task SearchTvShows_ShouldReturnMappedResults()
    {
        var token = await RegisterAndGetTokenAsync("tvsearch", "tvsearch@test.com");
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        _tmdbMock
            .Setup(t =>
                t.SearchTvShowsAsync(
                    It.IsAny<TmdbSearchTvQuery>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(
                new TmdbPagedResponse<TmdbSearchTvItem>
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
                            PosterPath = "/poster.jpg",
                        },
                    ],
                }
            );

        var response = await _client.GetAsync("/api/tv/search?query=breaking");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PagedTvShowsResponseDto>();
        result!.Results.Should().HaveCount(1);
        result.Results[0].Title.Should().Be("Breaking Bad");
        result.Results[0].PosterUrl.Should().Be("https://image.tmdb.org/t/p/w342/poster.jpg");
    }

    [Fact]
    public async Task GetSeriesDetails_ShouldCacheSeriesInDatabase()
    {
        var token = await RegisterAndGetTokenAsync("tvsdetails", "tvsdetails@test.com");
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        _tmdbMock
            .Setup(t =>
                t.GetTvShowDetailsAsync(
                    It.IsAny<TmdbTvDetailsQuery>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(
                new TmdbTvShowDetails
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
                    PosterPath = "/poster.jpg",
                    Genres = [new TmdbGenre { Id = 18, Name = "Drama" }],
                    Seasons =
                    [
                        new TmdbTvSeasonSummary
                        {
                            Id = 3578,
                            SeasonNumber = 1,
                            EpisodeCount = 7,
                        },
                    ],
                }
            );

        var response = await _client.GetAsync("/api/tv/1396");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<TvShowDetailsResponseDto>();
        result!.Name.Should().Be("Breaking Bad");
        result.Seasons.Should().HaveCount(1);
        result.Seasons[0].EpisodeCount.Should().Be(7);

        var series = await _db.TvSeries.SingleAsync(s => s.TmdbId == 1396);
        series.Title.Should().Be("Breaking Bad");
        series.NumberOfSeasons.Should().Be(5);
        series.Runtime.Should().BeNull();

        var genre = await _db.Genres.SingleAsync(g => g.TmdbId == 18);
        genre.Name.Should().Be("Drama");
        (await _db.MediaGenres.CountAsync(mg => mg.MediaId == series.Id)).Should().Be(1);
    }

    [Fact]
    public async Task GetSeason_ShouldCacheSeasonAndEpisodes()
    {
        var token = await RegisterAndGetTokenAsync("tvseason", "tvseason@test.com");
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var seriesId = Guid.NewGuid();
        _db.TvSeries.Add(
            new TvSeries { Id = seriesId, TmdbId = 1396, Title = "Breaking Bad" }
        );
        await _db.SaveChangesAsync();

        _tmdbMock
            .Setup(t =>
                t.GetTvSeasonAsync(It.IsAny<TmdbTvSeasonQuery>(), It.IsAny<CancellationToken>())
            )
            .ReturnsAsync(
                new TmdbTvSeasonDetails
                {
                    Id = 3578,
                    Name = "Season 1",
                    SeasonNumber = 1,
                    AirDate = "2008-01-20",
                    VoteAverage = 8.5,
                    Episodes =
                    [
                        new TmdbTvEpisodeSummary
                        {
                            Id = 62085,
                            EpisodeNumber = 1,
                            Name = "Pilot",
                            Runtime = 58,
                            StillPath = "/still1.jpg",
                        },
                    ],
                }
            );

        var response = await _client.GetAsync("/api/tv/1396/seasons/1");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<TvSeasonDetailsResponseDto>();
        result!.SeasonNumber.Should().Be(1);
        result.Episodes.Should().HaveCount(1);
        result.Episodes[0].Name.Should().Be("Pilot");

        var season = await _db.TvSeasons.SingleAsync(s => s.SeriesId == seriesId);
        season.TmdbId.Should().Be(3578);
        season.Title.Should().Be("Season 1");

        var episode = await _db.TvEpisodes.SingleAsync(e => e.SeasonId == season.Id);
        episode.Title.Should().Be("Pilot");
        episode.Runtime.Should().Be(58);
    }

    [Fact]
    public async Task GetEpisode_ShouldCacheEpisode()
    {
        var token = await RegisterAndGetTokenAsync("tvepisode", "tvepisode@test.com");
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var seriesId = Guid.NewGuid();
        _db.TvSeries.Add(
            new TvSeries { Id = seriesId, TmdbId = 1396, Title = "Breaking Bad" }
        );
        var seasonId = Guid.NewGuid();
        _db.TvSeasons.Add(
            new TvSeason
            {
                Id = seasonId,
                SeriesId = seriesId,
                TmdbId = 3578,
                SeasonNumber = 1,
                Title = "Season 1",
            }
        );
        await _db.SaveChangesAsync();

        _tmdbMock
            .Setup(t =>
                t.GetTvEpisodeAsync(It.IsAny<TmdbTvEpisodeQuery>(), It.IsAny<CancellationToken>())
            )
            .ReturnsAsync(
                new TmdbTvEpisodeDetails
                {
                    Id = 62085,
                    EpisodeNumber = 1,
                    SeasonNumber = 1,
                    Name = "Pilot",
                    Runtime = 58,
                    StillPath = "/still1.jpg",
                }
            );

        var response = await _client.GetAsync("/api/tv/1396/seasons/1/episodes/1");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<TvEpisodeDetailsResponseDto>();
        result!.Name.Should().Be("Pilot");
        result.SeriesTitle.Should().Be("Breaking Bad");

        var episode = await _db.TvEpisodes.SingleAsync(e => e.SeasonId == seasonId);
        episode.TmdbId.Should().Be(62085);
        episode.Title.Should().Be("Pilot");
    }

    [Fact]
    public async Task GetGenres_ShouldReturnTvGenres()
    {
        var token = await RegisterAndGetTokenAsync("tvgenres", "tvgenres@test.com");
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        _tmdbMock
            .Setup(t => t.GetTvGenresAsync(It.IsAny<TmdbGenreListQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                new TmdbGenreListResponse
                {
                    Genres = [new TmdbGenre { Id = 10759, Name = "Action & Adventure" }],
                }
            );

        var response = await _client.GetAsync("/api/tv/genres");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<GenresResponseDto>();
        result!.Genres.Should().ContainSingle(g => g.Name == "Action & Adventure");
    }

    [Fact]
    public async Task Unauthenticated_ShouldReturn401()
    {
        _client.DefaultRequestHeaders.Authorization = null;

        var response = await _client.GetAsync("/api/tv/search?query=breaking");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private async Task<string> RegisterAndGetTokenAsync(string username, string email)
    {
        _client.DefaultRequestHeaders.Authorization = null;

        var registerResponse = await _client.PostAsJsonAsync(
            "/api/auth/register",
            new RegisterRequestDto
            {
                Username = username,
                Email = email,
                Password = "Password123!",
            }
        );
        registerResponse.EnsureSuccessStatusCode();

        var result = await registerResponse.Content.ReadFromJsonAsync<AuthResponseDto>();
        return result!.AccessToken;
    }
}