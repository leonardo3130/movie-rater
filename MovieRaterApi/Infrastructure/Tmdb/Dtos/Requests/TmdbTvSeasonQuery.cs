using System.Text.Json.Serialization;

namespace MovieRaterApi.Infrastructure.Tmdb.Dtos.Requests;

public class TmdbTvSeasonQuery
{
    [JsonIgnore]
    public int SeriesId { get; set; }

    [JsonIgnore]
    public int SeasonNumber { get; set; }

    [JsonPropertyName("language")]
    public string? Language { get; set; }
}