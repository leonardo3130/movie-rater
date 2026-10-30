using MovieRaterApi.Data.Entities;

namespace MovieRaterApi.Features.Admin.DTOs;

public class AdminAccessResponseDto
{
    public bool IsAdmin { get; set; }
}

public class AdminOverviewQueryDto
{
    public bool Refresh { get; set; }
}

public class AdminActivityQueryDto
{
    public int Days { get; set; } = 30;
    public bool Refresh { get; set; }
}

public class AdminInvitationQueryDto
{
    public InvitationStatus? Status { get; set; }
    public string? Search { get; set; }
    public int Limit { get; set; } = 100;
}

public class AdminOverviewResponseDto
{
    public AdminUserStatsDto Users { get; set; } = new();
    public AdminGroupStatsDto Groups { get; set; } = new();
    public AdminMediaStatsDto Media { get; set; } = new();
    public AdminWatchSessionStatsDto WatchSessions { get; set; } = new();
    public AdminRatingStatsDto Ratings { get; set; } = new();
    public AdminInvitationStatsDto Invitations { get; set; } = new();
    public DateTime GeneratedAt { get; set; }
}

public class AdminUserStatsDto
{
    public int Total { get; set; }
    public int NewLast7Days { get; set; }
    public int NewLast30Days { get; set; }
    public int WithoutGroup { get; set; }

    // Activity is derived from refresh-token issuance (login + every token refresh).
    public int ActiveLast24Hours { get; set; }
    public int ActiveLast7Days { get; set; }
    public int ActiveLast30Days { get; set; }
    public int ActiveSessions { get; set; }
}

public class AdminGroupStatsDto
{
    public int Total { get; set; }
    public double AverageMembers { get; set; }
    public int WithoutWatchSessions { get; set; }
}

public class AdminMediaStatsDto
{
    public int Movies { get; set; }
    public int TvSeries { get; set; }
    public int TvSeasons { get; set; }
    public int TvEpisodes { get; set; }
}

public class AdminWatchSessionStatsDto
{
    public int Total { get; set; }
    public int Last30Days { get; set; }

    // Share of sessions where every member of the group (or the solo user) left a rating.
    public double FullyRatedPercent { get; set; }
}

public class AdminRatingStatsDto
{
    public int Total { get; set; }
    public double Average { get; set; }
    public List<AdminRatingBucketDto> Distribution { get; set; } = [];
}

public class AdminRatingBucketDto
{
    public int Value { get; set; }
    public int Count { get; set; }
}

public class AdminInvitationStatsDto
{
    public int Pending { get; set; }
    public int Accepted { get; set; }
    public int Expired { get; set; }
    public int Revoked { get; set; }

    // Pending rows whose ExpiresAt has passed; they are counted under Expired as well.
    public int ExpiredButStillPending { get; set; }

    // Accepted / resolved (everything that is no longer pending).
    public double AcceptanceRatePercent { get; set; }
}

public class AdminActivityResponseDto
{
    public int Days { get; set; }
    public List<AdminActivityDayDto> Series { get; set; } = [];
}

public class AdminActivityDayDto
{
    public DateOnly Date { get; set; }
    public int Signups { get; set; }
    public int ActiveUsers { get; set; }
    public int WatchSessions { get; set; }
    public int Ratings { get; set; }
}

public class AdminGroupDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public List<string> Members { get; set; } = [];
    public int WatchSessionCount { get; set; }
    public int RatingCount { get; set; }
    public DateTime? LastWatchSessionAt { get; set; }
    public int PendingInvitations { get; set; }
}

public static class AdminInvitationIssues
{
    public const string ExpiredButPending = "ExpiredButPending";
    public const string GroupMissing = "GroupMissing";
    public const string InviteeAlreadyMember = "InviteeAlreadyMember";
    public const string DuplicatePending = "DuplicatePending";
}

public class AdminInvitationDto
{
    public Guid Id { get; set; }
    public Guid GroupId { get; set; }
    public string? GroupName { get; set; }
    public Guid InviterUserId { get; set; }
    public string InviterUsername { get; set; } = string.Empty;
    public string InviteeEmail { get; set; } = string.Empty;
    public string? InviteeUsername { get; set; }
    public InvitationStatus Status { get; set; }

    // Status as the user experiences it: a Pending invitation past ExpiresAt is Expired.
    public InvitationStatus EffectiveStatus { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public string? AcceptedByUsername { get; set; }
    public List<string> Issues { get; set; } = [];
}

public class AdminInvitationListResponseDto
{
    public int TotalCount { get; set; }
    public List<AdminInvitationDto> Items { get; set; } = [];
}
