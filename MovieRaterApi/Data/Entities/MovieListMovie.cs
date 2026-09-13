namespace MovieRaterApi.Data.Entities;

public class MovieListMovie
{
    public Guid MovieListId { get; set; }
    public Guid MovieId { get; set; }
    public DateTime CreatedAt { get; set; }

    public MovieList MovieList { get; set; } = null!;
    public Movie Movie { get; set; } = null!;
}
