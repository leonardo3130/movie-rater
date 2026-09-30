namespace MovieRaterApi.Data.Entities;

public class TvEpisode : Media
{
    public int EpisodeNumber { get; set; }
    public int SeasonNumber { get; set; }
    public Guid SeasonId { get; set; }

    public TvSeason Season { get; set; } = null!;
}