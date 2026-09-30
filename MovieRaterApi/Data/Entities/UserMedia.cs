namespace MovieRaterApi.Data.Entities;

public class UserMedia
{
    public Guid UserId { get; set; }
    public Guid MediaId { get; set; }
    public bool IsFavorite { get; set; }
    public bool IsInWatchlist { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public User User { get; set; } = null!;
    public Media Media { get; set; } = null!;
}