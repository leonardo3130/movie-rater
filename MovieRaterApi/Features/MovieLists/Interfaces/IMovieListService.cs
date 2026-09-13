using MovieRaterApi.Features.MovieLists.DTOs;

namespace MovieRaterApi.Features.MovieLists.Interfaces;

public interface IMovieListService
{
    Task<MovieListResponseDto> CreateAsync(CreateMovieListRequestDto request, Guid userId);
    Task<MovieListResponseDto> UpdateAsync(
        Guid listId,
        UpdateMovieListRequestDto request,
        Guid userId
    );
    Task<MovieListResponseDto> GetAsync(Guid listId, Guid userId);
    Task<List<MovieListSummaryDto>> GetAllAsync(Guid userId);
    Task DeleteAsync(Guid listId, Guid userId);
    Task AddMovieAsync(Guid listId, Guid movieId, Guid userId);
    Task RemoveMovieAsync(Guid listId, Guid movieId, Guid userId);
}
