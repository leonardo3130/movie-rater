namespace MovieRaterApi.Features.Ratings.DTOs;

public class CreateRatingRequestDto
{
    public int RatingValue { get; set; }
    public string? Review { get; set; }
}

public class UpdateRatingRequestDto
{
    public int RatingValue { get; set; }
    public string? Review { get; set; }
}

public class RatingResponseDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public int RatingValue { get; set; }
    public string? Review { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class SessionRatingsResponseDto
{
    public Guid WatchSessionId { get; set; }
    public List<RatingResponseDto> Ratings { get; set; } = [];
}
