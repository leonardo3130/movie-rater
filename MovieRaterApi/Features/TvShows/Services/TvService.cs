using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using MovieRaterApi.Data;
using MovieRaterApi.Data.Entities;
using MovieRaterApi.Features.Movies.DTOs;
using MovieRaterApi.Features.Movies.Mapping;
using MovieRaterApi.Features.Shared.Interfaces;
using MovieRaterApi.Features.TvShows.DTOs;
using MovieRaterApi.Features.TvShows.Interfaces;
using MovieRaterApi.Features.TvShows.Mapping;
using MovieRaterApi.Infrastructure.Exceptions;
using MovieRaterApi.Infrastructure.Tmdb;
using MovieRaterApi.Infrastructure.Tmdb.Dtos.Requests;
using MovieRaterApi.Infrastructure.Tmdb.Dtos.Responses;

namespace MovieRaterApi.Features.TvShows.Services;

public class TvService : ITvService
{
    private static readonly string ImageConfigCacheKey = "TmdbImageConfig";

    private readonly ITmdbClient _tmdb;
    private readonly ApplicationDbContext _db;
    private readonly IMemoryCache _cache;
    private readonly ILogger<TvService> _logger;
    private readonly IMediaEnrichmentService _mediaEnrichment;

    public TvService(
        ITmdbClient tmdb,
        ApplicationDbContext db,
        IMemoryCache cache,
        ILogger<TvService> logger,
        IMediaEnrichmentService mediaEnrichment
    )
    {
        _tmdb = tmdb;
        _db = db;
        _cache = cache;
        _logger = logger;
        _mediaEnrichment = mediaEnrichment;
    }

    public async Task<PagedTvShowsResponseDto> SearchTvShowsAsync(
        SearchTvShowsRequestDto request,
        CancellationToken ct = default
    )
    {
        var query = new TmdbSearchTvQuery
        {
            Query = request.Query,
            Page = request.Page,
            FirstAirDateYear = request.FirstAirDateYear,
            Year = request.Year,
            IncludeAdult = request.IncludeAdult,
            Language = request.Language,
        };

        _logger.LogInformation(
            "Searching TV shows: query={Query}, page={Page}",
            request.Query,
            request.Page
        );

        var response = await _tmdb.SearchTvShowsAsync(query, ct);
        var config = await GetImageConfigAsync(ct);

        var result = TvShowMapper.ToPagedResult(
            response,
            item => MovieMapper.BuildPosterUrl(item.PosterPath, config.SecureBaseUrl),
            item => MovieMapper.BuildBackdropUrl(item.BackdropPath, config.SecureBaseUrl)
        );

        await _mediaEnrichment.EnrichAsync(result.Results, ct);

        return result;
    }

    public async Task<TvShowDetailsResponseDto> GetTvShowDetailsAsync(
        int tmdbId,
        string? language,
        CancellationToken ct = default
    )
    {
        _logger.LogInformation("Fetching TV show details for TMDB ID {TmdbId}", tmdbId);

        var detailsQuery = new TmdbTvDetailsQuery
        {
            SeriesId = tmdbId,
            Language = language,
        };

        var details = await _tmdb.GetTvShowDetailsAsync(detailsQuery, ct);
        var config = await GetImageConfigAsync(ct);

        var posterUrl = MovieMapper.BuildPosterUrl(details.PosterPath, config.SecureBaseUrl);
        var backdropUrl = MovieMapper.BuildBackdropUrl(details.BackdropPath, config.SecureBaseUrl);

        var dto = TvShowMapper.ToDetails(details, posterUrl, backdropUrl, config.SecureBaseUrl);

        await UpsertSeriesAsync(details, posterUrl, backdropUrl, ct);

        await _mediaEnrichment.EnrichAsync(dto, ct);

        return dto;
    }

    public async Task<TvSeasonDetailsResponseDto> GetTvSeasonAsync(
        int tmdbId,
        int seasonNumber,
        string? language,
        CancellationToken ct = default
    )
    {
        _logger.LogInformation(
            "Fetching TV season {SeasonNumber} for TMDB series {SeriesId}",
            seasonNumber,
            tmdbId
        );

        var series = await _db.TvSeries.FirstOrDefaultAsync(s => s.TmdbId == tmdbId, ct);
        if (series is null)
            throw new NotFoundException("TV series not found. Fetch the series details first.");

        var query = new TmdbTvSeasonQuery
        {
            SeriesId = tmdbId,
            SeasonNumber = seasonNumber,
            Language = language,
        };

        var season = await _tmdb.GetTvSeasonAsync(query, ct);
        var config = await GetImageConfigAsync(ct);

        var posterUrl = MovieMapper.BuildPosterUrl(season.PosterPath, config.SecureBaseUrl);

        var dto = TvShowMapper.ToSeasonDetails(season, posterUrl, tmdbId, config.SecureBaseUrl);

        await UpsertSeasonAsync(series, season, config.SecureBaseUrl, ct);

        await _mediaEnrichment.EnrichAsync(dto, ct);

        return dto;
    }

