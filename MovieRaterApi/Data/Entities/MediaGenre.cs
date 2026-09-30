namespace MovieRaterApi.Data.Entities;

public class MediaGenre
{
    public Guid MediaId { get; set; }
    public Guid GenreId { get; set; }

    public Media Media { get; set; } = null!;
    public Genre Genre { get; set; } = null!;
}