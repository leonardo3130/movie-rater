using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using MovieRaterApi.Data;
using MovieRaterApi.Data.Entities;
using MovieRaterApi.Features.Authentication.Infrastructure;
using MovieRaterApi.Features.Movies.DTOs;
using MovieRaterApi.Features.Movies.Interfaces;
using MovieRaterApi.Features.Movies.Mapping;
using MovieRaterApi.Infrastructure.Tmdb;
using MovieRaterApi.Infrastructure.Tmdb.Dtos.Requests;
using MovieRaterApi.Infrastructure.Tmdb.Dtos.Responses;

namespace MovieRaterApi.Features.Movies.Services;

public class MovieService : IMovieService
{
    private static readonly string ImageConfigCacheKey = "TmdbImageConfig";

    private readonly ITmdbClient _tmdb;
    private readonly ApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IMemoryCache _cache;
    private readonly ILogger<MovieService> _logger;

    public MovieService(
        ITmdbClient tmdb,
        ApplicationDbContext db,
        ICurrentUser currentUser,
        IMemoryCache cache,
        ILogger<MovieService> logger
    )
    {
        _tmdb = tmdb;
        _db = db;
        _currentUser = currentUser;
        _cache = cache;
        _logger = logger;
    }

    public async Task<PagedMoviesResponseDto> SearchMoviesAsync(
        SearchMoviesRequestDto request,
        CancellationToken ct = default
    )
    {
        var query = new TmdbSearchMovieQuery
        {
            Query = request.Query,
            Page = request.Page,
            Year = request.Year,
            PrimaryReleaseYear = request.PrimaryReleaseYear,
            IncludeAdult = request.IncludeAdult,
            Region = request.Region,
            Language = request.Language,
        };

        _logger.LogInformation(
            "Searching movies: query={Query}, page={Page}",
            request.Query,
            request.Page
        );

        var response = await _tmdb.SearchMoviesAsync(query, ct);
        var config = await GetImageConfigAsync(ct);

        var result = MovieMapper.ToPagedResult(
            response,
            item => MovieMapper.BuildPosterUrl(item.PosterPath, config.SecureBaseUrl),
            item => MovieMapper.BuildBackdropUrl(item.BackdropPath, config.SecureBaseUrl)
        );

        await EnrichWithUserDataAsync(result.Results, ct);

        return result;
    }

    public async Task<PagedMoviesResponseDto> DiscoverMoviesAsync(
        DiscoverMoviesRequestDto request,
        CancellationToken ct = default
    )
    {
        var query = new TmdbDiscoverMovieQuery
        {
            Page = request.Page,
            PrimaryReleaseYear = request.PrimaryReleaseYear,
            PrimaryReleaseDateGte = request.PrimaryReleaseDateGte,
            PrimaryReleaseDateLte = request.PrimaryReleaseDateLte,
            SortBy = request.SortBy ?? "popularity.desc",
            VoteAverageGte = request.VoteAverageGte,
            IncludeAdult = request.IncludeAdult,
            Region = request.Region,
            Language = request.Language,
            WithGenres = request.GenreIds,
        };

        _logger.LogInformation(
            "Discovering movies: sortBy={SortBy}, page={Page}, genres={Genres}",
            query.SortBy,
            query.Page,
            query.WithGenres
        );

        var response = await _tmdb.GetDiscoverMoviesAsync(query, ct);
        var config = await GetImageConfigAsync(ct);

        var result = MovieMapper.ToPagedResult(
            response,
            item => MovieMapper.BuildPosterUrl(item.PosterPath, config.SecureBaseUrl),
            item => MovieMapper.BuildBackdropUrl(item.BackdropPath, config.SecureBaseUrl)
        );

        await EnrichWithUserDataAsync(result.Results, ct);

        return result;
    }

    public async Task<PagedMoviesResponseDto> GetPopularMoviesAsync(
        MovieListRequestDto request,
        CancellationToken ct = default
    )
    {
        return await GetMovieListAsync((q, c) => _tmdb.GetPopularMoviesAsync(q, c), request, ct);
    }

    public async Task<PagedMoviesResponseDto> GetNowPlayingMoviesAsync(
        MovieListRequestDto request,
        CancellationToken ct = default
    )
    {
        return await GetMovieListAsync((q, c) => _tmdb.GetNowPlayingMoviesAsync(q, c), request, ct);
    }

    public async Task<PagedMoviesResponseDto> GetTopRatedMoviesAsync(
        MovieListRequestDto request,
        CancellationToken ct = default
    )
    {
        return await GetMovieListAsync((q, c) => _tmdb.GetTopRatedMoviesAsync(q, c), request, ct);
    }

