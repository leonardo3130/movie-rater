namespace MovieRaterApi.Data.Entities;

public class TvSeries : Media
{
    public int? NumberOfSeasons { get; set; }
    public int? NumberOfEpisodes { get; set; }
    public DateOnly? LastAirDate { get; set; }
    public string? Status { get; set; }
    public string? Type { get; set; }

    public ICollection<TvSeason> Seasons { get; set; } = new List<TvSeason>();
}