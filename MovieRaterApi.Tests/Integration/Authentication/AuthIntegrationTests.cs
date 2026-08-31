using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using MovieRaterApi.Data;
using MovieRaterApi.Features.Authentication.DTOs;
using MovieRaterApi.Infrastructure.Email;
using Testcontainers.PostgreSql;

namespace MovieRaterApi.Tests.Integration.Authentication;

public class AuthIntegrationTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgresContainer;
    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _client = null!;
    private Mock<IEmailSender> _emailSenderMock = null!;
    private readonly List<(string To, string Subject, string Body)> _sentEmails = new();

    public AuthIntegrationTests()
    {
        // crea un db postgres senza l'ausilio del docker compose
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
            builder.UseSetting("EmailSettings:FrontendBaseUrl", "https://movie-rater.leopo.dev");

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

                var emailDescriptor = services.SingleOrDefault(d =>
                    d.ServiceType == typeof(IEmailSender)
                );
                if (emailDescriptor is not null)
                    services.Remove(emailDescriptor);

                _emailSenderMock = new Mock<IEmailSender>();
                _emailSenderMock
                    .Setup(e => e.SendAsync(
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<CancellationToken>()
                    ))
                    .Callback<string, string, string, CancellationToken>(
                        (to, subject, body, _) =>
                            _sentEmails.Add((to, subject, body))
                    )
                    .Returns(Task.CompletedTask);

                services.AddScoped(_ => _emailSenderMock.Object);
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

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
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

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
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

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
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

        var result = await response.Content.ReadFromJsonAsync<UserResponseDto>();
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
        var meResult = await meResponse.Content.ReadFromJsonAsync<UserResponseDto>();
        meResult!.Username.Should().Be("lifecycle");

        var logoutResponse = await _client.PostAsync("/api/auth/logout", null);
        logoutResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    private async Task<string> RequestPasswordReset(string email)
    {
        _sentEmails.Clear();
        var response = await _client.PostAsJsonAsync(
            "/api/auth/forgot-password",
            new ForgotPasswordRequest { Email = email }
        );
        response.EnsureSuccessStatusCode();

        var sentEmail = _sentEmails.Single(e => e.To == email);
        return ExtractTokenFromBody(sentEmail.Body);
    }

    private static string ExtractTokenFromBody(string body)
    {
        const string marker = "reset-password?token=";
        var start = body.IndexOf(marker, StringComparison.Ordinal) + marker.Length;
        var end = body.IndexOf("\"", start, StringComparison.Ordinal);
        return body[start..end];
    }

    private async Task<int> CountResetTokensAsync(Guid userId)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        return await db.PasswordResetTokens.CountAsync(t => t.UserId == userId);
    }

    [Fact]
    public async Task ForgotPassword_ShouldReturn200AndPersistToken()
    {
        var user = await RegisterUser("pwduser", "pwduser@example.com", "Password123!");

        var response = await _client.PostAsJsonAsync(
            "/api/auth/forgot-password",
            new ForgotPasswordRequest { Email = "pwduser@example.com" }
        );

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        _sentEmails.Should().ContainSingle(e => e.To == "pwduser@example.com");
        (await CountResetTokensAsync(user.User.Id)).Should().Be(1);
    }

    [Fact]
    public async Task ForgotPassword_ShouldReturn200_WhenEmailUnknown()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/auth/forgot-password",
            new ForgotPasswordRequest { Email = "unknown@example.com" }
        );

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        _sentEmails.Should().BeEmpty();
    }

    [Fact]
    public async Task ResetPassword_ShouldAllowLoginWithNewPassword()
    {
        var user = await RegisterUser("resetuser", "resetuser@example.com", "Password123!");

        var token = await RequestPasswordReset("resetuser@example.com");

        var resetResponse = await _client.PostAsJsonAsync(
            "/api/auth/reset-password",
            new ResetPasswordRequest { Token = token, Password = "NewPassword123!" }
        );
        resetResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var loginResponse = await _client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequestDto { Email = "resetuser@example.com", Password = "NewPassword123!" }
        );
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await loginResponse.Content.ReadFromJsonAsync<AuthResponseDto>();
        result!.User.Username.Should().Be("resetuser");
        result.User.Id.Should().Be(user.User.Id);
    }

    [Fact]
    public async Task ResetPassword_ShouldReturn401_WhenTokenInvalid()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/auth/reset-password",
            new ResetPasswordRequest { Token = "invalid-token", Password = "NewPassword123!" }
        );

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ResetPassword_ShouldBeSingleUse()
    {
        await RegisterUser("singleuse", "singleuse@example.com", "Password123!");

        var token = await RequestPasswordReset("singleuse@example.com");

        var first = await _client.PostAsJsonAsync(
            "/api/auth/reset-password",
            new ResetPasswordRequest { Token = token, Password = "NewPassword123!" }
        );
        first.StatusCode.Should().Be(HttpStatusCode.OK);

        var second = await _client.PostAsJsonAsync(
            "/api/auth/reset-password",
            new ResetPasswordRequest { Token = token, Password = "AnotherPassword123!" }
        );
        second.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
