using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using MovieRaterApi.Data;
using MovieRaterApi.Data.Entities;
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
        var movieExists = await _db.Media.AnyAsync(m => m.Id == movieId);
        if (!movieExists)
            throw new NotFoundException("Movie not found.");

        var existing = await _db.UserMedias.FirstOrDefaultAsync(um =>
            um.UserId == userId && um.MediaId == movieId
        );

        if (!isFavorite)
        {
            if (existing is null)
                return DefaultResponse(movieId, userId);

            existing.IsFavorite = false;
            existing.UpdatedAt = DateTime.UtcNow;

            if (!existing.IsFavorite && !existing.IsInWatchlist)
            {
                _db.UserMedias.Remove(existing);
                await _db.SaveChangesAsync();

                _logger.LogInformation(
                    "Removed UserMedia for user {UserId}, media {MediaId} (both flags false)",
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
            existing = new Data.Entities.UserMedia
            {
                UserId = userId,
                MediaId = movieId,
                IsFavorite = true,
                IsInWatchlist = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            };
            _db.UserMedias.Add(existing);
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
        var movieExists = await _db.Media.AnyAsync(m => m.Id == movieId);
        if (!movieExists)
            throw new NotFoundException("Movie not found.");

        var existing = await _db.UserMedias.FirstOrDefaultAsync(um =>
            um.UserId == userId && um.MediaId == movieId
        );

        if (!isInWatchlist)
        {
            if (existing is null)
                return DefaultResponse(movieId, userId);

            existing.IsInWatchlist = false;
            existing.UpdatedAt = DateTime.UtcNow;

            if (!existing.IsFavorite && !existing.IsInWatchlist)
            {
                _db.UserMedias.Remove(existing);
                await _db.SaveChangesAsync();

                _logger.LogInformation(
                    "Removed UserMedia for user {UserId}, media {MediaId} (both flags false)",
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
            existing = new Data.Entities.UserMedia
            {
                UserId = userId,
                MediaId = movieId,
                IsFavorite = false,
                IsInWatchlist = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            };
            _db.UserMedias.Add(existing);
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
        var existing = await _db.UserMedias.FirstOrDefaultAsync(um =>
            um.UserId == userId && um.MediaId == movieId
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
        var query = _db.UserMedias.Include(um => um.Media).Where(um => um.UserId == userId);

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
                MediaId = um.Media.Id,
                TmdbId = um.Media.TmdbId,
                MediaType = um.Media.MediaType,
                Title = um.Media.Title,
                PosterPath = um.Media.PosterUrl,
                BackdropPath = um.Media.BackdropUrl,
                ReleaseDate = um.Media.ReleaseDate,
                VoteAverage = um.Media.AverageTmdbRating,
                IsFavorite = um.IsFavorite,
                IsInWatchlist = um.IsInWatchlist,
                CreatedAt = um.CreatedAt,
                UpdatedAt = um.UpdatedAt,
            })
            .ToListAsync();

        var tvInfo = await BuildTvMediaInfoAsync(
            raw.Where(r => r.MediaType is MediaType.TvSeason or MediaType.TvEpisode)
                .Select(r => r.MediaId)
                .ToList()
        );

        var imageConfig = await GetImageConfigAsync();

        var items = raw.Select(r =>
            {
                tvInfo.TryGetValue(r.MediaId, out var info);
                return new UserMovieWithMovieDto
                {
                    Id = r.MediaId,
                    TmdbId = r.TmdbId,
                    MediaType = r.MediaType,
                    Title = r.Title,
                    PosterUrl = MovieMapper.BuildPosterUrl(r.PosterPath, imageConfig.SecureBaseUrl),
                    BackdropUrl = MovieMapper.BuildBackdropUrl(
                        r.BackdropPath,
                        imageConfig.SecureBaseUrl
                    ),
                    ReleaseDate = r.ReleaseDate?.ToString("yyyy-MM-dd"),
                    VoteAverage = r.VoteAverage,
                    SeriesTmdbId = r.MediaType == MediaType.TvSeries ? r.TmdbId : info?.SeriesTmdbId,
                    SeriesTitle = r.MediaType == MediaType.TvSeries ? r.Title : info?.SeriesTitle,
                    SeasonNumber = r.MediaType == MediaType.TvSeries ? null : info?.SeasonNumber,
                    EpisodeNumber = info?.EpisodeNumber,
                    IsFavorite = r.IsFavorite,
                    IsInWatchlist = r.IsInWatchlist,
                    CreatedAt = r.CreatedAt,
                    UpdatedAt = r.UpdatedAt,
                };
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

    private static UserMovieResponseDto ToDto(Data.Entities.UserMedia um)
    {
        return new UserMovieResponseDto
        {
            UserId = um.UserId,
            MovieId = um.MediaId,
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

    private async Task<Dictionary<Guid, TvMediaInfoDto>> BuildTvMediaInfoAsync(
        List<Guid> mediaIds
    )
    {
        if (mediaIds.Count == 0)
            return [];

        var rows = await _db
            .Media.Where(m => mediaIds.Contains(m.Id))
            .Select(m => new TvMediaInfoDto
            {
                MediaId = m.Id,
                SeriesTmdbId =
                    m.MediaType == MediaType.TvSeason
                        ? ((TvSeason)m).Series!.TmdbId
                        : ((TvEpisode)m).Season!.Series!.TmdbId,
                SeriesTitle =
                    m.MediaType == MediaType.TvSeason
                        ? ((TvSeason)m).Series!.Title
                        : ((TvEpisode)m).Season!.Series!.Title,
                SeasonNumber =
                    m.MediaType == MediaType.TvSeason
                        ? ((TvSeason)m).SeasonNumber
                        : ((TvEpisode)m).SeasonNumber,
                EpisodeNumber = m.MediaType == MediaType.TvEpisode
                    ? ((TvEpisode)m).EpisodeNumber
                    : (int?)null,
            })
            .ToDictionaryAsync(x => x.MediaId);

        return rows;
    }

    private sealed record TvMediaInfoDto
    {
        public Guid MediaId { get; init; }
        public int SeriesTmdbId { get; init; }
        public string? SeriesTitle { get; init; }
        public int SeasonNumber { get; init; }
        public int? EpisodeNumber { get; init; }
    }
}
