namespace MovieRaterApi.Features.WatchSessions.DTOs;

public class CreateWatchSessionRequestDto
{
    public Guid MovieId { get; set; }
    public DateTime WatchedAt { get; set; }
    public string? Location { get; set; }
    public string? Notes { get; set; }
}

public class WatchSessionListItemDto
{
    public Guid Id { get; set; }
    public Guid MovieId { get; set; }
    public string MovieTitle { get; set; } = string.Empty;
    public string? MoviePosterUrl { get; set; }
    public DateTime WatchedAt { get; set; }
    public string? Location { get; set; }
    public string? Notes { get; set; }
    public Guid CreatedByUserId { get; set; }
    public string CreatedByUsername { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public int RatingCount { get; set; }
}

public class WatchSessionListResponseDto
{
    public List<WatchSessionListItemDto> Items { get; set; } = [];
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}

public class WatchSessionQueryDto
{
    public Guid? MovieId { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class WatchSessionResponseDto
{
    public Guid Id { get; set; }
    public Guid MovieId { get; set; }
    public string MovieTitle { get; set; } = string.Empty;
    public string? MoviePosterUrl { get; set; }
    public DateTime WatchedAt { get; set; }
    public string? Location { get; set; }
    public string? Notes { get; set; }
    public Guid CreatedByUserId { get; set; }
    public string CreatedByUsername { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public List<RatingSummaryDto> Ratings { get; set; } = [];
}

public class RatingSummaryDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public int RatingValue { get; set; }
    public string? Review { get; set; }
}

public class HeatmapResponseDto
{
    public Dictionary<string, int> DailyCounts { get; set; } = [];
}

public class HeatmapQueryDto
{
    public int Days { get; set; } = 365;
}
