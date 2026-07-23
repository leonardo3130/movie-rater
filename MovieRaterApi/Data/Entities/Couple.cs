namespace MovieRaterApi.Data.Entities;

public class Couple
{
    public Guid Id { get; set; }
    public Guid User1Id { get; set; }
    public Guid User2Id { get; set; }
    public DateTime CreatedAt { get; set; }

    public User User1 { get; set; } = null!;
    public User User2 { get; set; } = null!;
    public ICollection<WatchSession> WatchSessions { get; set; } = new List<WatchSession>();
}
