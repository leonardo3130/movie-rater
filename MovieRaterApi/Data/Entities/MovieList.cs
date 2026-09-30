namespace MovieRaterApi.Data.Entities;

public class MovieList
{
    public Guid Id { get; set; }
    public Guid OwnerUserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsPrivate { get; set; } = true;
    public bool CanGroupEdit { get; set; } = false;
    public DateTime CreatedAt { get; set; }
    public DateTime LastUpdatedAt { get; set; }

    public User OwnerUser { get; set; } = null!;
    public ICollection<MediaListMedia> MediaItems { get; set; } = new List<MediaListMedia>();
    public ICollection<MovieListGroup> SharedGroups { get; set; } = new List<MovieListGroup>();
}
