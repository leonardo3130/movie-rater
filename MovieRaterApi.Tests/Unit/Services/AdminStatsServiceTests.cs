using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using MovieRaterApi.Data;
using MovieRaterApi.Data.Entities;
using MovieRaterApi.Features.Admin.DTOs;
using MovieRaterApi.Features.Admin.Services;

namespace MovieRaterApi.Tests.Unit.Services;

public class AdminStatsServiceTests
{
    private readonly ApplicationDbContext _db;
    private readonly IMemoryCache _cache;
    private readonly AdminStatsService _sut;
    private readonly Guid _adminId = Guid.NewGuid();

    public AdminStatsServiceTests()
    {
        _db = TestHelpers.CreateInMemoryDbContext();
        _cache = new MemoryCache(new MemoryCacheOptions());
        _sut = new AdminStatsService(_db, _cache, new Mock<ILogger<AdminStatsService>>().Object);
    }

    // ---- Overview ----

    [Fact]
    public async Task GetOverviewAsync_ReturnsZeros_WhenDatabaseIsEmpty()
    {
        var result = await _sut.GetOverviewAsync(_adminId, new AdminOverviewQueryDto());

        result.Users.Total.Should().Be(0);
        result.Groups.AverageMembers.Should().Be(0);
        result.Ratings.Average.Should().Be(0);
        result.Ratings.Distribution.Should().HaveCount(10).And.OnlyContain(b => b.Count == 0);
        result.WatchSessions.FullyRatedPercent.Should().Be(0);
        result.Invitations.AcceptanceRatePercent.Should().Be(0);
    }

    [Fact]
    public async Task GetOverviewAsync_CountsUsers_NewSignups_AndUsersWithoutGroup()
    {
        var inGroup = SeedUser("ingroup", createdAt: DateTime.UtcNow.AddDays(-2));
        SeedUser("recent", createdAt: DateTime.UtcNow.AddDays(-10));
        SeedUser("old", createdAt: DateTime.UtcNow.AddDays(-60));
        var group = SeedGroup("Group one");
        SeedMembership(group, inGroup);

        var result = await _sut.GetOverviewAsync(_adminId, new AdminOverviewQueryDto());

        result.Users.Total.Should().Be(3);
        result.Users.NewLast7Days.Should().Be(1);
        result.Users.NewLast30Days.Should().Be(2);
        result.Users.WithoutGroup.Should().Be(2);
    }

    [Fact]
    public async Task GetOverviewAsync_CountsDistinctActiveUsers_FromRefreshTokens()
    {
        var u1 = SeedUser("u1");
        var u2 = SeedUser("u2");
        var u3 = SeedUser("u3");
        SeedRefreshToken(u1, DateTime.UtcNow.AddHours(-1));
        SeedRefreshToken(u1, DateTime.UtcNow.AddHours(-2));
        SeedRefreshToken(u2, DateTime.UtcNow.AddDays(-3));
        SeedRefreshToken(u3, DateTime.UtcNow.AddDays(-20), revoked: true);

        var result = await _sut.GetOverviewAsync(_adminId, new AdminOverviewQueryDto());

        result.Users.ActiveLast24Hours.Should().Be(1);
        result.Users.ActiveLast7Days.Should().Be(2);
        result.Users.ActiveLast30Days.Should().Be(3);
        result.Users.ActiveSessions.Should().Be(3);
    }

    [Fact]
    public async Task GetOverviewAsync_ComputesGroupStats()
    {
        var u1 = SeedUser("u1");
        var u2 = SeedUser("u2");
        var u3 = SeedUser("u3");
        var g1 = SeedGroup("Group one");
        var g2 = SeedGroup("Group two");
        SeedMembership(g1, u1);
        SeedMembership(g1, u2);
        SeedMembership(g2, u3);
        SeedSession(g1, SeedMovie(1), u1);

        var result = await _sut.GetOverviewAsync(_adminId, new AdminOverviewQueryDto());

        result.Groups.Total.Should().Be(2);
        result.Groups.AverageMembers.Should().Be(1.5);
        result.Groups.WithoutWatchSessions.Should().Be(1);
    }

