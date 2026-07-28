namespace MovieRaterApi.Features.UserMovie.DTOs;

public class UserMovieResponseDto
{
    public Guid UserId { get; set; }
    public Guid MovieId { get; set; }
    public bool IsFavorite { get; set; }
    public bool IsInWatchlist { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
