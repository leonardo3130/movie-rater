using MovieRaterApi.Features.Movies.DTOs;

namespace MovieRaterApi.Features.Movies.Interfaces;

public interface IMovieService
{
    Task<PagedMoviesResponseDto> SearchMoviesAsync(
        SearchMoviesRequestDto request,
        CancellationToken ct = default
    );

    Task<PagedMoviesResponseDto> DiscoverMoviesAsync(
        DiscoverMoviesRequestDto request,
        CancellationToken ct = default
    );

    Task<PagedMoviesResponseDto> GetPopularMoviesAsync(
        MovieListRequestDto request,
        CancellationToken ct = default
    );

    Task<PagedMoviesResponseDto> GetNowPlayingMoviesAsync(
        MovieListRequestDto request,
        CancellationToken ct = default
    );

    Task<PagedMoviesResponseDto> GetTopRatedMoviesAsync(
        MovieListRequestDto request,
        CancellationToken ct = default
    );

    Task<MovieDetailsResponseDto> GetMovieDetailsAsync(
        int tmdbId,
        string? language,
        CancellationToken ct = default
    );

    Task<PagedMoviesResponseDto> GetMovieRecommendationsAsync(
        int tmdbId,
        int? page,
        string? language,
        CancellationToken ct = default
    );

    Task<GenresResponseDto> GetGenresAsync(string? language, CancellationToken ct = default);
}