    [Fact]
    public async Task GetOverviewAsync_CountsMediaByType()
    {
        SeedMovie(1);
        SeedMovie(2);
        _db.TvSeries.Add(new TvSeries { Id = Guid.NewGuid(), TmdbId = 10, Title = "Show" });
        _db.TvEpisodes.Add(new TvEpisode { Id = Guid.NewGuid(), TmdbId = 11, Title = "Ep" });
        await _db.SaveChangesAsync();

        var result = await _sut.GetOverviewAsync(_adminId, new AdminOverviewQueryDto());

        result.Media.Movies.Should().Be(2);
        result.Media.TvSeries.Should().Be(1);
        result.Media.TvSeasons.Should().Be(0);
        result.Media.TvEpisodes.Should().Be(1);
    }

    [Fact]
    public async Task GetOverviewAsync_ComputesFullyRatedPercent_PerGroupSize()
    {
        var u1 = SeedUser("u1");
        var u2 = SeedUser("u2");
        var group = SeedGroup("Pair");
        SeedMembership(group, u1);
        SeedMembership(group, u2);
        var movie = SeedMovie(1);

        var bothRated = SeedSession(group, movie, u1);
        SeedRating(bothRated, u1, 8);
        SeedRating(bothRated, u2, 6);

        var oneRated = SeedSession(group, movie, u1);
        SeedRating(oneRated, u1, 7);

        var solo = SeedSession(null, movie, u1);
        SeedRating(solo, u1, 9);

        SeedSession(null, movie, u1);

        var result = await _sut.GetOverviewAsync(_adminId, new AdminOverviewQueryDto());

        result.WatchSessions.Total.Should().Be(4);
        result.WatchSessions.FullyRatedPercent.Should().Be(50);
    }

    [Fact]
    public async Task GetOverviewAsync_ComputesRatingAverage_AndDistribution()
    {
        var user = SeedUser("u1");
        var movie = SeedMovie(1);
        SeedRating(SeedSession(null, movie, user), user, 8);
        SeedRating(SeedSession(null, movie, user), user, 8);
        SeedRating(SeedSession(null, movie, user), user, 5);

        var result = await _sut.GetOverviewAsync(_adminId, new AdminOverviewQueryDto());

        result.Ratings.Total.Should().Be(3);
        result.Ratings.Average.Should().Be(7);
        result.Ratings.Distribution.Single(b => b.Value == 8).Count.Should().Be(2);
        result.Ratings.Distribution.Single(b => b.Value == 5).Count.Should().Be(1);
        result.Ratings.Distribution.Single(b => b.Value == 1).Count.Should().Be(0);
    }

    [Fact]
    public async Task GetOverviewAsync_CountsPendingPastExpiry_AsExpired()
    {
        var inviter = SeedUser("inviter");
        var group = SeedGroup("Group");
        SeedInvitation(group, inviter, "a@example.com", InvitationStatus.Pending, DateTime.UtcNow.AddDays(3));
        SeedInvitation(group, inviter, "b@example.com", InvitationStatus.Pending, DateTime.UtcNow.AddDays(-1));
        SeedInvitation(group, inviter, "c@example.com", InvitationStatus.Accepted, DateTime.UtcNow.AddDays(3));
        SeedInvitation(group, inviter, "d@example.com", InvitationStatus.Expired, DateTime.UtcNow.AddDays(-5));

        var result = await _sut.GetOverviewAsync(_adminId, new AdminOverviewQueryDto());

        result.Invitations.Pending.Should().Be(1);
        result.Invitations.Expired.Should().Be(2);
        result.Invitations.ExpiredButStillPending.Should().Be(1);
        result.Invitations.Accepted.Should().Be(1);
        result.Invitations.AcceptanceRatePercent.Should().Be(33.3);
    }

