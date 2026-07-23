namespace MovieRaterApi.Data.Entities;

public class Genre
{
    public Guid Id { get; set; }
    public int TmdbId { get; set; }
    public string Name { get; set; } = string.Empty;

    public ICollection<MovieGenre> MovieGenres { get; set; } = new List<MovieGenre>();
}
