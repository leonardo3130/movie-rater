using MovieRaterApi.Features.Movies.DTOs;
using MovieRaterApi.Features.Movies.Mapping;
using MovieRaterApi.Features.TvShows.DTOs;
using MovieRaterApi.Infrastructure.Tmdb.Dtos.Responses;

namespace MovieRaterApi.Features.TvShows.Mapping;

public static class TvShowMapper
{
    public static TvShowSummaryDto ToSummary(
        TmdbSearchTvItem item,
        string? posterUrl,
        string? backdropUrl
    )
    {
        return new TvShowSummaryDto
        {
            TmdbId = item.Id,
            Title = item.Name ?? item.OriginalName ?? "",
            PosterUrl = posterUrl,
            BackdropUrl = backdropUrl,
            Overview = item.Overview,
            FirstAirDate = item.FirstAirDate,
            VoteAverage = item.VoteAverage,
            VoteCount = item.VoteCount,
            GenreIds = item.GenreIds,
        };
    }

    public static PagedTvShowsResponseDto ToPagedResult(
        TmdbPagedResponse<TmdbSearchTvItem> paged,
        Func<TmdbSearchTvItem, string?> getPosterUrl,
        Func<TmdbSearchTvItem, string?> getBackdropUrl
    )
    {
        return new PagedTvShowsResponseDto
        {
            Page = paged.Page,
            TotalPages = paged.TotalPages,
            TotalResults = paged.TotalResults,
            Results = paged
                .Results.Select(item => ToSummary(item, getPosterUrl(item), getBackdropUrl(item)))
                .ToList(),
        };
    }

    public static TvShowDetailsResponseDto ToDetails(
        TmdbTvShowDetails details,
        string? posterUrl,
        string? backdropUrl,
        string baseUrl
    )
    {
        return new TvShowDetailsResponseDto
        {
            TmdbId = details.Id,
            Name = details.Name ?? details.OriginalName ?? "",
            Overview = details.Overview,
            PosterUrl = posterUrl,
            BackdropUrl = backdropUrl,
            FirstAirDate = details.FirstAirDate,
            LastAirDate = details.LastAirDate,
            NumberOfSeasons = details.NumberOfSeasons,
            NumberOfEpisodes = details.NumberOfEpisodes,
            Status = details.Status,
            Type = details.Type,
            VoteAverage = details.VoteAverage,
            VoteCount = details.VoteCount,
            Genres = details
                .Genres.Select(g => new GenreDto { TmdbId = g.Id, Name = g.Name })
                .ToList(),
            Seasons = details.Seasons.Select(s => ToSeasonSummary(s, baseUrl)).ToList(),
        };
    }

    public static TvSeasonSummaryDto ToSeasonSummary(
        TmdbTvSeasonSummary season,
        string baseUrl
    )
    {
        return new TvSeasonSummaryDto
        {
            TmdbId = season.Id,
            SeasonNumber = season.SeasonNumber,
            EpisodeCount = season.EpisodeCount,
            Overview = season.Overview,
            PosterUrl = MovieMapper.BuildPosterUrl(season.PosterPath, baseUrl),
            AirDate = season.AirDate,
        };
    }

    public static TvSeasonDetailsResponseDto ToSeasonDetails(
        TmdbTvSeasonDetails season,
        string? posterUrl,
        int seriesTmdbId,
        string baseUrl
    )
    {
        return new TvSeasonDetailsResponseDto
        {
            TmdbId = season.Id,
            Name = season.Name ?? $"Season {season.SeasonNumber}",
            SeriesTmdbId = seriesTmdbId,
            SeasonNumber = season.SeasonNumber,
            Overview = season.Overview,
            PosterUrl = posterUrl,
            AirDate = season.AirDate,
            VoteAverage = season.VoteAverage,
            VoteCount = season.VoteCount,
            Episodes = season.Episodes.Select(e => ToEpisodeSummary(e, baseUrl)).ToList(),
        };
    }

    public static TvEpisodeSummaryDto ToEpisodeSummary(
        TmdbTvEpisodeSummary episode,
        string baseUrl
    )
    {
        return new TvEpisodeSummaryDto
        {
            TmdbId = episode.Id,
            EpisodeNumber = episode.EpisodeNumber,
            Name = episode.Name ?? "",
            Overview = episode.Overview,
            StillUrl = MovieMapper.BuildPosterUrl(episode.StillPath, baseUrl, "w300"),
            AirDate = episode.AirDate,
            Runtime = episode.Runtime,
            VoteAverage = episode.VoteAverage,
        };
    }

    public static TvEpisodeDetailsResponseDto ToEpisodeDetails(
        TmdbTvEpisodeDetails episode,
        string? stillUrl,
        int seriesTmdbId,
        string? seriesTitle
    )
    {
        return new TvEpisodeDetailsResponseDto
        {
            TmdbId = episode.Id,
            Name = episode.Name ?? "",
            SeriesTmdbId = seriesTmdbId,
            SeriesTitle = seriesTitle,
            SeasonNumber = episode.SeasonNumber,
            EpisodeNumber = episode.EpisodeNumber,
            Overview = episode.Overview,
            StillUrl = stillUrl,
            AirDate = episode.AirDate,
            Runtime = episode.Runtime,
            VoteAverage = episode.VoteAverage,
        };
    }
}