    [Fact]
    public async Task GetOverviewAsync_ReturnsCachedValue_UnlessRefreshRequested()
    {
        SeedUser("u1");
        var first = await _sut.GetOverviewAsync(_adminId, new AdminOverviewQueryDto());

        SeedUser("u2");
        var cached = await _sut.GetOverviewAsync(_adminId, new AdminOverviewQueryDto());
        var refreshed = await _sut.GetOverviewAsync(
            _adminId,
            new AdminOverviewQueryDto { Refresh = true }
        );

        first.Users.Total.Should().Be(1);
        cached.Users.Total.Should().Be(1);
        refreshed.Users.Total.Should().Be(2);
    }

    // ---- Activity ----

    [Fact]
    public async Task GetActivityAsync_ReturnsOneEntryPerDay_EndingToday()
    {
        var result = await _sut.GetActivityAsync(_adminId, new AdminActivityQueryDto { Days = 14 });

        result.Series.Should().HaveCount(14);
        result.Series.Last().Date.Should().Be(DateOnly.FromDateTime(DateTime.UtcNow));
        result.Series.Should().OnlyContain(d => d.Signups == 0 && d.ActiveUsers == 0);
    }

    [Fact]
    public async Task GetActivityAsync_BucketsEventsByDay()
    {
        var today = DateTime.UtcNow.Date.AddHours(1);
        var yesterday = today.AddDays(-1);
        var u1 = SeedUser("u1", createdAt: today);
        var u2 = SeedUser("u2", createdAt: yesterday);
        SeedUser("tooOld", createdAt: today.AddDays(-40));
        SeedRefreshToken(u1, today);
        SeedRefreshToken(u1, today.AddMinutes(20));
        SeedRefreshToken(u2, today);
        var movie = SeedMovie(1);
        var session = SeedSession(null, movie, u1, createdAt: yesterday);
        SeedRating(session, u1, 7, createdAt: yesterday);

        var result = await _sut.GetActivityAsync(_adminId, new AdminActivityQueryDto { Days = 30 });

        var todayEntry = result.Series.Single(d => d.Date == DateOnly.FromDateTime(today));
        var yesterdayEntry = result.Series.Single(d => d.Date == DateOnly.FromDateTime(yesterday));

        todayEntry.Signups.Should().Be(1);
        todayEntry.ActiveUsers.Should().Be(2);
        yesterdayEntry.Signups.Should().Be(1);
        yesterdayEntry.WatchSessions.Should().Be(1);
        yesterdayEntry.Ratings.Should().Be(1);
        result.Series.Sum(d => d.Signups).Should().Be(2);
    }

    // ---- Groups ----

    [Fact]
    public async Task GetGroupsAsync_ReturnsMembersCounts_AndPendingInvitations()
    {
        var u1 = SeedUser("alice");
        var u2 = SeedUser("bob");
        var active = SeedGroup("Active");
        var dormant = SeedGroup("Dormant");
        SeedMembership(active, u1);
        SeedMembership(active, u2);
        SeedMembership(dormant, u1);
        var session = SeedSession(active, SeedMovie(1), u1);
        SeedRating(session, u1, 7);
        SeedRating(session, u2, 9);
        SeedInvitation(dormant, u1, "x@example.com", InvitationStatus.Pending, DateTime.UtcNow.AddDays(2));
        SeedInvitation(dormant, u1, "y@example.com", InvitationStatus.Pending, DateTime.UtcNow.AddDays(-2));

        var result = await _sut.GetGroupsAsync(_adminId);

        var activeDto = result.Single(g => g.Id == active);
        activeDto.Members.Should().BeEquivalentTo(["alice", "bob"]);
        activeDto.WatchSessionCount.Should().Be(1);
        activeDto.RatingCount.Should().Be(2);
        activeDto.LastWatchSessionAt.Should().NotBeNull();

        var dormantDto = result.Single(g => g.Id == dormant);
        dormantDto.WatchSessionCount.Should().Be(0);
        dormantDto.LastWatchSessionAt.Should().BeNull();
        dormantDto.PendingInvitations.Should().Be(1);
    }

