namespace MovieRaterApi.Data.Entities;

public class User
{
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string? ProfilePictureUrl { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ICollection<Rating> Ratings { get; set; } = new List<Rating>();
    public ICollection<UserMovie> UserMovies { get; set; } = new List<UserMovie>();
    public ICollection<UserGroup> UserGroups { get; set; } = new List<UserGroup>();
    public ICollection<UserAchievement> UserAchievements { get; set; } =
        new List<UserAchievement>();
    public ICollection<WatchSession> CreatedWatchSessions { get; set; } = new List<WatchSession>();
}
