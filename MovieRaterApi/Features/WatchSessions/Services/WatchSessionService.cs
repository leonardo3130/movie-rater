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
        var movie = await _db.Movies.FirstOrDefaultAsync(m => m.Id == request.MovieId);
        if (movie is null)
            throw new NotFoundException("Movie not found.");

        var session = new WatchSession
        {
            Id = Guid.NewGuid(),
            GroupId = groupId,
            MovieId = request.MovieId,
            WatchedAt = request.WatchedAt,
            Location = request.Location,
            Notes = request.Notes,
            CreatedByUserId = userId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

        _db.WatchSessions.Add(session);
        await _db.SaveChangesAsync();

        _logger.LogInformation(
            "Watch session {SessionId} created by user {UserId} for movie {MovieId} in group {GroupId}",
            session.Id,
            userId,
            request.MovieId,
            groupId
        );

        var creator = await _db.Users.FirstAsync(u => u.Id == userId);

        return new WatchSessionResponseDto
        {
            Id = session.Id,
            MovieId = movie.Id,
            MovieTitle = movie.Title,
            MoviePosterUrl = movie.PosterUrl,
            WatchedAt = session.WatchedAt,
            Location = session.Location,
            Notes = session.Notes,
            CreatedByUserId = userId,
            CreatedByUsername = creator.Username,
            CreatedAt = session.CreatedAt,
            Ratings = [],
        };
    }

    public async Task<WatchSessionListResponseDto> GetAllAsync(WatchSessionQueryDto query)
    {
        var userGroups = await _db
            .UserGroups.Where(ug => ug.UserId == _currentUser.UserId)
            .ToListAsync();

        if (query.GroupId is not null && !userGroups.Any(ug => ug.GroupId == query.GroupId))
            throw new ForbiddenException("You are not part of the group");

        var sessionsQuery = _db
            .WatchSessions.Include(ws => ws.Movie)
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

        if (query.MovieId.HasValue)
            sessionsQuery = sessionsQuery.Where(ws => ws.MovieId == query.MovieId.Value);

        var totalCount = await sessionsQuery.CountAsync();

        var sessions = await sessionsQuery
            .OrderByDescending(ws => ws.WatchedAt)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync();

        var items = sessions
            .Select(s => new WatchSessionListItemDto
            {
                Id = s.Id,
                MovieId = s.MovieId,
                MovieTitle = s.Movie.Title,
                MoviePosterUrl = s.Movie.PosterUrl,
                WatchedAt = s.WatchedAt,
                Location = s.Location,
                Notes = s.Notes,
                CreatedByUserId = s.CreatedByUserId,
                CreatedByUsername = s.CreatedByUser.Username,
                CreatedAt = s.CreatedAt,
                RatingCount = s.Ratings.Count,
                GroupId = s.GroupId,
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
            .WatchSessions.Include(ws => ws.Movie)
            .Include(ws => ws.CreatedByUser)
            .Include(ws => ws.Ratings)
                .ThenInclude(r => r.User)
            .FirstOrDefaultAsync(ws => ws.Id == id);

        if (session is null)
            throw new NotFoundException("Watch session not found.");

        return new WatchSessionResponseDto
        {
            Id = session.Id,
            MovieId = session.MovieId,
            MovieTitle = session.Movie.Title,
            MoviePosterUrl = session.Movie.PosterUrl,
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
}
