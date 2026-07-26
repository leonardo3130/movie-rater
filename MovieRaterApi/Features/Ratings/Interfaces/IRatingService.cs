using MovieRaterApi.Features.Ratings.DTOs;

namespace MovieRaterApi.Features.Ratings.Interfaces;

public interface IRatingService
{
    Task<RatingResponseDto> CreateAsync(
        Guid watchSessionId,
        CreateRatingRequestDto request,
        Guid userId
    );
    Task<RatingResponseDto> UpdateAsync(
        Guid watchSessionId,
        UpdateRatingRequestDto request,
        Guid userId
    );
    Task<SessionRatingsResponseDto> GetBySessionAsync(Guid watchSessionId, Guid coupleId);
}
