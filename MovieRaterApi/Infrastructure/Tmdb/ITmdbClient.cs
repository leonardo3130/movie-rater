using MovieRaterApi.Infrastructure.Tmdb.Dtos.Requests;
using MovieRaterApi.Infrastructure.Tmdb.Dtos.Responses;

namespace MovieRaterApi.Infrastructure.Tmdb;

public interface ITmdbClient
{
    Task<TmdbPagedResponse<TmdbSearchMovieItem>> SearchMoviesAsync(
        TmdbSearchMovieQuery query,
        CancellationToken ct = default
    );

    Task<TmdbMovieDetails> GetMovieDetailsAsync(
        TmdbMovieDetailsQuery query,
        CancellationToken ct = default
    );

    Task<TmdbCreditsResponse> GetMovieCreditsAsync(
        TmdbMovieCreditsQuery query,
        CancellationToken ct = default
    );

    Task<TmdbVideosResponse> GetMovieVideosAsync(
        TmdbMovieVideosQuery query,
        CancellationToken ct = default
    );

    Task<TmdbPagedResponse<TmdbMovieDetails>> GetMovieRecommendationsAsync(
        TmdbMovieRecommendationsQuery query,
        CancellationToken ct = default
    );

    Task<TmdbGenreListResponse> GetMovieGenresAsync(
        TmdbGenreListQuery? query = null,
        CancellationToken ct = default
    );

    Task<TmdbConfiguration> GetConfigurationAsync(CancellationToken ct = default);

    Task<TmdbPagedResponse<TmdbSearchMovieItem>> GetDiscoverMoviesAsync(
        TmdbDiscoverMovieQuery query,
        CancellationToken ct = default
    );

    Task<TmdbPagedResponse<TmdbSearchMovieItem>> GetPopularMoviesAsync(
        TmdbMovieListQuery query,
        CancellationToken ct = default
    );

    Task<TmdbPagedResponse<TmdbSearchMovieItem>> GetNowPlayingMoviesAsync(
        TmdbMovieListQuery query,
        CancellationToken ct = default
    );

    Task<TmdbPagedResponse<TmdbSearchMovieItem>> GetTopRatedMoviesAsync(
        TmdbMovieListQuery query,
        CancellationToken ct = default
    );
}
