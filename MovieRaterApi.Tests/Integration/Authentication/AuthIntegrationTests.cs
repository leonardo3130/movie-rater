using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MovieRaterApi.Data;
using MovieRaterApi.Features.Authentication.DTOs;
using Testcontainers.PostgreSql;

namespace MovieRaterApi.Tests.Integration.Authentication;

public class AuthIntegrationTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgresContainer;
    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _client = null!;

    public AuthIntegrationTests()
    {
        // crea un db postgres senza l.ausilio del docker compose
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
        optionsBuilder.UseNpgsql(_postgresContainer.GetConnectionString()); // npgsql è un driver postgres per dtonet (C#, F#)
        using var db = new ApplicationDbContext(optionsBuilder.Options);
        db.Database.EnsureCreated();
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        _factory.Dispose();
        await _postgresContainer.DisposeAsync();
    }

    private async Task<AuthResponseDto> RegisterUser(string username, string email, string password)
    {
        var request = new RegisterRequestDto
        {
            Username = username,
            Email = email,
            Password = password,
        };

        var response = await _client.PostAsJsonAsync("/api/auth/register", request);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
        return result!;
    }

    [Fact]
    public async Task Register_ShouldCreateUserAndReturnTokens()
    {
        var result = await RegisterUser("newuser", "newuser@example.com", "Password123!");

        result.AccessToken.Should().NotBeNullOrWhiteSpace();
        result.RefreshToken.Should().NotBeNullOrWhiteSpace();
        result.User.Username.Should().Be("newuser");
        result.User.Email.Should().Be("newuser@example.com");
    }

    [Fact]
    public async Task Register_ShouldReturn409_WhenEmailAlreadyExists()
    {
        await RegisterUser("user1", "duplicate@example.com", "Password123!");

        var request = new RegisterRequestDto
        {
            Username = "user2",
            Email = "duplicate@example.com",
            Password = "Password123!",
        };

        var response = await _client.PostAsJsonAsync("/api/auth/register", request);

        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task Register_ShouldReturn400_WhenValidationFails()
    {
        var request = new RegisterRequestDto
        {
            Username = "",
            Email = "invalid",
            Password = "12",
        };

        var response = await _client.PostAsJsonAsync("/api/auth/register", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Login_ShouldReturnTokens_WhenCredentialsAreValid()
    {
        await RegisterUser("loginuser", "loginuser@example.com", "Password123!");

        var request = new LoginRequestDto
        {
            Email = "loginuser@example.com",
            Password = "Password123!",
        };

        var response = await _client.PostAsJsonAsync("/api/auth/login", request);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
        result.Should().NotBeNull();
        result!.AccessToken.Should().NotBeNullOrWhiteSpace();
        result.RefreshToken.Should().NotBeNullOrWhiteSpace();
        result.User.Username.Should().Be("loginuser");
    }

    [Fact]
    public async Task Login_ShouldReturn401_WhenCredentialsAreInvalid()
    {
        await RegisterUser("loginfail", "loginfail@example.com", "Password123!");

        var request = new LoginRequestDto
        {
            Email = "loginfail@example.com",
            Password = "WrongPassword!",
        };

        var response = await _client.PostAsJsonAsync("/api/auth/login", request);

        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task Login_ShouldReturn401_WhenUserNotFound()
    {
        var request = new LoginRequestDto
        {
            Email = "nonexistent@example.com",
            Password = "Password123!",
        };

        var response = await _client.PostAsJsonAsync("/api/auth/login", request);

        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task GetMe_ShouldReturnUserInfo_WhenAuthenticated()
    {
        var registerResult = await RegisterUser("meuser", "meuser@example.com", "Password123!");
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue(
                "Bearer",
                registerResult.AccessToken
            );

        var response = await _client.GetAsync("/api/auth/me");
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<CurrentUserResponseDto>();
        result.Should().NotBeNull();
        result!.Username.Should().Be("meuser");
        result.Email.Should().Be("meuser@example.com");
    }

    [Fact]
    public async Task GetMe_ShouldReturn401_WhenNotAuthenticated()
    {
        _client.DefaultRequestHeaders.Authorization = null;

        var response = await _client.GetAsync("/api/auth/me");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Refresh_ShouldReturnNewTokens()
    {
        var registerResult = await RegisterUser(
            "refreshuser",
            "refreshuser@example.com",
            "Password123!"
        );
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue(
                "Bearer",
                registerResult.AccessToken
            );

        var refreshResponse = await _client.PostAsync("/api/auth/refresh", null);
        refreshResponse.EnsureSuccessStatusCode();

        var result = await refreshResponse.Content.ReadFromJsonAsync<AuthResponseDto>();
        result.Should().NotBeNull();
        result!.AccessToken.Should().NotBeNullOrWhiteSpace();
        result.RefreshToken.Should().NotBeNullOrWhiteSpace();
        result.User.Username.Should().Be("refreshuser");
    }

    [Fact]
    public async Task Logout_ShouldRevokeRefreshToken()
    {
        var registerResult = await RegisterUser(
            "logoutuser",
            "logoutuser@example.com",
            "Password123!"
        );
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue(
                "Bearer",
                registerResult.AccessToken
            );

        var logoutResponse = await _client.PostAsync("/api/auth/logout", null);
        logoutResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Logout_ShouldReturn401_WhenNotAuthenticated()
    {
        _client.DefaultRequestHeaders.Authorization = null;

        var response = await _client.PostAsync("/api/auth/logout", null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task FullLifecycle_Register_Login_GetMe_Logout()
    {
        var registerResult = await RegisterUser(
            "lifecycle",
            "lifecycle@example.com",
            "Password123!"
        );

        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue(
                "Bearer",
                registerResult.AccessToken
            );

        var meResponse = await _client.GetAsync("/api/auth/me");
        meResponse.EnsureSuccessStatusCode();
        var meResult = await meResponse.Content.ReadFromJsonAsync<CurrentUserResponseDto>();
        meResult!.Username.Should().Be("lifecycle");

        var logoutResponse = await _client.PostAsync("/api/auth/logout", null);
        logoutResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task InvitePartner_ShouldReturnInviteToken()
    {
        var user1 = await RegisterUser("inviter", "inviter@example.com", "Password123!");
        await RegisterUser("invitee", "invitee@example.com", "Password123!");

        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", user1.AccessToken);

        var request = new InvitePartnerRequestDto { InviteeEmail = "invitee@example.com" };
        var response = await _client.PostAsJsonAsync("/api/auth/invite", request);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<InviteResponseDto>();
        result.Should().NotBeNull();
        result!.InviteToken.Should().NotBeNullOrWhiteSpace();
        result.InvitationId.Should().NotBeEmpty();
    }

    [Fact]
    public async Task InvitePartner_ShouldReturn400_WhenInviteeNotFound()
    {
        var user1 = await RegisterUser("inviter2", "inviter2@example.com", "Password123!");

        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", user1.AccessToken);

        var request = new InvitePartnerRequestDto { InviteeEmail = "nonexistent@example.com" };
        var response = await _client.PostAsJsonAsync("/api/auth/invite", request);

        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task AcceptInvitation_ShouldCreateCouple()
    {
        var user1 = await RegisterUser("inviter3", "inviter3@example.com", "Password123!");
        var user2 = await RegisterUser("invitee3", "invitee3@example.com", "Password123!");

        // fai la richiesta loggato come user 1
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", user1.AccessToken);

        var inviteRequest = new InvitePartnerRequestDto { InviteeEmail = "invitee3@example.com" };
        var inviteResponse = await _client.PostAsJsonAsync("/api/auth/invite", inviteRequest);
        inviteResponse.EnsureSuccessStatusCode();
        var inviteResult = await inviteResponse.Content.ReadFromJsonAsync<InviteResponseDto>();

        // fai la richiesta loggato come user 2
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", user2.AccessToken);

        var acceptRequest = new AcceptInvitationRequestDto
        {
            InviteToken = inviteResult!.InviteToken,
        };
        var acceptResponse = await _client.PostAsJsonAsync(
            "/api/auth/invite/accept",
            acceptRequest
        );
        acceptResponse.EnsureSuccessStatusCode();

        var meResponse = await _client.GetAsync("/api/auth/me");
        meResponse.EnsureSuccessStatusCode();
        var meResult = await meResponse.Content.ReadFromJsonAsync<CurrentUserResponseDto>();

        meResult!.CoupleId.Should().NotBeNull();
        meResult.Partner.Should().NotBeNull();
        meResult.Partner!.Username.Should().Be("inviter3");
    }

    [Fact]
    public async Task AcceptInvitation_ShouldReturn400_WhenTokenInvalid()
    {
        var user2 = await RegisterUser("invitee4", "invitee4@example.com", "Password123!");

        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", user2.AccessToken);

        var request = new AcceptInvitationRequestDto { InviteToken = "invalid-token" };
        var response = await _client.PostAsJsonAsync("/api/auth/invite/accept", request);

        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
    }
}
