using Microsoft.EntityFrameworkCore;
using MovieRaterApi.Data;
using MovieRaterApi.Features.Authentication.Infrastructure;
using MovieRaterApi.Features.Shared.Interfaces;

namespace MovieRaterApi.Features.Shared.Services;

public class MediaEnrichmentService : IMediaEnrichmentService
{
    private readonly ApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<MediaEnrichmentService> _logger;

    public MediaEnrichmentService(
        ApplicationDbContext db,
        ICurrentUser currentUser,
        ILogger<MediaEnrichmentService> logger
    )
    {
        _db = db;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task EnrichAsync(IEnumerable<IEnrichableMediaDto> items, CancellationToken ct = default)
    {
        var list = items.ToList();

        if (list.Count == 0 || !_currentUser.IsAuthenticated)
            return;

        var tmdbIds = list.Select(i => i.TmdbId).ToList();
        var mediaTypes = list.Select(i => i.MediaType).Distinct().ToList();

        var cachedMedia = await _db
            .Media.Where(m => mediaTypes.Contains(m.MediaType) && tmdbIds.Contains(m.TmdbId))
            .ToListAsync(ct);

        var tmdbToGuid = cachedMedia.ToDictionary(m => (m.MediaType, m.TmdbId), m => m.Id);
        var guidIds = tmdbToGuid.Values.ToList();

        if (guidIds.Count == 0)
            return;

        var userMedias = await _db
            .UserMedias.Where(um =>
                um.UserId == _currentUser.UserId && guidIds.Contains(um.MediaId)
            )
            .ToListAsync(ct);

        var favLookup = userMedias
            .Where(um => um.IsFavorite)
            .Select(um => um.MediaId)
            .ToHashSet();

        var watchlistLookup = userMedias
            .Where(um => um.IsInWatchlist)
            .Select(um => um.MediaId)
            .ToHashSet();

        var currentUserId = _currentUser.UserId;

        var watchedLookup = await _db
            .WatchSessions.Where(ws =>
                guidIds.Contains(ws.MediaId)
                && (
                    (ws.CreatedByUserId == currentUserId && ws.GroupId == null)
                    || (
                        ws.Group != null
                        && ws.Group.UserGroups.Any(ug => ug.UserId == currentUserId)
                    )
                )
            )
            .GroupBy(ws => ws.MediaId)
            .Select(g => new { MediaId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(g => g.MediaId, g => g.Count, ct);

        foreach (var item in list)
        {
            if (tmdbToGuid.TryGetValue((item.MediaType, item.TmdbId), out var guidId))
            {
                item.Id = guidId;
                item.IsFavorite = favLookup.Contains(guidId);
                item.IsInWatchlist = watchlistLookup.Contains(guidId);
                item.WatchedCount = watchedLookup.GetValueOrDefault(guidId, 0);
            }
        }
    }

    public async Task EnrichAsync(IEnrichableMediaDto item, CancellationToken ct = default)
    {
        if (!_currentUser.IsAuthenticated)
            return;

        var media = await _db.Media.FirstOrDefaultAsync(
            m => m.MediaType == item.MediaType && m.TmdbId == item.TmdbId,
            ct
        );

        if (media is null)
            return;

        item.Id = media.Id;

        var userMedia = await _db.UserMedias.FirstOrDefaultAsync(
            um => um.UserId == _currentUser.UserId && um.MediaId == media.Id,
            ct
        );

        if (userMedia is not null)
        {
            item.IsFavorite = userMedia.IsFavorite;
            item.IsInWatchlist = userMedia.IsInWatchlist;
        }

        var currentUserId = _currentUser.UserId;

        item.WatchedCount = await _db.WatchSessions.CountAsync(
            ws =>
                ws.MediaId == media.Id
                && (
                    (ws.CreatedByUserId == currentUserId && ws.Group == null)
                    || (
                        ws.Group != null
                        && ws.Group.UserGroups.Any(ug => ug.UserId == currentUserId)
                    )
                ),
            ct
        );
    }
}