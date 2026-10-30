using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using MovieRaterApi.Data;
using MovieRaterApi.Data.Entities;
using MovieRaterApi.Features.Admin.DTOs;
using MovieRaterApi.Features.Admin.Interfaces;

namespace MovieRaterApi.Features.Admin.Services;

public class AdminStatsService : IAdminStatsService
{
    private const string OverviewCacheKey = "admin:overview";
    private const string ActivityCacheKeyPrefix = "admin:activity:";
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);

    private readonly ApplicationDbContext _db;
    private readonly IMemoryCache _cache;
    private readonly ILogger<AdminStatsService> _logger;

    public AdminStatsService(
        ApplicationDbContext db,
        IMemoryCache cache,
        ILogger<AdminStatsService> logger
    )
    {
        _db = db;
        _cache = cache;
        _logger = logger;
    }

    public async Task<AdminOverviewResponseDto> GetOverviewAsync(
        Guid adminUserId,
        AdminOverviewQueryDto query
    )
    {
        _logger.LogInformation(
            "Admin {AdminUserId} requested overview (refresh={Refresh})",
            adminUserId,
            query.Refresh
        );

        if (
            !query.Refresh
            && _cache.TryGetValue(OverviewCacheKey, out AdminOverviewResponseDto? cached)
        )
            return cached!;

        var overview = await ComputeOverviewAsync();
        _cache.Set(OverviewCacheKey, overview, CacheTtl);
        return overview;
    }

    public async Task<AdminActivityResponseDto> GetActivityAsync(
        Guid adminUserId,
        AdminActivityQueryDto query
    )
    {
        _logger.LogInformation(
            "Admin {AdminUserId} requested activity for {Days} days (refresh={Refresh})",
            adminUserId,
            query.Days,
            query.Refresh
        );

        var cacheKey = $"{ActivityCacheKeyPrefix}{query.Days}";
        if (!query.Refresh && _cache.TryGetValue(cacheKey, out AdminActivityResponseDto? cached))
            return cached!;

        var activity = await ComputeActivityAsync(query.Days);
        _cache.Set(cacheKey, activity, CacheTtl);
        return activity;
    }

    public async Task<List<AdminGroupDto>> GetGroupsAsync(Guid adminUserId)
    {
        var now = DateTime.UtcNow;

        var groups = await _db
            .Groups.AsNoTracking()
            .Select(g => new AdminGroupDto
            {
                Id = g.Id,
                Name = g.Name,
                CreatedAt = g.CreatedAt,
                Members = g.UserGroups.Select(ug => ug.User.Username).OrderBy(u => u).ToList(),
                WatchSessionCount = g.WatchSessions.Count(),
                RatingCount = g.WatchSessions.SelectMany(ws => ws.Ratings).Count(),
                LastWatchSessionAt = g.WatchSessions.Max(ws => (DateTime?)ws.CreatedAt),
                PendingInvitations = _db.Invitations.Count(i =>
                    i.GroupId == g.Id && i.Status == InvitationStatus.Pending && i.ExpiresAt > now
                ),
            })
            .ToListAsync();

        _logger.LogInformation(
            "Admin {AdminUserId} listed {GroupCount} groups",
            adminUserId,
            groups.Count
        );

        // Ordered in memory: Postgres puts NULLs first on DESC, and the group count is small.
        return groups.OrderByDescending(g => g.LastWatchSessionAt ?? g.CreatedAt).ToList();
    }

    public async Task<AdminInvitationListResponseDto> GetInvitationsAsync(
        Guid adminUserId,
        AdminInvitationQueryDto query
    )
    {
        var now = DateTime.UtcNow;
        var invitations = _db.Invitations.AsNoTracking();

        invitations = query.Status switch
        {
            InvitationStatus.Pending => invitations.Where(i =>
                i.Status == InvitationStatus.Pending && i.ExpiresAt > now
            ),
            InvitationStatus.Expired => invitations.Where(i =>
                i.Status == InvitationStatus.Expired
                || (i.Status == InvitationStatus.Pending && i.ExpiresAt <= now)
            ),
            { } status => invitations.Where(i => i.Status == status),
            null => invitations,
        };

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim().ToLower();
            invitations = invitations.Where(i =>
                i.InviteeEmail.ToLower().Contains(search)
                || i.InviterUser.Username.ToLower().Contains(search)
                || _db.Users.Any(u =>
                    u.Email == i.InviteeEmail && u.Username.ToLower().Contains(search)
                )
                || _db.Groups.Any(g => g.Id == i.GroupId && g.Name.ToLower().Contains(search))
            );
        }

        var totalCount = await invitations.CountAsync();

        var rows = await invitations
            .OrderByDescending(i => i.CreatedAt)
            .Take(query.Limit)
            .Select(i => new
            {
                i.Id,
                i.GroupId,
                GroupName = _db
                    .Groups.Where(g => g.Id == i.GroupId)
                    .Select(g => g.Name)
                    .FirstOrDefault(),
                i.InviterUserId,
                InviterUsername = i.InviterUser.Username,
                i.InviteeEmail,
                InviteeUsername = _db
                    .Users.Where(u => u.Email == i.InviteeEmail)
                    .Select(u => u.Username)
                    .FirstOrDefault(),
                InviteeIsMember = _db.UserGroups.Any(ug =>
                    ug.GroupId == i.GroupId && ug.User.Email == i.InviteeEmail
                ),
                OtherPendingForSameInvitee = _db.Invitations.Any(o =>
                    o.Id != i.Id
                    && o.GroupId == i.GroupId
                    && o.InviteeEmail == i.InviteeEmail
                    && o.Status == InvitationStatus.Pending
                    && o.ExpiresAt > now
                ),
                i.Status,
                i.CreatedAt,
                i.ExpiresAt,
                AcceptedByUsername = i.AcceptedByUser != null ? i.AcceptedByUser.Username : null,
            })
            .ToListAsync();

        var items = rows.Select(r =>
            {
                var effectiveStatus = GetEffectiveStatus(r.Status, r.ExpiresAt, now);
                var isPending = effectiveStatus == InvitationStatus.Pending;

                return new AdminInvitationDto
                {
                    Id = r.Id,
                    GroupId = r.GroupId,
                    GroupName = r.GroupName,
                    InviterUserId = r.InviterUserId,
                    InviterUsername = r.InviterUsername,
                    InviteeEmail = r.InviteeEmail,
                    InviteeUsername = r.InviteeUsername,
                    Status = r.Status,
                    EffectiveStatus = effectiveStatus,
                    CreatedAt = r.CreatedAt,
                    ExpiresAt = r.ExpiresAt,
                    AcceptedByUsername = r.AcceptedByUsername,
                    Issues = GetInvitationIssues(
                        r.Status,
                        effectiveStatus,
                        groupExists: r.GroupName is not null,
                        inviteeIsMember: isPending && r.InviteeIsMember,
                        hasDuplicatePending: isPending && r.OtherPendingForSameInvitee
                    ),
                };
            })
            .ToList();

        _logger.LogInformation(
            "Admin {AdminUserId} listed invitations (status={Status}, search={Search}): {Returned}/{Total}",
            adminUserId,
            query.Status,
            query.Search,
            items.Count,
            totalCount
        );

        return new AdminInvitationListResponseDto { TotalCount = totalCount, Items = items };
    }

    public static InvitationStatus GetEffectiveStatus(
        InvitationStatus status,
        DateTime expiresAt,
        DateTime now
    ) => status == InvitationStatus.Pending && expiresAt <= now ? InvitationStatus.Expired : status;

    public static List<string> GetInvitationIssues(
        InvitationStatus status,
        InvitationStatus effectiveStatus,
        bool groupExists,
        bool inviteeIsMember,
        bool hasDuplicatePending
    )
    {
        var issues = new List<string>();

        if (status == InvitationStatus.Pending && effectiveStatus == InvitationStatus.Expired)
            issues.Add(AdminInvitationIssues.ExpiredButPending);

        if (!groupExists)
            issues.Add(AdminInvitationIssues.GroupMissing);

        // Only actionable while the invitation can still be accepted.
        if (effectiveStatus == InvitationStatus.Pending)
        {
            if (inviteeIsMember)
                issues.Add(AdminInvitationIssues.InviteeAlreadyMember);
            if (hasDuplicatePending)
                issues.Add(AdminInvitationIssues.DuplicatePending);
        }

        return issues;
    }

    private async Task<AdminOverviewResponseDto> ComputeOverviewAsync()
    {
        var now = DateTime.UtcNow;
        var last24Hours = now.AddHours(-24);
        var last7Days = now.AddDays(-7);
        var last30Days = now.AddDays(-30);

        // Queries run sequentially: a DbContext does not support concurrent operations,
        // and every one of them is a single aggregate returning a handful of rows.
        var users = new AdminUserStatsDto
        {
            Total = await _db.Users.CountAsync(),
            NewLast7Days = await _db.Users.CountAsync(u => u.CreatedAt >= last7Days),
            NewLast30Days = await _db.Users.CountAsync(u => u.CreatedAt >= last30Days),
            WithoutGroup = await _db.Users.CountAsync(u => !u.UserGroups.Any()),
            ActiveLast24Hours = await CountActiveUsersSinceAsync(last24Hours),
            ActiveLast7Days = await CountActiveUsersSinceAsync(last7Days),
            ActiveLast30Days = await CountActiveUsersSinceAsync(last30Days),
            ActiveSessions = await _db.RefreshTokens.CountAsync(t =>
                t.RevokedAt == null && t.ExpiresAt > now
            ),
        };

        var groups = new AdminGroupStatsDto
        {
            Total = await _db.Groups.CountAsync(),
            AverageMembers = Math.Round(
                await _db.Groups.Select(g => (double?)g.UserGroups.Count()).AverageAsync() ?? 0,
                2
            ),
            WithoutWatchSessions = await _db.Groups.CountAsync(g => !g.WatchSessions.Any()),
        };

        var mediaCounts = await _db
            .Media.GroupBy(m => m.MediaType)
            .Select(g => new { MediaType = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.MediaType, x => x.Count);

        var media = new AdminMediaStatsDto
        {
            Movies = mediaCounts.GetValueOrDefault(MediaType.Movie),
            TvSeries = mediaCounts.GetValueOrDefault(MediaType.TvSeries),
            TvSeasons = mediaCounts.GetValueOrDefault(MediaType.TvSeason),
            TvEpisodes = mediaCounts.GetValueOrDefault(MediaType.TvEpisode),
        };

        var totalSessions = await _db.WatchSessions.CountAsync();
        var fullyRatedSessions = await _db.WatchSessions.CountAsync(ws =>
            ws.Ratings.Count() >= (ws.GroupId == null ? 1 : ws.Group!.UserGroups.Count())
        );

        var watchSessions = new AdminWatchSessionStatsDto
        {
            Total = totalSessions,
            Last30Days = await _db.WatchSessions.CountAsync(ws => ws.CreatedAt >= last30Days),
            FullyRatedPercent = Percent(fullyRatedSessions, totalSessions),
        };

        var distribution = await _db
            .Ratings.GroupBy(r => r.RatingValue)
            .Select(g => new AdminRatingBucketDto { Value = g.Key, Count = g.Count() })
            .ToListAsync();

        var ratings = new AdminRatingStatsDto
        {
            Total = distribution.Sum(b => b.Count),
            Average = Math.Round(
                await _db.Ratings.AverageAsync(r => (double?)r.RatingValue) ?? 0,
                2
            ),
            Distribution = Enumerable
                .Range(1, 10)
                .Select(value => new AdminRatingBucketDto
                {
                    Value = value,
                    Count = distribution.FirstOrDefault(b => b.Value == value)?.Count ?? 0,
                })
                .ToList(),
        };

        var invitations = await ComputeInvitationStatsAsync(now);

        _logger.LogInformation(
            "Admin overview computed: {UserCount} users, {GroupCount} groups, {SessionCount} sessions, {RatingCount} ratings",
            users.Total,
            groups.Total,
            watchSessions.Total,
            ratings.Total
        );

        return new AdminOverviewResponseDto
        {
            Users = users,
            Groups = groups,
            Media = media,
            WatchSessions = watchSessions,
            Ratings = ratings,
            Invitations = invitations,
            GeneratedAt = now,
        };
    }

    private async Task<AdminInvitationStatsDto> ComputeInvitationStatsAsync(DateTime now)
    {
        var byStatus = await _db
            .Invitations.GroupBy(i => i.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Status, x => x.Count);

        var expiredButPending = await _db.Invitations.CountAsync(i =>
            i.Status == InvitationStatus.Pending && i.ExpiresAt <= now
        );

        var pending = byStatus.GetValueOrDefault(InvitationStatus.Pending) - expiredButPending;
        var accepted = byStatus.GetValueOrDefault(InvitationStatus.Accepted);
        var expired = byStatus.GetValueOrDefault(InvitationStatus.Expired) + expiredButPending;
        var revoked = byStatus.GetValueOrDefault(InvitationStatus.Revoked);

        return new AdminInvitationStatsDto
        {
            Pending = pending,
            Accepted = accepted,
            Expired = expired,
            Revoked = revoked,
            ExpiredButStillPending = expiredButPending,
            AcceptanceRatePercent = Percent(accepted, accepted + expired + revoked),
        };
    }

    private async Task<AdminActivityResponseDto> ComputeActivityAsync(int days)
    {
        var since = DateTime.UtcNow.Date.AddDays(-(days - 1));

        // Bucketing by day happens in Postgres (date_trunc); only <= `days` rows come back.
        var signups = await _db
            .Users.Where(u => u.CreatedAt >= since)
            .GroupBy(u => u.CreatedAt.Date)
            .Select(g => new { Day = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => DateOnly.FromDateTime(x.Day), x => x.Count);

        var activeUsers = await _db
            .RefreshTokens.Where(t => t.CreatedAt >= since)
            .GroupBy(t => t.CreatedAt.Date)
            .Select(g => new { Day = g.Key, Count = g.Select(t => t.UserId).Distinct().Count() })
            .ToDictionaryAsync(x => DateOnly.FromDateTime(x.Day), x => x.Count);

        var watchSessions = await _db
            .WatchSessions.Where(ws => ws.CreatedAt >= since)
            .GroupBy(ws => ws.CreatedAt.Date)
            .Select(g => new { Day = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => DateOnly.FromDateTime(x.Day), x => x.Count);

        var ratings = await _db
            .Ratings.Where(r => r.CreatedAt >= since)
            .GroupBy(r => r.CreatedAt.Date)
            .Select(g => new { Day = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => DateOnly.FromDateTime(x.Day), x => x.Count);

        var firstDay = DateOnly.FromDateTime(since);
        var series = Enumerable
            .Range(0, days)
            .Select(offset =>
            {
                var day = firstDay.AddDays(offset);
                return new AdminActivityDayDto
                {
                    Date = day,
                    Signups = signups.GetValueOrDefault(day),
                    ActiveUsers = activeUsers.GetValueOrDefault(day),
                    WatchSessions = watchSessions.GetValueOrDefault(day),
                    Ratings = ratings.GetValueOrDefault(day),
                };
            })
            .ToList();

        return new AdminActivityResponseDto { Days = days, Series = series };
    }

    private Task<int> CountActiveUsersSinceAsync(DateTime since) =>
        _db
            .RefreshTokens.Where(t => t.CreatedAt >= since)
            .Select(t => t.UserId)
            .Distinct()
            .CountAsync();

    private static double Percent(int part, int total) =>
        total == 0 ? 0 : Math.Round(part * 100.0 / total, 1);
}