    public async Task<MovieDetailsResponseDto> GetMovieDetailsAsync(
        int tmdbId,
        string? language,
        CancellationToken ct = default
    )
    {
        _logger.LogInformation("Fetching movie details for TMDB ID {TmdbId}", tmdbId);

        var detailsQuery = new TmdbMovieDetailsQuery
        {
            MovieId = tmdbId,
            AppendToResponse = "credits,videos",
            Language = language,
        };

        var details = await _tmdb.GetMovieDetailsAsync(detailsQuery, ct);
        var config = await GetImageConfigAsync(ct);

        var posterUrl = MovieMapper.BuildPosterUrl(details.PosterPath, config.SecureBaseUrl);
        var backdropUrl = MovieMapper.BuildBackdropUrl(details.BackdropPath, config.SecureBaseUrl);

        var dto = MovieMapper.ToDetails(details, posterUrl, backdropUrl);

        var creditsQuery = new TmdbMovieCreditsQuery { MovieId = tmdbId, Language = language };
        var credits = await _tmdb.GetMovieCreditsAsync(creditsQuery, ct);
        MovieMapper.PopulateCredits(dto, credits, config.SecureBaseUrl);

        var videosQuery = new TmdbMovieVideosQuery { MovieId = tmdbId, Language = language };
        var videos = await _tmdb.GetMovieVideosAsync(videosQuery, ct);
        MovieMapper.PopulateVideos(dto, videos);

        await UpsertMovieAsync(details, ct);

        await EnrichWithUserDataAsync(dto, ct);

        return dto;
    }

    public async Task<PagedMoviesResponseDto> GetMovieRecommendationsAsync(
        int tmdbId,
        int? page,
        string? language,
        CancellationToken ct = default
    )
    {
        _logger.LogInformation(
            "Fetching recommendations for TMDB ID {TmdbId}, page {Page}",
            tmdbId,
            page
        );

        var query = new TmdbMovieRecommendationsQuery
        {
            MovieId = tmdbId,
            Page = page,
            Language = language,
        };

        var response = await _tmdb.GetMovieRecommendationsAsync(query, ct);
        var config = await GetImageConfigAsync(ct);

        var result = MovieMapper.ToPagedResult(
            response,
            item => MovieMapper.BuildPosterUrl(item.PosterPath, config.SecureBaseUrl),
            item => MovieMapper.BuildBackdropUrl(item.BackdropPath, config.SecureBaseUrl)
        );

        await EnrichWithUserDataAsync(result.Results, ct);

        return result;
    }

    public async Task<GenresResponseDto> GetGenresAsync(
        string? language,
        CancellationToken ct = default
    )
    {
        _logger.LogInformation("Fetching movie genres");

        var response = await _tmdb.GetMovieGenresAsync(
            new TmdbGenreListQuery { Language = language },
            ct
        );

        return new GenresResponseDto
        {
            Genres = response
                .Genres.Select(g => new GenreDto { TmdbId = g.Id, Name = g.Name })
                .ToList(),
        };
    }

    private async Task<PagedMoviesResponseDto> GetMovieListAsync(
        Func<
            TmdbMovieListQuery,
            CancellationToken,
            Task<TmdbPagedResponse<TmdbSearchMovieItem>>
        > fetchFunc,
        MovieListRequestDto request,
        CancellationToken ct
    )
    {
        var query = new TmdbMovieListQuery
        {
            Page = request.Page,
            Language = request.Language,
            Region = request.Region,
        };

        var response = await fetchFunc(query, ct);
        var config = await GetImageConfigAsync(ct);

        var result = MovieMapper.ToPagedResult(
            response,
            item => MovieMapper.BuildPosterUrl(item.PosterPath, config.SecureBaseUrl),
            item => MovieMapper.BuildBackdropUrl(item.BackdropPath, config.SecureBaseUrl)
        );

        await EnrichWithUserDataAsync(result.Results, ct);

        return result;
    }

    private async Task<TmdbImageConfig> GetImageConfigAsync(CancellationToken ct)
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

