using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MovieRaterApi.Data;
using MovieRaterApi.Data.Entities;
using MovieRaterApi.Features.Authentication.DTOs;
using MovieRaterApi.Features.Ratings.DTOs;
using MovieRaterApi.Features.WatchSessions.DTOs;
using Testcontainers.PostgreSql;

namespace MovieRaterApi.Tests.Integration.Ratings;

public class RatingsIntegrationTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgresContainer;
    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _client = null!;
    private ApplicationDbContext _db = null!;

    public RatingsIntegrationTests()
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
    public async Task CreateRating_ShouldReturn201()
    {
        var (token, userId, coupleId, sessionId) = await SeedSessionAsync(
            "rtcreator",
            "rtcreator@test.com"
        );
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var request = new CreateRatingRequestDto { RatingValue = 8, Review = "Great movie!" };

        var response = await _client.PostAsJsonAsync(
            $"/api/watch-sessions/{sessionId}/ratings",
            request
        );
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var result = await response.Content.ReadFromJsonAsync<RatingResponseDto>();
        result.Should().NotBeNull();
        result!.RatingValue.Should().Be(8);
        result.Review.Should().Be("Great movie!");
    }

    [Fact]
    public async Task UpdateRating_ShouldReturn200()
    {
        var (token, userId, coupleId, sessionId) = await SeedSessionAsync(
            "rtupdate",
            "rtupdate@test.com"
        );
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        await _client.PostAsJsonAsync(
            $"/api/watch-sessions/{sessionId}/ratings",
            new CreateRatingRequestDto { RatingValue = 5, Review = "Average" }
        );

        var updateRequest = new UpdateRatingRequestDto
        {
            RatingValue = 9,
            Review = "Actually great!",
        };

        var response = await _client.PutAsJsonAsync(
            $"/api/watch-sessions/{sessionId}/ratings",
            updateRequest
        );
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<RatingResponseDto>();
        result!.RatingValue.Should().Be(9);
        result.Review.Should().Be("Actually great!");
    }

    [Fact]
    public async Task GetSessionRatings_ShouldReturn200()
    {
        var (token, userId, coupleId, sessionId) = await SeedSessionAsync(
            "rtget",
            "rtget@test.com"
        );
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        await _client.PostAsJsonAsync(
            $"/api/watch-sessions/{sessionId}/ratings",
            new CreateRatingRequestDto { RatingValue = 8, Review = "Great!" }
        );

        var response = await _client.GetAsync($"/api/watch-sessions/{sessionId}/ratings");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<SessionRatingsResponseDto>();
        result.Should().NotBeNull();
        result!.WatchSessionId.Should().Be(sessionId);
        result.Ratings.Should().HaveCount(1);
    }

    [Fact]
    public async Task DuplicateRating_ShouldReturn409()
    {
        var (token, userId, coupleId, sessionId) = await SeedSessionAsync(
            "rtdup",
            "rtdup@test.com"
        );
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        await _client.PostAsJsonAsync(
            $"/api/watch-sessions/{sessionId}/ratings",
            new CreateRatingRequestDto { RatingValue = 7 }
        );

        var response = await _client.PostAsJsonAsync(
            $"/api/watch-sessions/{sessionId}/ratings",
            new CreateRatingRequestDto { RatingValue = 9 }
        );

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Unauthenticated_ShouldReturn401()
    {
        _client.DefaultRequestHeaders.Authorization = null;

        var response = await _client.GetAsync(
            "/api/watch-sessions/00000000-0000-0000-0000-000000000000/ratings"
        );
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private async Task<(string token, Guid userId, Guid coupleId, Guid sessionId)> SeedSessionAsync(
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

        var createSessionRequest = new CreateWatchSessionRequestDto
        {
            MovieId = movieId,
            WatchedAt = DateTime.UtcNow,
        };
        var createResponse = await _client.PostAsJsonAsync(
            "/api/watch-sessions",
            createSessionRequest
        );
        createResponse.EnsureSuccessStatusCode();
        var sessionResult =
            await createResponse.Content.ReadFromJsonAsync<WatchSessionResponseDto>();

        return (loginResult.AccessToken, meResult!.Id, meResult.CoupleId!.Value, sessionResult!.Id);
    }
}
