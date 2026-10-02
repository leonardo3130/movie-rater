using Microsoft.EntityFrameworkCore;
using MovieRaterApi.Data;
using MovieRaterApi.Data.Entities;
using MovieRaterApi.Features.Dashboard.DTOs;
using MovieRaterApi.Features.Dashboard.Interfaces;

namespace MovieRaterApi.Features.Dashboard.Services;

public class DashboardService : IDashboardService
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<DashboardService> _logger;

    public DashboardService(ApplicationDbContext db, ILogger<DashboardService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<DashboardResponseDto> GetDashboardAsync(Guid userId, Guid? groupId)
    {
        var now = DateTime.UtcNow;
        var startOfMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var startOfYear = new DateTime(now.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        var sessionsQuery = _db
            .WatchSessions.Include(ws => ws.Media)
            .Include(ws => ws.Ratings)
            .AsQueryable();

        if (groupId is null)
            sessionsQuery = sessionsQuery.Where(ws => ws.CreatedByUserId == userId);
        else
            sessionsQuery = sessionsQuery.Where(ws => ws.GroupId == groupId);

        var sessions = await sessionsQuery.ToListAsync();
        var moviesWatched = sessions.Count;
        var moviesThisMonth = sessions.Count(s => s.WatchedAt >= startOfMonth);
        var moviesThisYear = sessions.Count(s => s.WatchedAt >= startOfYear);

        var allRatings = sessions.SelectMany(s => s.Ratings).ToList();
        var averageRating =
            allRatings.Count > 0
                ? Math.Round(allRatings.Average(r => r.RatingValue), allRatings.Count)
                : 0;

        var favoriteGenres = await GetFavoriteGenresAsync(userId, groupId);
        var mostWatchedGenres = await GetMostWatchedGenresAsync(userId, groupId);

        var movieRatings = sessions
            .Where(s => s.Ratings.Count > 0)
            .GroupBy(s => s.MediaId)
            .Select(g => new
            {
                MediaId = g.Key,
                Title = g.First().Media.Title,
                WatchedCount = g.Count(),
                AvgRating = Math.Round(g.SelectMany(s => s.Ratings).Average(r => r.RatingValue), 2),
            })
            .ToList();

        MovieStatDto? highestRated = null;
        MovieStatDto? lowestRated = null;

        if (movieRatings.Count > 0)
        {
            var highest = movieRatings.OrderByDescending(m => m.AvgRating).First();
            var lowest = movieRatings.OrderByDescending(m => m.AvgRating).Last();

            highestRated = new MovieStatDto
            {
                MovieId = highest.MediaId,
                Title = highest.Title,
                AverageRating = highest.AvgRating,
                WatchedCount = highest.WatchedCount,
            };

            lowestRated = new MovieStatDto
            {
                MovieId = lowest.MediaId,
                Title = lowest.Title,
                AverageRating = lowest.AvgRating,
                WatchedCount = lowest.WatchedCount,
            };
        }

        var disagreementInfo = await GetDisagreementInfoAsync(userId, groupId);

        var rewatchCount = sessions
            .GroupBy(s => s.MediaId)
            .Where(g => g.Count() > 1)
            .Sum(g => g.Count() - 1);

        var streaks = ComputeStreaks(sessions.Select(s => s.WatchedAt).ToList());

        _logger.LogInformation(
            "Dashboard computed for user={UserId} group={GroupId}: {MoviesWatched} movies, avg rating {AvgRating}",
            userId,
            groupId,
            moviesWatched,
            averageRating
        );

        return new DashboardResponseDto
        {
            MoviesWatched = moviesWatched,
            MoviesThisMonth = moviesThisMonth,
            MoviesThisYear = moviesThisYear,
            AverageRating = averageRating,
            FavoriteGenres = favoriteGenres,
            MostWatchedGenres = mostWatchedGenres,
            HighestRatedMovie = highestRated,
            LowestRatedMovie = lowestRated,
            BiggestDisagreement = disagreementInfo.BiggestDisagreement,
            AverageDisagreement = disagreementInfo.AverageDisagreement,
            RewatchCount = rewatchCount,
            CurrentStreak = streaks.CurrentStreak,
            LongestStreak = streaks.LongestStreak,
        };
    }

    private async Task<List<GenreStatDto>> GetFavoriteGenresAsync(Guid userId, Guid? groupId)
    {
        var ratings = await _db
            .Ratings.Where(r =>
                groupId != null ? r.WatchSession.GroupId == groupId : r.UserId == userId
            )
            .Select(r => new { r.RatingValue, MediaId = r.WatchSession.MediaId })
            .ToListAsync();

        var rootMap = await BuildRootMediaMapAsync(ratings.Select(r => r.MediaId).ToList());
        var genresByRoot = await GetGenresByRootMediaAsync(rootMap.Values.ToList());

        var ratingGenres = new List<(string GenreName, int RatingValue)>();
        foreach (var rating in ratings)
        {
            if (
                !rootMap.TryGetValue(rating.MediaId, out var root)
                || !genresByRoot.TryGetValue(root, out var names)
            )
                continue;

            foreach (var name in names)
                ratingGenres.Add((name, rating.RatingValue));
        }

        return ratingGenres
            .GroupBy(x => x.GenreName)
            .Select(g => new GenreStatDto
            {
                GenreName = g.Key,
                Count = g.Count(),
                AverageRating = g.Average(x => x.RatingValue),
            })
            .OrderByDescending(g => g.AverageRating)
            .Take(5)
            .ToList();
    }

    private async Task<List<GenreStatDto>> GetMostWatchedGenresAsync(Guid userId, Guid? groupId)
    {
        var mediaIds = await _db
            .WatchSessions.Where(ws =>
                groupId != null ? ws.GroupId == groupId : ws.CreatedByUserId == userId
            )
            .Select(ws => ws.MediaId)
            .ToListAsync();

        var rootMap = await BuildRootMediaMapAsync(mediaIds);
        var genresByRoot = await GetGenresByRootMediaAsync(rootMap.Values.ToList());

        var watchedGenreNames = new List<string>();
        foreach (var mediaId in mediaIds)
        {
            if (
                !rootMap.TryGetValue(mediaId, out var root)
                || !genresByRoot.TryGetValue(root, out var names)
            )
                continue;

            watchedGenreNames.AddRange(names);
        }

        return watchedGenreNames
            .GroupBy(name => name)
            .Select(g => new GenreStatDto
            {
                GenreName = g.Key,
                Count = g.Count(),
                AverageRating = 0,
            })
            .OrderByDescending(g => g.Count)
            .Take(5)
            .ToList();
    }

    private async Task<Dictionary<Guid, Guid>> BuildRootMediaMapAsync(List<Guid> mediaIds)
    {
        var distinct = mediaIds.Distinct().ToList();
        if (distinct.Count == 0)
            return new Dictionary<Guid, Guid>();

        var media = await _db
            .Media.Where(m => distinct.Contains(m.Id))
            .Select(m => new { m.Id, m.MediaType })
            .ToListAsync();

        var seasonIds = media
            .Where(m => m.MediaType == MediaType.TvSeason)
            .Select(m => m.Id)
            .ToList();

        var episodeIds = media
            .Where(m => m.MediaType == MediaType.TvEpisode)
            .Select(m => m.Id)
            .ToList();

        var seriesBySeasonId = new Dictionary<Guid, Guid>();
        if (seasonIds.Count > 0)
        {
            seriesBySeasonId = await _db
                .TvSeasons.Where(s => seasonIds.Contains(s.Id))
                .Select(s => new { s.Id, s.SeriesId })
                .ToDictionaryAsync(s => s.Id, s => s.SeriesId);
        }

        var seasonByEpisodeId = new Dictionary<Guid, Guid>();
        if (episodeIds.Count > 0)
        {
            seasonByEpisodeId = await _db
                .TvEpisodes.Where(e => episodeIds.Contains(e.Id))
                .Select(e => new { e.Id, e.SeasonId })
                .ToDictionaryAsync(e => e.Id, e => e.SeasonId);
        }

        var seasonIdsToResolve = seasonIds
            .Concat(seasonByEpisodeId.Values)
            .Distinct()
            .ToList();
        if (seasonIdsToResolve.Count > 0)
        {
            seriesBySeasonId = await _db
                .TvSeasons.Where(s => seasonIdsToResolve.Contains(s.Id))
                .Select(s => new { s.Id, s.SeriesId })
                .ToDictionaryAsync(s => s.Id, s => s.SeriesId);
        }

        var map = new Dictionary<Guid, Guid>();
        foreach (var m in media)
        {
            var root = m.MediaType switch
            {
                MediaType.Movie or MediaType.TvSeries => m.Id,
                MediaType.TvSeason => seriesBySeasonId.GetValueOrDefault(m.Id, m.Id),
                _ => seriesBySeasonId.GetValueOrDefault(
                    seasonByEpisodeId.GetValueOrDefault(m.Id, Guid.Empty),
                    m.Id
                ),
            };

            map[m.Id] = root;
        }

        return map;
    }

    private async Task<Dictionary<Guid, string[]>> GetGenresByRootMediaAsync(
        List<Guid> rootIds
    )
    {
        var distinct = rootIds.Distinct().ToList();
        if (distinct.Count == 0)
            return new Dictionary<Guid, string[]>();

        var rows = await _db
            .MediaGenres.Where(mg => distinct.Contains(mg.MediaId))
            .Select(mg => new { mg.MediaId, GenreName = mg.Genre.Name })
            .ToListAsync();

        return rows.GroupBy(x => x.MediaId).ToDictionary(g => g.Key, g => g.Select(x => x.GenreName).ToArray());
    }

    // TODO:: add user infos for the 2 involed in the boggest disagreement
    private async Task<(
        MovieStatDto? BiggestDisagreement,
        double AverageDisagreement
    )> GetDisagreementInfoAsync(Guid userId, Guid? groupId)
    {
        var sessionsWithAtLeastTwoRatings = await _db
            .WatchSessions.Include(ws => ws.Media)
            .Include(ws => ws.Ratings)
            .Where(ws =>
                (groupId != null ? ws.GroupId == groupId : ws.CreatedByUserId == userId)
                && ws.Ratings.Count >= 2
            )
            .ToListAsync();

        if (sessionsWithAtLeastTwoRatings.Count == 0)
            return (null, 0);

        var disagreements = sessionsWithAtLeastTwoRatings
            .Select(s =>
            {
                var ratings = s.Ratings.ToList();
                var diff = Math.Abs(
                    ratings.Min(r => r.RatingValue) - ratings.Max(r => r.RatingValue)
                );
                return new { Session = s, Disagreement = diff };
            })
            .ToList();

        var biggest = disagreements.OrderByDescending(d => d.Disagreement).First();

        var biggestDisagreement = new MovieStatDto
        {
            MovieId = biggest.Session.MediaId,
            Title = biggest.Session.Media.Title,
            AverageRating = Math.Round(biggest.Session.Ratings.Average(r => r.RatingValue), 2),
            WatchedCount = 1,
        };

        var averageDisagreement = Math.Round(disagreements.Average(d => d.Disagreement), 2);

        return (biggestDisagreement, averageDisagreement);
    }

    private static (int CurrentStreak, int LongestStreak) ComputeStreaks(List<DateTime> watchDates)
    {
        if (watchDates.Count == 0)
            return (0, 0);

        var distinctWeeks = watchDates
            .Select(d => GetWeekStart(d))
            .Distinct()
            .OrderBy(w => w)
            .ToList();

        var longestStreak = 1;
        var currentRun = 1;

        for (var i = 1; i < distinctWeeks.Count; i++)
        {
            var diff = (distinctWeeks[i] - distinctWeeks[i - 1]).Days;
            if (diff <= 10)
            {
                currentRun++;
                if (currentRun > longestStreak)
                    longestStreak = currentRun;
            }
            else
            {
                currentRun = 1;
            }
        }

        var currentStreak = 0;
        var todayWeekStart = GetWeekStart(DateTime.UtcNow);

        for (var i = distinctWeeks.Count - 1; i >= 0; i--)
        {
            var expected = todayWeekStart.AddDays(-(currentStreak * 7));
            var diff = (expected - distinctWeeks[i]).Days;
            if (Math.Abs(diff) <= 3)
            {
                currentStreak++;
            }
            else
            {
                break;
            }
        }

        return (currentStreak, longestStreak);
    }

    private static DateTime GetWeekStart(DateTime date)
    {
        var diff = (7 + (date.DayOfWeek - DayOfWeek.Monday)) % 7;
        return date.AddDays(-diff).Date;
    }
}
