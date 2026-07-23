namespace MovieRaterApi.Data.Entities;

public class AiSummary
{
    public Guid Id { get; set; }
    public Guid WatchSessionId { get; set; }
    public string Summary { get; set; } = string.Empty;
    public DateTime GeneratedAt { get; set; }

    public WatchSession WatchSession { get; set; } = null!;
}
