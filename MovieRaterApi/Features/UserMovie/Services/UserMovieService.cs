using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using MovieRaterApi.Data;
using MovieRaterApi.Features.Movies.Mapping;
using MovieRaterApi.Features.UserMovie.DTOs;
using MovieRaterApi.Features.UserMovie.Interfaces;
using MovieRaterApi.Infrastructure.Exceptions;
using MovieRaterApi.Infrastructure.Tmdb;
using MovieRaterApi.Infrastructure.Tmdb.Dtos.Responses;

namespace MovieRaterApi.Features.UserMovie.Services;

public class UserMovieService : IUserMovieService
{
    private static readonly string ImageConfigCacheKey = "TmdbImageConfig";

    private readonly ApplicationDbContext _db;
    private readonly ITmdbClient _tmdb;
    private readonly IMemoryCache _cache;
    private readonly ILogger<UserMovieService> _logger;

    public UserMovieService(
        ApplicationDbContext db,
        ITmdbClient tmdb,
        IMemoryCache cache,
        ILogger<UserMovieService> logger
    )
    {
        _db = db;
        _tmdb = tmdb;
        _cache = cache;
        _logger = logger;
    }

    private async Task<TmdbImageConfig> GetImageConfigAsync(CancellationToken ct = default)
    {
        return await _cache.GetOrCreateAsync(
                ImageConfigCacheKey,
                async entry =>
                {
                    entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24);
                    _logger.LogDebug("Fetching TMDB image configuration");
                    var config = await _tmdb.GetConfigurationAsync(ct);
                    return config.Images;
                }
            ) ?? new TmdbImageConfig { SecureBaseUrl = "https://image.tmdb.org/t/p/" };
    }

    public async Task<UserMovieResponseDto> SetFavoriteAsync(
        Guid movieId,
        Guid userId,
        bool isFavorite
    )
    {
        var movieExists = await _db.Movies.AnyAsync(m => m.Id == movieId);
        if (!movieExists)
            throw new NotFoundException("Movie not found.");

        var existing = await _db.UserMovies.FirstOrDefaultAsync(um =>
            um.UserId == userId && um.MovieId == movieId
        );

        if (!isFavorite)
        {
            if (existing is null)
                return DefaultResponse(movieId, userId);

            existing.IsFavorite = false;
            existing.UpdatedAt = DateTime.UtcNow;

            if (!existing.IsFavorite && !existing.IsInWatchlist)
            {
                _db.UserMovies.Remove(existing);
                await _db.SaveChangesAsync();

                _logger.LogInformation(
                    "Removed UserMovie for user {UserId}, movie {MovieId} (both flags false)",
                    userId,
                    movieId
                );

                return DefaultResponse(movieId, userId);
            }

            await _db.SaveChangesAsync();

            _logger.LogInformation(
                "User {UserId} movie {MovieId} isFavorite={IsFavorite}",
                userId,
                movieId,
                existing.IsFavorite
            );

            return ToDto(existing);
        }

        if (existing is null)
        {
            existing = new Data.Entities.UserMovie
            {
                UserId = userId,
                MovieId = movieId,
                IsFavorite = true,
                IsInWatchlist = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            };
            _db.UserMovies.Add(existing);
        }
        else
        {
            existing.IsFavorite = true;
            existing.UpdatedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();

        _logger.LogInformation(
            "User {UserId} movie {MovieId} isFavorite={IsFavorite}",
            userId,
            movieId,
            isFavorite
        );

        return ToDto(existing);
    }

    public async Task<UserMovieResponseDto> SetWatchlistAsync(
        Guid movieId,
        Guid userId,
        bool isInWatchlist
    )
    {
        var movieExists = await _db.Movies.AnyAsync(m => m.Id == movieId);
        if (!movieExists)
            throw new NotFoundException("Movie not found.");

        var existing = await _db.UserMovies.FirstOrDefaultAsync(um =>
            um.UserId == userId && um.MovieId == movieId
        );

        if (!isInWatchlist)
        {
            if (existing is null)
                return DefaultResponse(movieId, userId);

            existing.IsInWatchlist = false;
            existing.UpdatedAt = DateTime.UtcNow;

            if (!existing.IsFavorite && !existing.IsInWatchlist)
            {
                _db.UserMovies.Remove(existing);
                await _db.SaveChangesAsync();

                _logger.LogInformation(
                    "Removed UserMovie for user {UserId}, movie {MovieId} (both flags false)",
                    userId,
                    movieId
                );

                return DefaultResponse(movieId, userId);
            }

            await _db.SaveChangesAsync();

            _logger.LogInformation(
                "User {UserId} movie {MovieId} isInWatchlist={IsInWatchlist}",
                userId,
                movieId,
                existing.IsInWatchlist
            );

            return ToDto(existing);
        }

        if (existing is null)
        {
            existing = new Data.Entities.UserMovie
            {
                UserId = userId,
                MovieId = movieId,
                IsFavorite = false,
                IsInWatchlist = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            };
            _db.UserMovies.Add(existing);
        }
        else
        {
            existing.IsInWatchlist = true;
            existing.UpdatedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();

        _logger.LogInformation(
            "User {UserId} movie {MovieId} isInWatchlist={IsInWatchlist}",
            userId,
            movieId,
            isInWatchlist
        );

        return ToDto(existing);
    }

    public async Task<UserMovieResponseDto> GetAsync(Guid movieId, Guid userId)
    {
        var existing = await _db.UserMovies.FirstOrDefaultAsync(um =>
            um.UserId == userId && um.MovieId == movieId
        );

        if (existing is null)
            return DefaultResponse(movieId, userId);

        return ToDto(existing);
    }

    public async Task<PagedUserMoviesResponseDto> GetUserMoviesAsync(
        Guid userId,
        UserMovieListRequestDto request
    )
    {
        var query = _db.UserMovies
            .Include(um => um.Movie)
            .Where(um => um.UserId == userId);

        if (request.FavoritesOnly == true)
            query = query.Where(um => um.IsFavorite);

        if (request.WatchlistOnly == true)
            query = query.Where(um => um.IsInWatchlist);

        var totalResults = await query.CountAsync();

        var totalPages = (int)Math.Ceiling(totalResults / (double)request.PageSize);

        var raw = await query
            .OrderByDescending(um => um.UpdatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(um => new
            {
                MovieId = um.Movie.Id,
                TmdbId = um.Movie.TmdbId,
                Title = um.Movie.Title,
                PosterPath = um.Movie.PosterUrl,
                BackdropPath = um.Movie.BackdropUrl,
                ReleaseDate = um.Movie.ReleaseDate,
                VoteAverage = um.Movie.AverageTmdbRating,
                IsFavorite = um.IsFavorite,
                IsInWatchlist = um.IsInWatchlist,
                CreatedAt = um.CreatedAt,
                UpdatedAt = um.UpdatedAt,
            })
            .ToListAsync();

        var imageConfig = await GetImageConfigAsync();

        var items = raw
            .Select(r => new UserMovieWithMovieDto
            {
                Id = r.MovieId,
                TmdbId = r.TmdbId,
                Title = r.Title,
                PosterUrl = MovieMapper.BuildPosterUrl(r.PosterPath, imageConfig.SecureBaseUrl),
                BackdropUrl = MovieMapper.BuildBackdropUrl(r.BackdropPath, imageConfig.SecureBaseUrl),
                ReleaseDate = r.ReleaseDate?.ToString("yyyy-MM-dd"),
                VoteAverage = r.VoteAverage,
                IsFavorite = r.IsFavorite,
                IsInWatchlist = r.IsInWatchlist,
                CreatedAt = r.CreatedAt,
                UpdatedAt = r.UpdatedAt,
            })
            .ToList();

        _logger.LogInformation(
            "Retrieved {Count} user-movies for user {UserId} (favoritesOnly={FavoritesOnly}, watchlistOnly={WatchlistOnly})",
            items.Count,
            userId,
            request.FavoritesOnly,
            request.WatchlistOnly
        );

        return new PagedUserMoviesResponseDto
        {
            Page = request.Page,
            TotalPages = totalPages,
            TotalResults = totalResults,
            Results = items,
        };
    }

    private static UserMovieResponseDto ToDto(Data.Entities.UserMovie um)
    {
        return new UserMovieResponseDto
        {
            UserId = um.UserId,
            MovieId = um.MovieId,
            IsFavorite = um.IsFavorite,
            IsInWatchlist = um.IsInWatchlist,
            CreatedAt = um.CreatedAt,
            UpdatedAt = um.UpdatedAt,
        };
    }

    private static UserMovieResponseDto DefaultResponse(Guid movieId, Guid userId)
    {
        return new UserMovieResponseDto
        {
            UserId = userId,
            MovieId = movieId,
            IsFavorite = false,
            IsInWatchlist = false,
            CreatedAt = DateTime.MinValue,
            UpdatedAt = DateTime.MinValue,
        };
    }
}
