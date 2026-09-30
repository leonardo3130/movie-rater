namespace MovieRaterApi.Data.Entities;

public class WatchSession
{
    public Guid Id { get; set; }
    public Guid? GroupId { get; set; }
    public Guid MediaId { get; set; }
    public DateTime WatchedAt { get; set; }
    public string? Location { get; set; }
    public string? Notes { get; set; }
    public Guid CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Group? Group { get; set; } = null;
    public Media Media { get; set; } = null!;
    public User CreatedByUser { get; set; } = null!;
    public ICollection<Rating> Ratings { get; set; } = new List<Rating>();
    public AiSummary? AiSummary { get; set; }
}