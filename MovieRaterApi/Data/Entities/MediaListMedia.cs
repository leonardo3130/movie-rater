namespace MovieRaterApi.Data.Entities;

public class MediaListMedia
{
    public Guid MediaListId { get; set; }
    public Guid MediaId { get; set; }
    public DateTime CreatedAt { get; set; }

    public MovieList MediaList { get; set; } = null!;
    public Media Media { get; set; } = null!;
}