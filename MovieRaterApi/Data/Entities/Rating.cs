namespace MovieRaterApi.Data.Entities;

public class Rating
{
    public Guid Id { get; set; }
    public Guid WatchSessionId { get; set; }
    public Guid UserId { get; set; }
    public int RatingValue { get; set; }
    public string? Review { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public WatchSession WatchSession { get; set; } = null!;
    public User User { get; set; } = null!;
}
