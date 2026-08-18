using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MovieRaterApi.Data;
using MovieRaterApi.Features.Authentication.DTOs;
using MovieRaterApi.Features.Groups.DTOs;
using Testcontainers.PostgreSql;

namespace MovieRaterApi.Tests.Integration.Invitations;

public class InvitationsIntegrationTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgresContainer;
    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _client = null!;
    private ApplicationDbContext _db = null!;

    public InvitationsIntegrationTests()
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
    public async Task AcceptInvitation_ShouldReturn404_WhenTokenInvalid()
    {
        var user2 = await RegisterUser("invitee4", "invitee4@example.com", "Password123!");

        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", user2.AccessToken);

        var request = new AcceptInvitationRequestDto { InviteToken = "invalid-token" };
        var response = await _client.PostAsJsonAsync("/api/auth/invite/accept", request);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task InvitePartner_ShouldReturnInviteToken()
    {
        var user1 = await RegisterUser("inviter", "inviter@example.com", "Password123!");
        await RegisterUser("invitee", "invitee@example.com", "Password123!");

        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", user1.AccessToken);

        var createGroupResponse = await _client.PostAsJsonAsync(
            "/api/groups/",
            new CreateGroupRequest { GroupName = "New group" }
        );

        createGroupResponse.EnsureSuccessStatusCode();
        var group = await createGroupResponse.Content.ReadFromJsonAsync<GroupDto>();

        Assert.NotNull(group);

        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", user1.AccessToken);

        var request = new InvitationRequestDto
        {
            InviteeEmail = "invitee@example.com",
            GroupId = group.Id,
        };
        var response = await _client.PostAsJsonAsync("/api/groups/invite", request);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<InvitationResponseDto>();
        result.Should().NotBeNull();
        result!.InviteToken.Should().NotBeNullOrWhiteSpace();
        result.InvitationId.Should().NotBeEmpty();
    }

    [Fact]
    public async Task InvitePartner_ShouldReturn404_WhenInviteeNotFound()
    {
        var user1 = await RegisterUser("inviter2", "inviter2@example.com", "Password123!");

        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", user1.AccessToken);

        var request = new InvitationRequestDto { InviteeEmail = "nonexistent@example.com" };
        var response = await _client.PostAsJsonAsync("/api/auth/invite", request);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // [Fact]
    // public async Task AcceptInvitation_ShouldJoinGroup()
    // {
    //     var user1 = await RegisterUser("inviter3", "inviter3@example.com", "Password123!");
    //     var user2 = await RegisterUser("invitee3", "invitee3@example.com", "Password123!");
    //
    //     // fai la richiesta loggato come user 1
    //     _client.DefaultRequestHeaders.Authorization =
    //         new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", user1.AccessToken);
    //
    //     var inviteRequest = new InvitationRequestDto { InviteeEmail = "invitee3@example.com" };
    //     var inviteResponse = await _client.PostAsJsonAsync("/api/auth/invite", inviteRequest);
    //     inviteResponse.EnsureSuccessStatusCode();
    //     var inviteResult = await inviteResponse.Content.ReadFromJsonAsync<InvitationResponseDto>();
    //
    //     // fai la richiesta loggato come user 2
    //     _client.DefaultRequestHeaders.Authorization =
    //         new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", user2.AccessToken);
    //
    //     var acceptRequest = new AcceptInvitationRequestDto
    //     {
    //         InviteToken = inviteResult!.InviteToken,
    //     };
    //     var acceptResponse = await _client.PostAsJsonAsync(
    //         "/api/auth/invite/accept",
    //         acceptRequest
    //     );
    //     acceptResponse.EnsureSuccessStatusCode();
    //
    //     var meResponse = await _client.GetAsync("/api/auth/me");
    //     meResponse.EnsureSuccessStatusCode();
    //     var meResult = await meResponse.Content.ReadFromJsonAsync<UserResponseDto>();
    //
    //     meResult!.CoupleId.Should().NotBeNull();
    //     meResult.Partner.Should().NotBeNull();
    //     meResult.Partner!.Username.Should().Be("inviter3");
    // }
    //
}
