namespace MovieRaterApi.Data.Entities;

public class MovieListGroup
{
    public Guid MovieListId { get; set; }
    public Guid GroupId { get; set; }

    public MovieList MovieList { get; set; } = null!;
    public Group Group { get; set; } = null!;
}