    [Fact]
    public async Task GetGroupsAsync_OrdersByMostRecentActivity()
    {
        var user = SeedUser("u1");
        var stale = SeedGroup("Stale", createdAt: DateTime.UtcNow.AddDays(-30));
        var fresh = SeedGroup("Fresh", createdAt: DateTime.UtcNow.AddDays(-60));
        SeedSession(fresh, SeedMovie(1), user, createdAt: DateTime.UtcNow.AddHours(-1));

        var result = await _sut.GetGroupsAsync(_adminId);

        result.Select(g => g.Id).Should().ContainInOrder(fresh, stale);
    }

    // ---- Invitations ----

    [Fact]
    public async Task GetInvitationsAsync_ReturnsNewestFirst_WithResolvedNames()
    {
        var inviter = SeedUser("inviter");
        var invitee = SeedUser("invitee", email: "invitee@example.com");
        var group = SeedGroup("Movie night");
        var older = SeedInvitation(group, inviter, "invitee@example.com", InvitationStatus.Accepted, DateTime.UtcNow.AddDays(3), createdAt: DateTime.UtcNow.AddDays(-2), acceptedBy: invitee);
        var newer = SeedInvitation(group, inviter, "someone@example.com", InvitationStatus.Revoked, DateTime.UtcNow.AddDays(3));

        var result = await _sut.GetInvitationsAsync(_adminId, new AdminInvitationQueryDto());

        result.TotalCount.Should().Be(2);
        result.Items.Select(i => i.Id).Should().ContainInOrder(newer, older);
        var accepted = result.Items.Single(i => i.Id == older);
        accepted.GroupName.Should().Be("Movie night");
        accepted.InviterUsername.Should().Be("inviter");
        accepted.InviteeUsername.Should().Be("invitee");
        accepted.AcceptedByUsername.Should().Be("invitee");
        accepted.Issues.Should().BeEmpty();
    }

    [Fact]
    public async Task GetInvitationsAsync_FiltersByEffectiveStatus()
    {
        var inviter = SeedUser("inviter");
        var group = SeedGroup("Group");
        var live = SeedInvitation(group, inviter, "a@example.com", InvitationStatus.Pending, DateTime.UtcNow.AddDays(3));
        var stale = SeedInvitation(group, inviter, "b@example.com", InvitationStatus.Pending, DateTime.UtcNow.AddDays(-1));
        var expired = SeedInvitation(group, inviter, "c@example.com", InvitationStatus.Expired, DateTime.UtcNow.AddDays(-1));

        var pending = await _sut.GetInvitationsAsync(
            _adminId,
            new AdminInvitationQueryDto { Status = InvitationStatus.Pending }
        );
        var expiredResult = await _sut.GetInvitationsAsync(
            _adminId,
            new AdminInvitationQueryDto { Status = InvitationStatus.Expired }
        );

        pending.Items.Select(i => i.Id).Should().BeEquivalentTo([live]);
        expiredResult.Items.Select(i => i.Id).Should().BeEquivalentTo([stale, expired]);
        var staleDto = expiredResult.Items.Single(i => i.Id == stale);
        staleDto.Status.Should().Be(InvitationStatus.Pending);
        staleDto.EffectiveStatus.Should().Be(InvitationStatus.Expired);
        staleDto.Issues.Should().Contain(AdminInvitationIssues.ExpiredButPending);
    }

