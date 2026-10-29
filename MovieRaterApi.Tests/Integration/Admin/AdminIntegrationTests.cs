using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MovieRaterApi.Data;
using MovieRaterApi.Data.Entities;
using MovieRaterApi.Features.Admin.DTOs;
using MovieRaterApi.Features.Authentication.DTOs;
using MovieRaterApi.Features.Authentication.Services;
using MovieRaterApi.Features.Groups.DTOs;
using Testcontainers.PostgreSql;

namespace MovieRaterApi.Tests.Integration.Admin;

public class AdminIntegrationTests : IAsyncLifetime
{
    private const string AdminEmail = "admin@example.com";
    private const string Password = "Password123!";

    private readonly PostgreSqlContainer _postgresContainer;
    private readonly Guid _adminId = Guid.NewGuid();
    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _client = null!;
    private ApplicationDbContext _db = null!;

    public AdminIntegrationTests()
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
            builder.UseSetting("Admin:UserIds:0", _adminId.ToString());

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

        // The admin id has to be known before the host starts, so the admin is inserted directly.
        _db.Users.Add(
            new User
            {
                Id = _adminId,
                Username = "admin",
                Email = AdminEmail,
                PasswordHash = new PasswordHasher().Hash(Password),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            }
        );
        await _db.SaveChangesAsync();
    }

    public async Task DisposeAsync()
    {
        await _db.DisposeAsync();
        _client.Dispose();
        _factory.Dispose();
        await _postgresContainer.DisposeAsync();
    }

    private async Task<AuthResponseDto> RegisterUser(string username)
    {
        var response = await _client.PostAsJsonAsync(
            "/api/auth/register",
            new RegisterRequestDto
            {
                Username = username,
                Email = $"{username}@example.com",
                Password = Password,
            }
        );
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<AuthResponseDto>())!;
    }

    private async Task<AuthResponseDto> LoginAsAdmin()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequestDto { Email = AdminEmail, Password = Password }
        );
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<AuthResponseDto>())!;
    }

    private void Authenticate(AuthResponseDto auth) =>
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            auth.AccessToken
        );

    private async Task<GroupDto> CreateGroup(string name)
    {
        var response = await _client.PostAsJsonAsync(
            "/api/groups/",
            new CreateGroupRequest { GroupName = name }
        );
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<GroupDto>())!;
    }

    private async Task<InvitationResponseDto> Invite(Guid groupId, string inviteeUsername)
    {
        var response = await _client.PostAsJsonAsync(
            "/api/groups/invite",
            new InvitationRequestDto { GroupId = groupId, InviteeUsername = inviteeUsername }
        );
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<InvitationResponseDto>())!;
    }

    [Fact]
    public async Task Access_ReturnsFalse_ForRegularUser_AndTrue_ForAdmin()
    {
        Authenticate(await RegisterUser("regular"));
        var regular = await _client.GetFromJsonAsync<AdminAccessResponseDto>("/api/admin/access");

        Authenticate(await LoginAsAdmin());
        var admin = await _client.GetFromJsonAsync<AdminAccessResponseDto>("/api/admin/access");

        regular!.IsAdmin.Should().BeFalse();
        admin!.IsAdmin.Should().BeTrue();
    }

    [Theory]
    [InlineData("/api/admin/overview")]
    [InlineData("/api/admin/activity")]
    [InlineData("/api/admin/groups")]
    [InlineData("/api/admin/invitations")]
    public async Task AdminEndpoints_Return403_ForRegularUser(string url)
    {
        Authenticate(await RegisterUser("regular"));

        var response = await _client.GetAsync(url);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Theory]
    [InlineData("/api/admin/access")]
    [InlineData("/api/admin/overview")]
    public async Task AdminEndpoints_Return401_WhenUnauthenticated(string url)
    {
        var response = await _client.GetAsync(url);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Overview_ReturnsPlatformStats()
    {
        var alice = await RegisterUser("alice");
        await RegisterUser("bobby");
        Authenticate(alice);
        var group = await CreateGroup("Movie night");
        await Invite(group.Id, "bobby");

        Authenticate(await LoginAsAdmin());
        var response = await _client.GetAsync("/api/admin/overview?refresh=true");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var overview = await response.Content.ReadFromJsonAsync<AdminOverviewResponseDto>();
        overview!.Users.Total.Should().Be(3);
        overview.Users.NewLast7Days.Should().Be(3);
        overview.Users.WithoutGroup.Should().Be(2);
        overview.Users.ActiveLast24Hours.Should().Be(3);
        overview.Groups.Total.Should().Be(1);
        overview.Groups.AverageMembers.Should().Be(1);
        overview.Invitations.Pending.Should().Be(1);
        overview.Ratings.Distribution.Should().HaveCount(10);
    }

    [Fact]
    public async Task Activity_ReturnsDailySeries_ComputedInPostgres()
    {
        await RegisterUser("alice");
        Authenticate(await LoginAsAdmin());

        var response = await _client.GetAsync("/api/admin/activity?days=7&refresh=true");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var activity = await response.Content.ReadFromJsonAsync<AdminActivityResponseDto>();
        activity!.Series.Should().HaveCount(7);
        var today = activity.Series.Last();
        today.Date.Should().Be(DateOnly.FromDateTime(DateTime.UtcNow));
        today.Signups.Should().Be(2);
        today.ActiveUsers.Should().Be(2);
    }

    [Theory]
    [InlineData("/api/admin/activity?days=3")]
    [InlineData("/api/admin/activity?days=1000")]
    [InlineData("/api/admin/invitations?limit=0")]
    [InlineData("/api/admin/invitations?status=Bogus")]
    public async Task AdminEndpoints_Return400_ForInvalidQuery(string url)
    {
        Authenticate(await LoginAsAdmin());

        var response = await _client.GetAsync(url);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Groups_ReturnsMembersAndPendingInvitations()
    {
        var alice = await RegisterUser("alice");
        await RegisterUser("bobby");
        Authenticate(alice);
        var group = await CreateGroup("Movie night");
        await Invite(group.Id, "bobby");

        Authenticate(await LoginAsAdmin());
        var groups = await _client.GetFromJsonAsync<List<AdminGroupDto>>("/api/admin/groups");

        var dto = groups!.Single(g => g.Id == group.Id);
        dto.Name.Should().Be("Movie night");
        dto.Members.Should().BeEquivalentTo(["alice"]);
        dto.WatchSessionCount.Should().Be(0);
        dto.LastWatchSessionAt.Should().BeNull();
        dto.PendingInvitations.Should().Be(1);
    }

    [Fact]
    public async Task Invitations_ShowPendingInvitation_WithResolvedNames()
    {
        var alice = await RegisterUser("alice");
        await RegisterUser("bobby");
        Authenticate(alice);
        var group = await CreateGroup("Movie night");
        var invitation = await Invite(group.Id, "bobby");

        Authenticate(await LoginAsAdmin());
        var result = await _client.GetFromJsonAsync<AdminInvitationListResponseDto>(
            "/api/admin/invitations?status=Pending&search=BOB"
        );

        result!.TotalCount.Should().Be(1);
        var dto = result.Items.Single();
        dto.Id.Should().Be(invitation.InvitationId);
        dto.GroupName.Should().Be("Movie night");
        dto.InviterUsername.Should().Be("alice");
        dto.InviteeUsername.Should().Be("bobby");
        dto.EffectiveStatus.Should().Be(InvitationStatus.Pending);
        dto.Issues.Should().BeEmpty();
    }

    [Fact]
    public async Task Invitations_ReportExpiredButPending_WhenPastExpiry()
    {
        var alice = await RegisterUser("alice");
        await RegisterUser("bobby");
        Authenticate(alice);
        var group = await CreateGroup("Movie night");
        var invitation = await Invite(group.Id, "bobby");

        await _db
            .Invitations.Where(i => i.Id == invitation.InvitationId)
            .ExecuteUpdateAsync(s => s.SetProperty(i => i.ExpiresAt, DateTime.UtcNow.AddDays(-1)));

        Authenticate(await LoginAsAdmin());
        var pending = await _client.GetFromJsonAsync<AdminInvitationListResponseDto>(
            "/api/admin/invitations?status=Pending"
        );
        var expired = await _client.GetFromJsonAsync<AdminInvitationListResponseDto>(
            "/api/admin/invitations?status=Expired"
        );

        pending!.TotalCount.Should().Be(0);
        var dto = expired!.Items.Single();
        dto.Status.Should().Be(InvitationStatus.Pending);
        dto.EffectiveStatus.Should().Be(InvitationStatus.Expired);
        dto.Issues.Should().Contain(AdminInvitationIssues.ExpiredButPending);
    }

    [Fact]
    public async Task Invitations_FlagInviteeAlreadyMember_ForStalePendingInvitation()
    {
        var alice = await RegisterUser("alice");
        var bobby = await RegisterUser("bobby");
        var carol = await RegisterUser("carol");
        Authenticate(alice);
        var group = await CreateGroup("Movie night");
        var fromAlice = await Invite(group.Id, "carol");
        var accept = await _client.PostAsJsonAsync(
            "/api/groups/invite",
            new InvitationRequestDto { GroupId = group.Id, InviteeUsername = "bobby" }
        );
        accept.EnsureSuccessStatusCode();
        var toBobby = (await accept.Content.ReadFromJsonAsync<InvitationResponseDto>())!;

        Authenticate(bobby);
        var accepted = await _client.PostAsJsonAsync(
            "/api/groups/invite/accept",
            new AcceptInvitationRequestDto { InviteToken = toBobby.InviteToken }
        );
        accepted.EnsureSuccessStatusCode();

        // Bobby (now a member) invites carol too, then carol accepts Bobby's invite:
        // Alice's invitation to carol stays Pending although carol is already in the group.
        var fromBobby = await Invite(group.Id, "carol");
        Authenticate(carol);
        var carolAccepts = await _client.PostAsJsonAsync(
            "/api/groups/invite/accept",
            new AcceptInvitationRequestDto { InviteToken = fromBobby.InviteToken }
        );
        carolAccepts.EnsureSuccessStatusCode();

        Authenticate(await LoginAsAdmin());
        var result = await _client.GetFromJsonAsync<AdminInvitationListResponseDto>(
            "/api/admin/invitations?status=Pending"
        );

        var stale = result!.Items.Single(i => i.Id == fromAlice.InvitationId);
        stale.Issues.Should().Contain(AdminInvitationIssues.InviteeAlreadyMember);
    }
}
