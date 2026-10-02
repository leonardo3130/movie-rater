using Microsoft.EntityFrameworkCore;
using MovieRaterApi.Data;
using MovieRaterApi.Data.Entities;
using MovieRaterApi.Features.Authentication.Infrastructure;
using MovieRaterApi.Features.WatchSessions.DTOs;
using MovieRaterApi.Features.WatchSessions.Interfaces;
using MovieRaterApi.Infrastructure.Exceptions;

namespace MovieRaterApi.Features.WatchSessions.Services;

public class WatchSessionService : IWatchSessionService
{
    private readonly ApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<WatchSessionService> _logger;

    public WatchSessionService(
        ApplicationDbContext db,
        ICurrentUser currentUser,
        ILogger<WatchSessionService> logger
    )
    {
        _db = db;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<WatchSessionResponseDto> CreateAsync(
        CreateWatchSessionRequestDto request,
        Guid userId,
        Guid? groupId
    )
    {
        var media = await _db.Media.FirstOrDefaultAsync(m => m.Id == request.MediaId);
        if (media is null)
            throw new NotFoundException("Media not found.");

        if (media.MediaType is not MediaType.Movie and not MediaType.TvEpisode)
            throw new BadRequestException("Watch sessions can only be created for movies or episodes.");

        var session = new WatchSession
        {
            Id = Guid.NewGuid(),
            GroupId = groupId,
            MediaId = request.MediaId,
            WatchedAt = request.WatchedAt,
            Location = request.Location,
            Notes = request.Notes,
            CreatedByUserId = userId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
        session.Media = media;

        _db.WatchSessions.Add(session);
        await _db.SaveChangesAsync();

        _logger.LogInformation(
            "Watch session {SessionId} created by user {UserId} for media {MediaId} in group {GroupId}",
            session.Id,
            userId,
            request.MediaId,
            groupId
        );

        session.CreatedByUser = await _db.Users.FirstAsync(u => u.Id == userId);

        return await ToResponseDtoAsync(session);
    }

    public async Task<WatchSessionListResponseDto> GetAllAsync(WatchSessionQueryDto query)
    {
        var userGroups = await _db
            .UserGroups.Where(ug => ug.UserId == _currentUser.UserId)
            .ToListAsync();

        if (query.GroupId is not null && !userGroups.Any(ug => ug.GroupId == query.GroupId))
            throw new ForbiddenException("You are not part of the group");

        var sessionsQuery = _db
            .WatchSessions.Include(ws => ws.Media)
            .Include(ws => ws.CreatedByUser)
            .Include(ws => ws.Ratings)
            .AsQueryable();

        var userGroupsIds = userGroups.Select(ug => ug.GroupId).ToList();

        if (query.GroupId is not null)
            sessionsQuery = sessionsQuery.Where(ws => ws.GroupId == query.GroupId);
        else
            sessionsQuery = sessionsQuery.Where(ws =>
                ws.CreatedByUserId == _currentUser.UserId
                || (ws.GroupId != null && userGroupsIds.Contains(ws.GroupId.Value))
            );

        if (query.MediaId.HasValue)
            sessionsQuery = sessionsQuery.Where(ws => ws.MediaId == query.MediaId.Value);

        var totalCount = await sessionsQuery.CountAsync();

        var sessions = await sessionsQuery
            .OrderByDescending(ws => ws.WatchedAt)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync();

        var tvInfo = await BuildTvDisplayAsync(sessions);

        var items = sessions
            .Select(s =>
            {
                var dto = new WatchSessionListItemDto
                {
                    Id = s.Id,
                    MediaId = s.MediaId,
                    Title = s.Media.Title,
                    PosterUrl = s.Media.PosterUrl,
                    MediaType = s.Media.MediaType,
                    SeasonNumber = null,
                    EpisodeNumber = null,
                    WatchedAt = s.WatchedAt,
                    Location = s.Location,
                    Notes = s.Notes,
                    CreatedByUserId = s.CreatedByUserId,
                    CreatedByUsername = s.CreatedByUser.Username,
                    CreatedAt = s.CreatedAt,
                    RatingCount = s.Ratings.Count,
                    GroupId = s.GroupId,
                };

                ApplyTvInfo(dto, s, tvInfo);

                return dto;
            })
            .ToList();

        return new WatchSessionListResponseDto
        {
            Items = items,
            TotalCount = totalCount,
            Page = query.Page,
            PageSize = query.PageSize,
        };
    }

    public async Task<WatchSessionResponseDto> GetByIdAsync(Guid id)
    {
        var session = await _db
            .WatchSessions.Include(ws => ws.Media)
            .Include(ws => ws.CreatedByUser)
            .Include(ws => ws.Ratings)
                .ThenInclude(r => r.User)
            .FirstOrDefaultAsync(ws => ws.Id == id);

        if (session is null)
            throw new NotFoundException("Watch session not found.");

        return await ToResponseDtoAsync(session);
    }

    public async Task<WatchSessionResponseDto> UpdateAsync(
        Guid id,
        UpdateWatchSessionRequestDto request,
        Guid userId
    )
    {
        var session = await _db
            .WatchSessions.Include(ws => ws.Media)
            .Include(ws => ws.CreatedByUser)
            .FirstOrDefaultAsync(ws => ws.Id == id);

        if (session is null)
            throw new NotFoundException("Watch session not found.");

        if (session.CreatedByUserId != userId)
            throw new ForbiddenException("You can only edit your own watch sessions.");

        session.WatchedAt = request.WatchedAt;
        session.Location = request.Location;
        session.Notes = request.Notes;
        session.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        _logger.LogInformation(
            "Watch session {SessionId} updated by user {UserId} with watchedAt {WatchedAt}",
            id,
            userId,
            request.WatchedAt
        );

        return await ToResponseDtoAsync(session);
    }

    private async Task<WatchSessionResponseDto> ToResponseDtoAsync(WatchSession session)
    {
        var tvInfo = await BuildTvDisplayAsync([session]);

        var dto = new WatchSessionResponseDto
        {
            Id = session.Id,
            MediaId = session.MediaId,
            Title = session.Media.Title,
            PosterUrl = session.Media.PosterUrl,
            MediaType = session.Media.MediaType,
            WatchedAt = session.WatchedAt,
            Location = session.Location,
            Notes = session.Notes,
            CreatedByUserId = session.CreatedByUserId,
            CreatedByUsername = session.CreatedByUser.Username,
            CreatedAt = session.CreatedAt,
            Ratings = session
                .Ratings.Select(r => new RatingSummaryDto
                {
                    Id = r.Id,
                    UserId = r.UserId,
                    Username = r.User.Username,
                    RatingValue = r.RatingValue,
                    Review = r.Review,
                })
                .ToList(),
        };

        ApplyTvInfo(dto, session, tvInfo);

        return dto;
    }

    private static void ApplyTvInfo(
        WatchSessionListItemDto dto,
        WatchSession session,
        Dictionary<Guid, TvDisplayInfo> tvInfo
    )
    {
        if (session.Media is TvEpisode episode)
        {
            var info = tvInfo[episode.Id];
            dto.SeriesTitle = info.SeriesTitle;
            dto.SeasonNumber = info.SeasonNumber;
            dto.EpisodeNumber = info.EpisodeNumber;
        }
    }

    private static void ApplyTvInfo(
        WatchSessionResponseDto dto,
        WatchSession session,
        Dictionary<Guid, TvDisplayInfo> tvInfo
    )
    {
        if (session.Media is TvEpisode episode)
        {
            var info = tvInfo[episode.Id];
            dto.SeriesTitle = info.SeriesTitle;
            dto.SeasonNumber = info.SeasonNumber;
            dto.EpisodeNumber = info.EpisodeNumber;
        }
    }

    private async Task<Dictionary<Guid, TvDisplayInfo>> BuildTvDisplayAsync(
        List<WatchSession> sessions,
        CancellationToken ct = default
    )
    {
        var display = new Dictionary<Guid, TvDisplayInfo>();

        var episodes = sessions
            .Where(s => s.Media is TvEpisode)
            .Select(s => (TvEpisode)s.Media)
            .GroupBy(e => e.Id)
            .Select(g => g.First())
            .ToList();

        if (episodes.Count == 0)
            return display;

        var seasonIds = episodes.Select(e => e.SeasonId).Distinct().ToList();

        var seriesBySeasonId = new Dictionary<Guid, Guid>();
        if (seasonIds.Count > 0)
        {
            seriesBySeasonId = await _db
                .TvSeasons.Where(s => seasonIds.Contains(s.Id))
                .Select(s => new { s.Id, s.SeriesId })
                .ToDictionaryAsync(s => s.Id, s => s.SeriesId, ct);
        }

        var seriesIds = seriesBySeasonId.Values.Distinct().ToList();

        var titlesBySeriesId = new Dictionary<Guid, string>();
        if (seriesIds.Count > 0)
        {
            titlesBySeriesId = await _db
                .TvSeries.Where(s => seriesIds.Contains(s.Id))
                .Select(s => new { s.Id, s.Title })
                .ToDictionaryAsync(s => s.Id, s => s.Title, ct);
        }

        foreach (var episode in episodes)
        {
            var seriesId = seriesBySeasonId.GetValueOrDefault(episode.SeasonId, Guid.Empty);
            display[episode.Id] = new TvDisplayInfo(
                titlesBySeriesId.GetValueOrDefault(seriesId, ""),
                episode.SeasonNumber,
                episode.EpisodeNumber
            );
        }

        return display;
    }

    public async Task DeleteAsync(Guid id, Guid userId)
    {
        var session = await _db.WatchSessions.FirstOrDefaultAsync(ws => ws.Id == id);

        if (session is null)
            throw new NotFoundException("Watch session not found.");

        if (session.CreatedByUserId != userId)
            throw new ForbiddenException("You can only delete your own watch sessions.");

        _db.WatchSessions.Remove(session);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Watch session {SessionId} deleted by user {UserId}", id, userId);
    }

    public async Task<HeatmapResponseDto> GetHeatmapAsync(int days, Guid userId, Guid? groupId)
    {
        var cutoffDate = DateTime.UtcNow.Date.AddDays(-days);

        var countsQuery = _db.WatchSessions.Where(ws => ws.WatchedAt >= cutoffDate);

        if (groupId is null)
            countsQuery = countsQuery.Where(ws => ws.CreatedByUserId == userId);
        else
            countsQuery = countsQuery.Where(ws => ws.GroupId == groupId);

        var counts = await countsQuery
            .GroupBy(ws => ws.WatchedAt.Date)
            .Select(g => new { Date = g.Key, Count = g.Count() })
            .ToListAsync();

        var dailyCounts = new Dictionary<string, int>();
        foreach (var c in counts)
            dailyCounts[c.Date.ToString("yyyy-MM-dd")] = c.Count;

        _logger.LogInformation(
            "Heatmap requested for group={GroupId}, user={UserId}, days={Days}, dates={DateCount}",
            groupId,
            userId,
            days,
            dailyCounts.Count
        );

        return new HeatmapResponseDto { DailyCounts = dailyCounts };
    }

    private sealed record TvDisplayInfo(
        string SeriesTitle,
        int? SeasonNumber,
        int? EpisodeNumber
    );
}