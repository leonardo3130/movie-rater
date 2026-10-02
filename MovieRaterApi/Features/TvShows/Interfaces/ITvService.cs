using MovieRaterApi.Features.Movies.DTOs;
using MovieRaterApi.Features.TvShows.DTOs;

namespace MovieRaterApi.Features.TvShows.Interfaces;

public interface ITvService
{
    Task<PagedTvShowsResponseDto> SearchTvShowsAsync(
        SearchTvShowsRequestDto request,
        CancellationToken ct = default
    );

    Task<TvShowDetailsResponseDto> GetTvShowDetailsAsync(
        int tmdbId,
        string? language,
        CancellationToken ct = default
    );

    Task<TvSeasonDetailsResponseDto> GetTvSeasonAsync(
        int tmdbId,
        int seasonNumber,
        string? language,
        CancellationToken ct = default
    );

    Task<TvEpisodeDetailsResponseDto> GetTvEpisodeAsync(
        int tmdbId,
        int seasonNumber,
        int episodeNumber,
        string? language,
        CancellationToken ct = default
    );

    Task<GenresResponseDto> GetGenresAsync(string? language, CancellationToken ct = default);
}