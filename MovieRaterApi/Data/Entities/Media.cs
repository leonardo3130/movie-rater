namespace MovieRaterApi.Data.Entities;

public abstract class Media
{
    public Guid Id { get; set; }
    public MediaType MediaType { get; set; }
    public int TmdbId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? PosterUrl { get; set; }
    public string? BackdropUrl { get; set; }
    public string? Overview { get; set; }
    public DateOnly? ReleaseDate { get; set; }
    public int? Runtime { get; set; }
    public double AverageTmdbRating { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ICollection<MediaGenre> MediaGenres { get; set; } = new List<MediaGenre>();
    public ICollection<WatchSession> WatchSessions { get; set; } = new List<WatchSession>();
    public ICollection<UserMedia> UserMedias { get; set; } = new List<UserMedia>();
    public ICollection<MediaListMedia> MediaListMedias { get; set; } = new List<MediaListMedia>();
}