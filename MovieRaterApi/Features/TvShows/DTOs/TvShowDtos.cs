using Microsoft.AspNetCore.Mvc;
using MovieRaterApi.Data.Entities;
using MovieRaterApi.Features.Movies.DTOs;
using MovieRaterApi.Features.Shared.Interfaces;

namespace MovieRaterApi.Features.TvShows.DTOs;

public class SearchTvShowsRequestDto
{
    public string Query { get; set; } = string.Empty;
    public int? Page { get; set; }
    public string? FirstAirDateYear { get; set; }
    public string? Year { get; set; }
    public bool? IncludeAdult { get; set; }
    public string? Language { get; set; }
}

public class DiscoverTvShowsRequestDto
{
    public int? Page { get; set; }
    public string? GenreIds { get; set; }
    public string? FirstAirDateYear { get; set; }
    public string? FirstAirDateGte { get; set; }
    public string? FirstAirDateLte { get; set; }
    public string? SortBy { get; set; }
    public double? VoteAverageGte { get; set; }
    public bool? IncludeAdult { get; set; }
    public string? Language { get; set; }
}

public class TvShowDetailsRequestDto
{
    [FromRoute]
    public int TmdbId { get; set; }

    public string? Language { get; set; }
}

public class TvSeasonRequestDto
{
    [FromRoute]
    public int TmdbId { get; set; }

    [FromRoute]
    public int SeasonNumber { get; set; }

    public string? Language { get; set; }
}

public class TvEpisodeRequestDto
{
    [FromRoute]
    public int TmdbId { get; set; }

    [FromRoute]
    public int SeasonNumber { get; set; }

    [FromRoute]
    public int EpisodeNumber { get; set; }

    public string? Language { get; set; }
}

public class PagedTvShowsResponseDto
{
    public int Page { get; set; }
    public int TotalPages { get; set; }
    public int TotalResults { get; set; }
    public List<TvShowSummaryDto> Results { get; set; } = [];
}

public class TvShowSummaryDto : IEnrichableMediaDto
{
    public Guid Id { get; set; }
    public int TmdbId { get; set; }
    public MediaType MediaType => MediaType.TvSeries;
    public string Title { get; set; } = string.Empty;
    public string? PosterUrl { get; set; }
    public string? BackdropUrl { get; set; }
    public string? Overview { get; set; }
    public string? FirstAirDate { get; set; }
    public double VoteAverage { get; set; }
    public int VoteCount { get; set; }
    public List<int> GenreIds { get; set; } = [];
    public bool IsFavorite { get; set; }
    public bool IsInWatchlist { get; set; }
    public int WatchedCount { get; set; }
}

public class TvShowDetailsResponseDto : IEnrichableMediaDto
{
    public Guid Id { get; set; }
    public int TmdbId { get; set; }
    public MediaType MediaType => MediaType.TvSeries;
    public string Name { get; set; } = string.Empty;
    public string? Overview { get; set; }
    public string? PosterUrl { get; set; }
    public string? BackdropUrl { get; set; }
    public string? FirstAirDate { get; set; }
    public string? LastAirDate { get; set; }
    public int NumberOfSeasons { get; set; }
    public int NumberOfEpisodes { get; set; }
    public string? Status { get; set; }
    public string? Type { get; set; }
    public double VoteAverage { get; set; }
    public int VoteCount { get; set; }
    public List<GenreDto> Genres { get; set; } = [];
    public List<TvSeasonSummaryDto> Seasons { get; set; } = [];
    public bool IsFavorite { get; set; }
    public bool IsInWatchlist { get; set; }
    public int WatchedCount { get; set; }
}

public class TvSeasonSummaryDto
{
    public int TmdbId { get; set; }
    public int SeasonNumber { get; set; }
    public int EpisodeCount { get; set; }
    public string? Overview { get; set; }
    public string? PosterUrl { get; set; }
    public string? AirDate { get; set; }
}

public class TvSeasonDetailsResponseDto : IEnrichableMediaDto
{
    public Guid Id { get; set; }
    public int TmdbId { get; set; }
    public MediaType MediaType => MediaType.TvSeason;
    public string Name { get; set; } = string.Empty;
    public int SeriesTmdbId { get; set; }
    public int SeasonNumber { get; set; }
    public string? Overview { get; set; }
    public string? PosterUrl { get; set; }
    public string? AirDate { get; set; }
    public double VoteAverage { get; set; }
    public int VoteCount { get; set; }
    public List<TvEpisodeSummaryDto> Episodes { get; set; } = [];
    public bool IsFavorite { get; set; }
    public bool IsInWatchlist { get; set; }
    public int WatchedCount { get; set; }
}

public class TvEpisodeSummaryDto
{
    public int TmdbId { get; set; }
    public int EpisodeNumber { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Overview { get; set; }
    public string? StillUrl { get; set; }
    public string? AirDate { get; set; }
    public int? Runtime { get; set; }
    public double VoteAverage { get; set; }
}

public class TvEpisodeDetailsResponseDto : IEnrichableMediaDto
{
    public Guid Id { get; set; }
    public int TmdbId { get; set; }
    public MediaType MediaType => MediaType.TvEpisode;
    public string Name { get; set; } = string.Empty;
    public int SeriesTmdbId { get; set; }
    public string? SeriesTitle { get; set; }
    public int SeasonNumber { get; set; }
    public int EpisodeNumber { get; set; }
    public string? Overview { get; set; }
    public string? StillUrl { get; set; }
    public string? AirDate { get; set; }
    public int? Runtime { get; set; }
    public double VoteAverage { get; set; }
    public bool IsFavorite { get; set; }
    public bool IsInWatchlist { get; set; }
    public int WatchedCount { get; set; }
}