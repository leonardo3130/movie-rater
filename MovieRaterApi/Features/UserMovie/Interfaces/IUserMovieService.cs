using MovieRaterApi.Features.UserMovie.DTOs;

namespace MovieRaterApi.Features.UserMovie.Interfaces;

public interface IUserMovieService
{
    Task<UserMovieResponseDto> SetFavoriteAsync(Guid movieId, Guid userId, bool isFavorite);
    Task<UserMovieResponseDto> SetWatchlistAsync(Guid movieId, Guid userId, bool isInWatchlist);
    Task<UserMovieResponseDto> GetAsync(Guid movieId, Guid userId);
    Task<PagedUserMoviesResponseDto> GetUserMoviesAsync(Guid userId, UserMovieListRequestDto request);
}