    [Fact]
    public async Task GetInvitationsAsync_SearchesEmailUsernamesAndGroupName()
    {
        var inviter = SeedUser("marco");
        SeedUser("giulia", email: "giulia@example.com");
        var friends = SeedGroup("Friends");
        var family = SeedGroup("Family");
        var toGiulia = SeedInvitation(friends, inviter, "giulia@example.com", InvitationStatus.Pending, DateTime.UtcNow.AddDays(3));
        var toFamily = SeedInvitation(family, inviter, "other@example.com", InvitationStatus.Pending, DateTime.UtcNow.AddDays(3));

        var byInviteeUsername = await _sut.GetInvitationsAsync(_adminId, new AdminInvitationQueryDto { Search = "GIUL" });
        var byGroup = await _sut.GetInvitationsAsync(_adminId, new AdminInvitationQueryDto { Search = "family" });
        var byInviter = await _sut.GetInvitationsAsync(_adminId, new AdminInvitationQueryDto { Search = "marco" });

        byInviteeUsername.Items.Select(i => i.Id).Should().BeEquivalentTo([toGiulia]);
        byGroup.Items.Select(i => i.Id).Should().BeEquivalentTo([toFamily]);
        byInviter.TotalCount.Should().Be(2);
    }

    [Fact]
    public async Task GetInvitationsAsync_AppliesLimit_ButReportsTotalCount()
    {
        var inviter = SeedUser("inviter");
        var group = SeedGroup("Group");
        for (var i = 0; i < 5; i++)
            SeedInvitation(group, inviter, $"u{i}@example.com", InvitationStatus.Revoked, DateTime.UtcNow);

        var result = await _sut.GetInvitationsAsync(_adminId, new AdminInvitationQueryDto { Limit = 2 });

        result.TotalCount.Should().Be(5);
        result.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetInvitationsAsync_FlagsProblemsOnPendingInvitations()
    {
        var inviter = SeedUser("inviter");
        var otherInviter = SeedUser("other");
        var member = SeedUser("member", email: "member@example.com");
        var group = SeedGroup("Group");
        SeedMembership(group, member);

        var alreadyMember = SeedInvitation(group, inviter, "member@example.com", InvitationStatus.Pending, DateTime.UtcNow.AddDays(3));
        var unregistered = SeedInvitation(group, inviter, "changed@example.com", InvitationStatus.Pending, DateTime.UtcNow.AddDays(3));
        var duplicateA = SeedInvitation(group, inviter, "dup@example.com", InvitationStatus.Pending, DateTime.UtcNow.AddDays(3));
        SeedInvitation(group, otherInviter, "dup@example.com", InvitationStatus.Pending, DateTime.UtcNow.AddDays(3));
        var orphan = SeedInvitation(Guid.NewGuid(), inviter, "member@example.com", InvitationStatus.Revoked, DateTime.UtcNow);

        var result = await _sut.GetInvitationsAsync(_adminId, new AdminInvitationQueryDto());

        result.Items.Single(i => i.Id == alreadyMember).Issues
            .Should().Contain(AdminInvitationIssues.InviteeAlreadyMember);
        result.Items.Single(i => i.Id == unregistered).Issues
            .Should().Contain(AdminInvitationIssues.InviteeNotRegistered);
        result.Items.Single(i => i.Id == duplicateA).Issues
            .Should().Contain(AdminInvitationIssues.DuplicatePending);
        var orphanDto = result.Items.Single(i => i.Id == orphan);
        orphanDto.GroupName.Should().BeNull();
        orphanDto.Issues.Should().BeEquivalentTo([AdminInvitationIssues.GroupMissing]);
    }

    // ---- Pure helpers ----

    [Theory]
    [InlineData(InvitationStatus.Pending, 1, InvitationStatus.Pending)]
    [InlineData(InvitationStatus.Pending, -1, InvitationStatus.Expired)]
    [InlineData(InvitationStatus.Accepted, -1, InvitationStatus.Accepted)]
    [InlineData(InvitationStatus.Revoked, -1, InvitationStatus.Revoked)]
    public void GetEffectiveStatus_TreatsPendingPastExpiryAsExpired(
        InvitationStatus status,
        int expiresInDays,
        InvitationStatus expected
    )
    {
        var now = DateTime.UtcNow;

        AdminStatsService
            .GetEffectiveStatus(status, now.AddDays(expiresInDays), now)
            .Should()
            .Be(expected);
    }

    [Fact]
    public void GetInvitationIssues_IgnoresInviteeProblems_WhenNotPending()
    {
        var issues = AdminStatsService.GetInvitationIssues(
            InvitationStatus.Accepted,
            InvitationStatus.Accepted,
            groupExists: true,
            inviteeRegistered: false,
            inviteeIsMember: true,
            hasDuplicatePending: true
        );

        issues.Should().BeEmpty();
    }

    // ---- Seed helpers ----

    private Guid SeedUser(string username, string? email = null, DateTime? createdAt = null)
    {
        var id = Guid.NewGuid();
        _db.Users.Add(
            new User
            {
                Id = id,
                Username = username,
                Email = email ?? $"{username}@example.com",
                CreatedAt = createdAt ?? DateTime.UtcNow.AddDays(-100),
            }
        );
        _db.SaveChanges();
        return id;
    }

    private Guid SeedGroup(string name, DateTime? createdAt = null)
    {
        var id = Guid.NewGuid();
        _db.Groups.Add(
            new Group
            {
                Id = id,
                Name = name,
                CreatedAt = createdAt ?? DateTime.UtcNow.AddDays(-100),
            }
        );
        _db.SaveChanges();
        return id;
    }

    private void SeedMembership(Guid groupId, Guid userId)
    {
        _db.UserGroups.Add(
            new UserGroup
            {
                Id = Guid.NewGuid(),
                GroupId = groupId,
                UserId = userId,
            }
        );
        _db.SaveChanges();
    }

    private Guid SeedMovie(int tmdbId)
    {
        var id = Guid.NewGuid();
        _db.Movies.Add(
            new Movie
            {
                Id = id,
                TmdbId = tmdbId,
                Title = $"Movie {tmdbId}",
            }
        );
        _db.SaveChanges();
        return id;
    }

    private Guid SeedSession(Guid? groupId, Guid mediaId, Guid userId, DateTime? createdAt = null)
    {
        var id = Guid.NewGuid();
        var at = createdAt ?? DateTime.UtcNow.AddDays(-50);
        _db.WatchSessions.Add(
            new WatchSession
            {
                Id = id,
                GroupId = groupId,
                MediaId = mediaId,
                CreatedByUserId = userId,
                WatchedAt = at,
                CreatedAt = at,
                UpdatedAt = at,
            }
        );
        _db.SaveChanges();
        return id;
    }

    private void SeedRating(Guid sessionId, Guid userId, int value, DateTime? createdAt = null)
    {
        var at = createdAt ?? DateTime.UtcNow.AddDays(-50);
        _db.Ratings.Add(
            new Rating
            {
                Id = Guid.NewGuid(),
                WatchSessionId = sessionId,
                UserId = userId,
                RatingValue = value,
                CreatedAt = at,
                UpdatedAt = at,
            }
        );
        _db.SaveChanges();
    }

    private void SeedRefreshToken(Guid userId, DateTime createdAt, bool revoked = false)
    {
        _db.RefreshTokens.Add(
            new RefreshToken
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                TokenHash = Guid.NewGuid().ToString(),
                CreatedAt = createdAt,
                ExpiresAt = createdAt.AddDays(30),
                RevokedAt = revoked ? createdAt.AddMinutes(1) : null,
            }
        );
        _db.SaveChanges();
    }

    private Guid SeedInvitation(
        Guid groupId,
        Guid inviterId,
        string inviteeEmail,
        InvitationStatus status,
        DateTime expiresAt,
        DateTime? createdAt = null,
        Guid? acceptedBy = null
    )
    {
        var id = Guid.NewGuid();
        _db.Invitations.Add(
            new Invitation
            {
                Id = id,
                GroupId = groupId,
                InviterUserId = inviterId,
                InviteeEmail = inviteeEmail,
                InviteTokenHash = Guid.NewGuid().ToString(),
                Status = status,
                ExpiresAt = expiresAt,
                CreatedAt = createdAt ?? DateTime.UtcNow,
                AcceptedByUserId = acceptedBy,
            }
        );
        _db.SaveChanges();
        return id;
    }
}