    public async Task<TvEpisodeDetailsResponseDto> GetTvEpisodeAsync(
        int tmdbId,
        int seasonNumber,
        int episodeNumber,
        string? language,
        CancellationToken ct = default
    )
    {
        _logger.LogInformation(
            "Fetching TV episode {EpisodeNumber} of season {SeasonNumber} for TMDB series {SeriesId}",
            episodeNumber,
            seasonNumber,
            tmdbId
        );

        var series = await _db.TvSeries.FirstOrDefaultAsync(s => s.TmdbId == tmdbId, ct);
        if (series is null)
            throw new NotFoundException("TV series not found. Fetch the series details first.");

        var seasonEntity = await _db
            .TvSeasons.FirstOrDefaultAsync(
                s => s.SeriesId == series.Id && s.SeasonNumber == seasonNumber,
                ct
            );
        if (seasonEntity is null)
            throw new NotFoundException("TV season not found. Fetch the season details first.");

        var query = new TmdbTvEpisodeQuery
        {
            SeriesId = tmdbId,
            SeasonNumber = seasonNumber,
            EpisodeNumber = episodeNumber,
            Language = language,
        };

        var episode = await _tmdb.GetTvEpisodeAsync(query, ct);
        var config = await GetImageConfigAsync(ct);

        var stillUrl = MovieMapper.BuildPosterUrl(episode.StillPath, config.SecureBaseUrl, "w300");

        var dto = TvShowMapper.ToEpisodeDetails(episode, stillUrl, tmdbId, series.Title);

        await UpsertEpisodeAsync(
            seasonEntity,
            episode.Id,
            episode.EpisodeNumber,
            episode.Name,
            episode.Overview,
            episode.AirDate,
            episode.Runtime,
            episode.VoteAverage,
            stillUrl,
            ct
        );

        await _db.SaveChangesAsync(ct);

        await _mediaEnrichment.EnrichAsync(dto, ct);

        return dto;
    }

