using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MovieRaterApi.Data;
using MovieRaterApi.Data.Entities;
using MovieRaterApi.Features.Authentication.DTOs;
using MovieRaterApi.Features.UserMovie.DTOs;
using Testcontainers.PostgreSql;

namespace MovieRaterApi.Tests.Integration.UserMovie;

public class UserMovieIntegrationTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgresContainer;
    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _client = null!;
    private ApplicationDbContext _db = null!;

    public UserMovieIntegrationTests()
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
                var descriptor = services.SingleOrDefault(d =>
                    d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>)
                );
                if (descriptor is not null)
                    services.Remove(descriptor);

                services.AddDbContext<ApplicationDbContext>(options =>
                    options.UseNpgsql(_postgresContainer.GetConnectionString())
                );
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
    public async Task SetFavorite_ShouldReturn200()
    {
        var (token, movieId) = await SeedMovieAsync("fav1");
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var response = await _client.PostAsync($"/api/user-movies/{movieId}/favorite", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<UserMovieResponseDto>();
        result.Should().NotBeNull();
        result!.IsFavorite.Should().BeTrue();
        result.IsInWatchlist.Should().BeFalse();
        result.MovieId.Should().Be(movieId);
    }

    [Fact]
    public async Task RemoveFavorite_ShouldReturn200()
    {
        var (token, movieId) = await SeedMovieAsync("fav2");
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        await _client.PostAsync($"/api/user-movies/{movieId}/favorite", null);

        var response = await _client.DeleteAsync($"/api/user-movies/{movieId}/favorite");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<UserMovieResponseDto>();
        result.Should().NotBeNull();
        result!.IsFavorite.Should().BeFalse();
        result.IsInWatchlist.Should().BeFalse();
    }

    [Fact]
    public async Task SetWatchlist_ShouldReturn200()
    {
        var (token, movieId) = await SeedMovieAsync("wl1");
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var response = await _client.PostAsync($"/api/user-movies/{movieId}/watchlist", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<UserMovieResponseDto>();
        result.Should().NotBeNull();
        result!.IsInWatchlist.Should().BeTrue();
        result.IsFavorite.Should().BeFalse();
        result.MovieId.Should().Be(movieId);
    }

    [Fact]
    public async Task RemoveWatchlist_ShouldReturn200()
    {
        var (token, movieId) = await SeedMovieAsync("wl2");
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        await _client.PostAsync($"/api/user-movies/{movieId}/watchlist", null);

        var response = await _client.DeleteAsync($"/api/user-movies/{movieId}/watchlist");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<UserMovieResponseDto>();
        result.Should().NotBeNull();
        result!.IsInWatchlist.Should().BeFalse();
        result.IsFavorite.Should().BeFalse();
    }

    [Fact]
    public async Task GetUserMovie_ShouldReturn200()
    {
        var (token, movieId) = await SeedMovieAsync("get1");
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        await _client.PostAsync($"/api/user-movies/{movieId}/favorite", null);

        var response = await _client.GetAsync($"/api/user-movies/{movieId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<UserMovieResponseDto>();
        result.Should().NotBeNull();
        result!.IsFavorite.Should().BeTrue();
        result.IsInWatchlist.Should().BeFalse();
    }

    [Fact]
    public async Task SetFavorite_MovieNotFound_ShouldReturn404()
    {
        var (token, _) = await SeedMovieAsync("nf1");
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var response = await _client.PostAsync($"/api/user-movies/{Guid.NewGuid()}/favorite", null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Unauthenticated_ShouldReturn401()
    {
        _client.DefaultRequestHeaders.Authorization = null;

        var response = await _client.GetAsync($"/api/user-movies/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetUserMovies_ShouldIncludeTvMediaWithSeriesInfo()
    {
        var (token, seasonId, episodeId) = await SeedTvAsync("favtv");

        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var favResponse = await _client.PostAsync($"/api/user-movies/{episodeId}/favorite", null);
        favResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var wlResponse = await _client.PostAsync($"/api/user-movies/{seasonId}/watchlist", null);
        wlResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var favoritesResponse = await _client.GetAsync("/api/user-movies?favoritesOnly=true");
        favoritesResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var favorites = await favoritesResponse.Content.ReadFromJsonAsync<PagedUserMoviesResponseDto>();
        var episodeItem = favorites!.Results.Single(r => r.Id == episodeId);
        episodeItem.MediaType.Should().Be(MediaType.TvEpisode);
        episodeItem.SeriesTmdbId.Should().NotBeNull();
        episodeItem.SeriesTitle.Should().Be("Test Series");
        episodeItem.SeasonNumber.Should().Be(1);
        episodeItem.EpisodeNumber.Should().Be(3);

        var watchlistResponse = await _client.GetAsync("/api/user-movies?watchlistOnly=true");
        watchlistResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var watchlist = await watchlistResponse.Content.ReadFromJsonAsync<PagedUserMoviesResponseDto>();
        var seasonItem = watchlist!.Results.Single(r => r.Id == seasonId);
        seasonItem.MediaType.Should().Be(MediaType.TvSeason);
        seasonItem.SeriesTmdbId.Should().NotBeNull();
        seasonItem.SeriesTitle.Should().Be("Test Series");
        seasonItem.SeasonNumber.Should().Be(1);
        seasonItem.EpisodeNumber.Should().BeNull();
    }

    private async Task<(string token, Guid seasonId, Guid episodeId)> SeedTvAsync(
        string username
    )
    {
        var registerResponse = await _client.PostAsJsonAsync(
            "/api/auth/register",
            new RegisterRequestDto
            {
                Username = username,
                Email = $"{username}@test.com",
                Password = "Password123!",
            }
        );
        registerResponse.EnsureSuccessStatusCode();

        var loginResponse = await _client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequestDto { Email = $"{username}@test.com", Password = "Password123!" }
        );
        loginResponse.EnsureSuccessStatusCode();
        var loginResult = await loginResponse.Content.ReadFromJsonAsync<AuthResponseDto>();

        var seriesId = Guid.NewGuid();
        var seasonId = Guid.NewGuid();
        var episodeId = Guid.NewGuid();
        var seriesTmdbId = new Random().Next(100000, 999999);

        _db.TvSeries.Add(
            new TvSeries
            {
                Id = seriesId,
                TmdbId = seriesTmdbId,
                Title = "Test Series",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            }
        );
        _db.TvSeasons.Add(
            new TvSeason
            {
                Id = seasonId,
                SeriesId = seriesId,
                TmdbId = seriesTmdbId + 1,
                SeasonNumber = 1,
                Title = "Season 1",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            }
        );
        _db.TvEpisodes.Add(
            new TvEpisode
            {
                Id = episodeId,
                SeasonId = seasonId,
                TmdbId = seriesTmdbId + 2,
                SeasonNumber = 1,
                EpisodeNumber = 3,
                Title = "Test Episode",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            }
        );
        await _db.SaveChangesAsync();

        return (loginResult!.AccessToken, seasonId, episodeId);
    }

    private async Task<(string token, Guid movieId)> SeedMovieAsync(string username)
    {
        var registerResponse = await _client.PostAsJsonAsync(
            "/api/auth/register",
            new RegisterRequestDto
            {
                Username = username,
                Email = $"{username}@test.com",
                Password = "Password123!",
            }
        );
        registerResponse.EnsureSuccessStatusCode();

        var loginResponse = await _client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequestDto { Email = $"{username}@test.com", Password = "Password123!" }
        );
        loginResponse.EnsureSuccessStatusCode();
        var loginResult = await loginResponse.Content.ReadFromJsonAsync<AuthResponseDto>();

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
        await _db.SaveChangesAsync();

        return (loginResult!.AccessToken, movieId);
    }
}