    private async Task UpsertMovieAsync(TmdbMovieDetails details, CancellationToken ct)
    {
        DateOnly? releaseDate = null;
        if (DateOnly.TryParse(details.ReleaseDate, out var parsed))
        {
            releaseDate = parsed;
        }

        var existingMovie = await _db
            .Movies.Include(m => m.MovieGenres)
            .FirstOrDefaultAsync(m => m.TmdbId == details.Id, ct);

        if (existingMovie is null)
        {
            existingMovie = new Movie
            {
                Id = Guid.NewGuid(),
                TmdbId = details.Id,
                Title = details.Title ?? details.OriginalTitle ?? "",
                PosterUrl = details.PosterPath,
                BackdropUrl = details.BackdropPath,
                Overview = details.Overview,
                ReleaseDate = releaseDate,
                Runtime = details.Runtime,
                AverageTmdbRating = details.VoteAverage,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            };

            _db.Movies.Add(existingMovie);
            _logger.LogInformation(
                "Cached new movie {TmdbId} ({Title}) in DB",
                details.Id,
                existingMovie.Title
            );
        }
        else
        {
            existingMovie.Title = details.Title ?? details.OriginalTitle ?? existingMovie.Title;
            existingMovie.PosterUrl = details.PosterPath ?? existingMovie.PosterUrl;
            existingMovie.BackdropUrl = details.BackdropPath ?? existingMovie.BackdropUrl;
            existingMovie.Overview = details.Overview ?? existingMovie.Overview;
            existingMovie.ReleaseDate = releaseDate ?? existingMovie.ReleaseDate;
            existingMovie.Runtime = details.Runtime ?? existingMovie.Runtime;
            existingMovie.AverageTmdbRating = details.VoteAverage;
            existingMovie.UpdatedAt = DateTime.UtcNow;

            _logger.LogDebug(
                "Updated cached movie {TmdbId} ({Title}) in DB",
                details.Id,
                existingMovie.Title
            );
        }

        foreach (var tmdbGenre in details.Genres)
        {
            var genre = await _db.Genres.FirstOrDefaultAsync(g => g.TmdbId == tmdbGenre.Id, ct);

            if (genre is null)
            {
                genre = new Genre
                {
                    Id = Guid.NewGuid(),
                    TmdbId = tmdbGenre.Id,
                    Name = tmdbGenre.Name,
                };
                _db.Genres.Add(genre);
            }

            var alreadyLinked = existingMovie.MovieGenres.Any(mg => mg.GenreId == genre.Id);

            if (!alreadyLinked)
            {
                _db.MovieGenres.Add(
                    new MovieGenre { MovieId = existingMovie.Id, GenreId = genre.Id }
                );
            }
        }

        await _db.SaveChangesAsync(ct);
    }

    private async Task EnrichWithUserDataAsync(List<MovieSummaryDto> results, CancellationToken ct)
    {
        if (results.Count == 0)
            return;

        var tmdbIds = results.Select(r => r.TmdbId).ToList();

        var cachedMovies = await _db.Movies.Where(m => tmdbIds.Contains(m.TmdbId)).ToListAsync(ct);

        var tmdbToGuid = cachedMovies.ToDictionary(m => m.TmdbId, m => m.Id);
        var guidIds = tmdbToGuid.Values.ToList();

        if (_currentUser.IsAuthenticated && guidIds.Count > 0)
        {
            var userMovies = await _db
                .UserMovies.Where(um =>
                    um.UserId == _currentUser.UserId && guidIds.Contains(um.MovieId)
                )
                .ToListAsync(ct);

            var favLookup = userMovies
                .Where(um => um.IsFavorite)
                .Select(um => um.MovieId)
                .ToHashSet();

            var watchlistLookup = userMovies
                .Where(um => um.IsInWatchlist)
                .Select(um => um.MovieId)
                .ToHashSet();

            Dictionary<Guid, int> watchedLookup = [];
            if (_currentUser.CoupleId.HasValue)
            {
                watchedLookup = await _db
                    .WatchSessions.Where(ws =>
                        ws.CoupleId == _currentUser.CoupleId.Value && guidIds.Contains(ws.MovieId)
                    )
                    .GroupBy(ws => ws.MovieId)
                    .Select(g => new { MovieId = g.Key, Count = g.Count() })
                    .ToDictionaryAsync(g => g.MovieId, g => g.Count, ct);
            }

            foreach (var result in results)
            {
                if (tmdbToGuid.TryGetValue(result.TmdbId, out var guidId))
                {
                    result.IsFavorite = favLookup.Contains(guidId);
                    result.IsInWatchlist = watchlistLookup.Contains(guidId);
                    result.WatchedCount = watchedLookup.GetValueOrDefault(guidId, 0);
                }
            }
        }
    }

    private async Task EnrichWithUserDataAsync(MovieDetailsResponseDto dto, CancellationToken ct)
    {
        var movie = await _db.Movies.FirstOrDefaultAsync(m => m.TmdbId == dto.TmdbId, ct);

        if (movie is null || !_currentUser.IsAuthenticated)
            return;

        if (_currentUser.IsAuthenticated)
        {
            var userMovie = await _db.UserMovies.FirstOrDefaultAsync(
                um => um.UserId == _currentUser.UserId && um.MovieId == movie.Id,
                ct
            );

            if (userMovie is not null)
            {
                dto.IsFavorite = userMovie.IsFavorite;
                dto.IsInWatchlist = userMovie.IsInWatchlist;
            }
        }

        if (_currentUser.CoupleId.HasValue)
        {
            dto.WatchedCount = await _db.WatchSessions.CountAsync(
                ws => ws.CoupleId == _currentUser.CoupleId.Value && ws.MovieId == movie.Id,
                ct
            );
        }
    }
}
