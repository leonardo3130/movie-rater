namespace MovieRaterApi.Data.Entities;

public class Movie
{
    public Guid Id { get; set; }
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

    public ICollection<MovieGenre> MovieGenres { get; set; } = new List<MovieGenre>();
    public ICollection<WatchSession> WatchSessions { get; set; } = new List<WatchSession>();
    public ICollection<UserMovie> UserMovies { get; set; } = new List<UserMovie>();
}
