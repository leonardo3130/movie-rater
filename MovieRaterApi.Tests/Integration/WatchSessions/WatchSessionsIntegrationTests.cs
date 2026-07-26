using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MovieRaterApi.Data;
using MovieRaterApi.Data.Entities;
using MovieRaterApi.Features.Authentication.DTOs;
using MovieRaterApi.Features.WatchSessions.DTOs;
using Testcontainers.PostgreSql;

namespace MovieRaterApi.Tests.Integration.WatchSessions;

public class WatchSessionsIntegrationTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgresContainer;
    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _client = null!;
    private ApplicationDbContext _db = null!;

    public WatchSessionsIntegrationTests()
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
    public async Task CreateWatchSession_ShouldReturn201()
    {
        var (token, userId, coupleId) = await SeedUserAndCoupleAsync(
            "wscreator",
            "wscreator@test.com"
        );
        var movieId = await SeedMovieAsync(1001, "Inception");
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var request = new CreateWatchSessionRequestDto
        {
            MovieId = movieId,
            WatchedAt = new DateTime(2026, 7, 15, 20, 0, 0, DateTimeKind.Utc),
            Location = "Home",
            Notes = "Amazing movie!",
        };

        var response = await _client.PostAsJsonAsync("/api/watch-sessions", request);
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var result = await response.Content.ReadFromJsonAsync<WatchSessionResponseDto>();
        result.Should().NotBeNull();
        result!.MovieId.Should().Be(movieId);
        result.MovieTitle.Should().Be("Inception");
        result.Location.Should().Be("Home");
        result.Notes.Should().Be("Amazing movie!");
        result.CreatedByUserId.Should().Be(userId);
    }

    [Fact]
    public async Task ListWatchSessions_ShouldReturn200()
    {
        var (token, _, coupleId) = await SeedUserAndCoupleAsync("wslist", "wslist@test.com");
        var movieId = await SeedMovieAsync(1002, "Interstellar");
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync("/api/watch-sessions");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<WatchSessionListResponseDto>();
        result.Should().NotBeNull();
        result!.Items.Should().NotBeNull();
    }

    [Fact]
    public async Task GetWatchSessionById_ShouldReturn200()
    {
        var (token, userId, coupleId) = await SeedUserAndCoupleAsync("wsgetid", "wsgetid@test.com");
        var movieId = await SeedMovieAsync(1003, "The Matrix");
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var createRequest = new CreateWatchSessionRequestDto
        {
            MovieId = movieId,
            WatchedAt = new DateTime(2026, 7, 20, 20, 0, 0, DateTimeKind.Utc),
        };
        var createResponse = await _client.PostAsJsonAsync("/api/watch-sessions", createRequest);
        var created = await createResponse.Content.ReadFromJsonAsync<WatchSessionResponseDto>();

        var response = await _client.GetAsync($"/api/watch-sessions/{created!.Id}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<WatchSessionResponseDto>();
        result!.MovieTitle.Should().Be("The Matrix");
    }

    [Fact]
    public async Task DeleteOwnWatchSession_ShouldReturn204()
    {
        var (token, userId, coupleId) = await SeedUserAndCoupleAsync("wsdel", "wsdel@test.com");
        var movieId = await SeedMovieAsync(1004, "Tenet");
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var createRequest = new CreateWatchSessionRequestDto
        {
            MovieId = movieId,
            WatchedAt = new DateTime(2026, 7, 25, 20, 0, 0, DateTimeKind.Utc),
        };
        var createResponse = await _client.PostAsJsonAsync("/api/watch-sessions", createRequest);
        var created = await createResponse.Content.ReadFromJsonAsync<WatchSessionResponseDto>();

        var response = await _client.DeleteAsync($"/api/watch-sessions/{created!.Id}");
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Heatmap_ShouldReturn200()
    {
        var (token, userId, coupleId) = await SeedUserAndCoupleAsync("wsheat", "wsheat@test.com");
        var movieId = await SeedMovieAsync(1005, "Dunkirk");
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync("/api/watch-sessions/heatmap");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<HeatmapResponseDto>();
        result.Should().NotBeNull();
        result!.DailyCounts.Should().NotBeNull();
    }

    [Fact]
    public async Task Unauthenticated_ShouldReturn401()
    {
        _client.DefaultRequestHeaders.Authorization = null;

        var response = await _client.GetAsync("/api/watch-sessions");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private async Task<(string token, Guid userId, Guid coupleId)> SeedUserAndCoupleAsync(
        string username,
        string email
    )
    {
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

        var partnerResponse = await _client.PostAsJsonAsync(
            "/api/auth/register",
            new RegisterRequestDto
            {
                Username = $"{username}_partner",
                Email = $"partner_{email}",
                Password = "Password123!",
            }
        );
        partnerResponse.EnsureSuccessStatusCode();

        var userResult = await registerResponse.Content.ReadFromJsonAsync<AuthResponseDto>();
        var partnerResult = await partnerResponse.Content.ReadFromJsonAsync<AuthResponseDto>();

        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue(
                "Bearer",
                userResult!.AccessToken
            );

        var inviteResponse = await _client.PostAsJsonAsync(
            "/api/auth/invite",
            new InvitePartnerRequestDto { InviteeEmail = $"partner_{email}" }
        );
        inviteResponse.EnsureSuccessStatusCode();
        var inviteResult = await inviteResponse.Content.ReadFromJsonAsync<InviteResponseDto>();

        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue(
                "Bearer",
                partnerResult!.AccessToken
            );

        var acceptResponse = await _client.PostAsJsonAsync(
            "/api/auth/invite/accept",
            new AcceptInvitationRequestDto { InviteToken = inviteResult!.InviteToken }
        );
        acceptResponse.EnsureSuccessStatusCode();

        // Re-login as user to get fresh JWT with coupleId claim
        _client.DefaultRequestHeaders.Authorization = null;
        var loginResponse = await _client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequestDto { Email = email, Password = "Password123!" }
        );
        loginResponse.EnsureSuccessStatusCode();
        var loginResult = await loginResponse.Content.ReadFromJsonAsync<AuthResponseDto>();

        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue(
                "Bearer",
                loginResult!.AccessToken
            );

        var meResponse = await _client.GetAsync("/api/auth/me");
        var meResult = await meResponse.Content.ReadFromJsonAsync<CurrentUserResponseDto>();

        return (loginResult.AccessToken, meResult!.Id, meResult.CoupleId!.Value);
    }

    private async Task<Guid> SeedMovieAsync(int tmdbId, string title)
    {
        var movie = new Movie
        {
            Id = Guid.NewGuid(),
            TmdbId = tmdbId,
            Title = title,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
        _db.Movies.Add(movie);
        await _db.SaveChangesAsync();
        return movie.Id;
    }
}
