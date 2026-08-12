namespace MovieRaterApi.Data.Entities;

public class Group
{
    public Guid Id { get; set; }
    public string Name { get; set; } = String.Empty;
    public DateTime CreatedAt { get; set; }

    public ICollection<UserGroup> UserGroups { get; set; } = new List<UserGroup>();
    public ICollection<WatchSession> WatchSessions { get; set; } = new List<WatchSession>();
}
