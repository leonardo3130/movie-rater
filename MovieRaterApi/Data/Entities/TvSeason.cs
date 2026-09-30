namespace MovieRaterApi.Data.Entities;

public class TvSeason : Media
{
    public int SeasonNumber { get; set; }
    public Guid SeriesId { get; set; }

    public TvSeries Series { get; set; } = null!;
    public ICollection<TvEpisode> Episodes { get; set; } = new List<TvEpisode>();
}