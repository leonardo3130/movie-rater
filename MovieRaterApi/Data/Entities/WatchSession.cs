namespace MovieRaterApi.Data.Entities;

public class WatchSession
{
    public Guid Id { get; set; }
    public Guid CoupleId { get; set; }
    public Guid MovieId { get; set; }
    public DateTime WatchedAt { get; set; }
    public string? Location { get; set; }
    public string? Notes { get; set; }
    public Guid CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Couple Couple { get; set; } = null!;
    public Movie Movie { get; set; } = null!;
    public User CreatedByUser { get; set; } = null!;
    public ICollection<Rating> Ratings { get; set; } = new List<Rating>();
    public AiSummary? AiSummary { get; set; }
}
