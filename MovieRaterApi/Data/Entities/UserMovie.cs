namespace MovieRaterApi.Data.Entities;

public class UserMovie
{
    public Guid UserId { get; set; }
    public Guid MovieId { get; set; }
    public bool IsFavorite { get; set; }
    public bool IsInWatchlist { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public User User { get; set; } = null!;
    public Movie Movie { get; set; } = null!;
}