    public async Task<GenresResponseDto> GetGenresAsync(
        string? language,
        CancellationToken ct = default
    )
    {
        _logger.LogInformation("Fetching TV genres");

        var response = await _tmdb.GetTvGenresAsync(
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

    private async Task UpsertSeriesAsync(
        TmdbTvShowDetails details,
        string? posterUrl,
        string? backdropUrl,
        CancellationToken ct
    )
    {
        var releaseDate = ParseDate(details.FirstAirDate);
        var lastAirDate = ParseDate(details.LastAirDate);

        var existing = await _db
            .TvSeries.Include(s => s.MediaGenres)
            .FirstOrDefaultAsync(s => s.TmdbId == details.Id, ct);

        if (existing is null)
        {
            existing = new TvSeries
            {
                Id = Guid.NewGuid(),
                TmdbId = details.Id,
                Title = details.Name ?? details.OriginalName ?? "",
                PosterUrl = posterUrl,
                BackdropUrl = backdropUrl,
                Overview = details.Overview,
                ReleaseDate = releaseDate,
                LastAirDate = lastAirDate,
                NumberOfSeasons = details.NumberOfSeasons,
                NumberOfEpisodes = details.NumberOfEpisodes,
                Status = details.Status,
                Type = details.Type,
                AverageTmdbRating = details.VoteAverage,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            };

            _db.TvSeries.Add(existing);
            _logger.LogInformation(
                "Cached new TV series {TmdbId} ({Title}) in DB",
                details.Id,
                existing.Title
            );
        }
        else
        {
            existing.Title = details.Name ?? details.OriginalName ?? existing.Title;
            existing.PosterUrl = posterUrl ?? existing.PosterUrl;
            existing.BackdropUrl = backdropUrl ?? existing.BackdropUrl;
            existing.Overview = details.Overview ?? existing.Overview;
            existing.ReleaseDate = releaseDate ?? existing.ReleaseDate;
            existing.LastAirDate = lastAirDate ?? existing.LastAirDate;
            existing.NumberOfSeasons =
                details.NumberOfSeasons > 0 ? details.NumberOfSeasons : existing.NumberOfSeasons;
            existing.NumberOfEpisodes =
                details.NumberOfEpisodes > 0
                    ? details.NumberOfEpisodes
                    : existing.NumberOfEpisodes;
            existing.Status = details.Status ?? existing.Status;
            existing.Type = details.Type ?? existing.Type;
            existing.AverageTmdbRating = details.VoteAverage;
            existing.UpdatedAt = DateTime.UtcNow;

            _logger.LogDebug(
                "Updated cached TV series {TmdbId} ({Title}) in DB",
                details.Id,
                existing.Title
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

            var alreadyLinked = existing.MediaGenres.Any(mg => mg.GenreId == genre.Id);

            if (!alreadyLinked)
            {
                _db.MediaGenres.Add(
                    new MediaGenre { MediaId = existing.Id, GenreId = genre.Id }
                );
            }
        }

        await _db.SaveChangesAsync(ct);
    }

    private async Task UpsertSeasonAsync(
        TvSeries series,
        TmdbTvSeasonDetails season,
        string baseUrl,
        CancellationToken ct
    )
    {
        var posterUrl = MovieMapper.BuildPosterUrl(season.PosterPath, baseUrl);
        var airDate = ParseDate(season.AirDate);

        var existing = await _db.TvSeasons.FirstOrDefaultAsync(
            s => s.SeriesId == series.Id && s.SeasonNumber == season.SeasonNumber,
            ct
        );

        if (existing is null)
        {
            existing = new TvSeason
            {
                Id = Guid.NewGuid(),
                TmdbId = season.Id,
                SeasonNumber = season.SeasonNumber,
                SeriesId = series.Id,
                Title = $"Season {season.SeasonNumber}",
                Overview = season.Overview,
                PosterUrl = posterUrl,
                ReleaseDate = airDate,
                AverageTmdbRating = season.VoteAverage,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            };

            _db.TvSeasons.Add(existing);
            _logger.LogInformation(
                "Cached new TV season {SeasonNumber} for series {SeriesId}",
                season.SeasonNumber,
                series.Id
            );
        }
        else
        {
            existing.TmdbId = season.Id;
            existing.Overview = season.Overview ?? existing.Overview;
            existing.PosterUrl = posterUrl ?? existing.PosterUrl;
            existing.ReleaseDate = airDate ?? existing.ReleaseDate;
            existing.AverageTmdbRating = season.VoteAverage;
            existing.UpdatedAt = DateTime.UtcNow;

            _logger.LogDebug(
                "Updated cached TV season {SeasonNumber} for series {SeriesId}",
                season.SeasonNumber,
                series.Id
            );
        }

        foreach (var episode in season.Episodes)
        {
            await UpsertEpisodeAsync(
                existing,
                episode.Id,
                episode.EpisodeNumber,
                episode.Name,
                episode.Overview,
                episode.AirDate,
                episode.Runtime,
                episode.VoteAverage,
                MovieMapper.BuildPosterUrl(episode.StillPath, baseUrl, "w300"),
                ct
            );
        }

        await _db.SaveChangesAsync(ct);
    }

    private async Task UpsertEpisodeAsync(
        TvSeason season,
        int tmdbId,
        int episodeNumber,
        string? name,
        string? overview,
        string? airDate,
        int? runtime,
        double voteAverage,
        string? stillUrl,
        CancellationToken ct
    )
    {
        var existing = await _db.TvEpisodes.FirstOrDefaultAsync(
            e => e.SeasonId == season.Id && e.EpisodeNumber == episodeNumber,
            ct
        );

        if (existing is null)
        {
            existing = new TvEpisode
            {
                Id = Guid.NewGuid(),
                TmdbId = tmdbId,
                EpisodeNumber = episodeNumber,
                SeasonNumber = season.SeasonNumber,
                SeasonId = season.Id,
                Title = name ?? "",
                Overview = overview,
                PosterUrl = stillUrl,
                ReleaseDate = ParseDate(airDate),
                Runtime = runtime,
                AverageTmdbRating = voteAverage,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            };

            _db.TvEpisodes.Add(existing);
            _logger.LogDebug(
                "Cached new TV episode {EpisodeNumber} in season {SeasonNumber} of series {SeriesId}",
                episodeNumber,
                season.SeasonNumber,
                season.SeriesId
            );
        }
        else
        {
            existing.TmdbId = tmdbId;
            existing.Title = name ?? existing.Title;
            existing.Overview = overview ?? existing.Overview;
            existing.PosterUrl = stillUrl ?? existing.PosterUrl;
            existing.ReleaseDate = ParseDate(airDate) ?? existing.ReleaseDate;
            existing.Runtime = runtime ?? existing.Runtime;
            existing.AverageTmdbRating = voteAverage;
            existing.UpdatedAt = DateTime.UtcNow;

            _logger.LogDebug(
                "Updated cached TV episode {EpisodeNumber} in season {SeasonNumber} of series {SeriesId}",
                episodeNumber,
                season.SeasonNumber,
                season.SeriesId
            );
        }
    }

    private static DateOnly? ParseDate(string? value)
    {
        return DateOnly.TryParse(value, out var parsed) ? parsed : null;
    }
}