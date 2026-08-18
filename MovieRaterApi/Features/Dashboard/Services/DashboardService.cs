using Microsoft.EntityFrameworkCore;
using MovieRaterApi.Data;
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
            .WatchSessions.Include(ws => ws.Movie)
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
            .GroupBy(s => s.MovieId)
            .Select(g => new
            {
                MovieId = g.Key,
                Title = g.First().Movie.Title,
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
                MovieId = highest.MovieId,
                Title = highest.Title,
                AverageRating = highest.AvgRating,
                WatchedCount = highest.WatchedCount,
            };

            lowestRated = new MovieStatDto
            {
                MovieId = lowest.MovieId,
                Title = lowest.Title,
                AverageRating = lowest.AvgRating,
                WatchedCount = lowest.WatchedCount,
            };
        }

        var disagreementInfo = await GetDisagreementInfoAsync(userId, groupId);

        var rewatchCount = sessions
            .GroupBy(s => s.MovieId)
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
        var genreStats = await _db
            .Ratings.Where(r =>
                groupId != null ? r.WatchSession.GroupId == groupId : r.UserId == userId
            )
            .Join(
                _db.MovieGenres,
                r => r.WatchSession.MovieId,
                mg => mg.MovieId,
                (r, mg) => new { r.RatingValue, mg.GenreId }
            )
            .Join(
                _db.Genres,
                x => x.GenreId,
                g => g.Id,
                (x, g) => new { x.RatingValue, GenreName = g.Name }
            )
            .GroupBy(x => x.GenreName)
            .Select(g => new GenreStatDto
            {
                GenreName = g.Key,
                Count = g.Count(),
                AverageRating = g.Average(x => x.RatingValue),
            })
            .OrderByDescending(g => g.AverageRating)
            .Take(5)
            .ToListAsync();

        return genreStats;
    }

    private async Task<List<GenreStatDto>> GetMostWatchedGenresAsync(Guid userId, Guid? groupId)
    {
        var genreStats = await _db
            .WatchSessions.Where(ws =>
                groupId != null ? ws.GroupId == groupId : ws.CreatedByUserId == userId
            )
            .SelectMany(ws => ws.Movie.MovieGenres)
            .GroupBy(mg => mg.Genre.Name)
            .Select(g => new GenreStatDto
            {
                GenreName = g.Key,
                Count = g.Count(),
                AverageRating = 0,
            })
            .OrderByDescending(g => g.Count)
            .Take(5)
            .ToListAsync();

        return genreStats;
    }

    // TODO:: add user infos for the 2 involed in the boggest disagreement
    private async Task<(
        MovieStatDto? BiggestDisagreement,
        double AverageDisagreement
    )> GetDisagreementInfoAsync(Guid userId, Guid? groupId)
    {
        var sessionsWithAtLeastTwoRatings = await _db
            .WatchSessions.Include(ws => ws.Movie)
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
            MovieId = biggest.Session.MovieId,
            Title = biggest.Session.Movie.Title,
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
