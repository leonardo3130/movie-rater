using System.Text.Json.Serialization;

namespace MovieRaterApi.Infrastructure.Tmdb.Dtos.Requests;

public class TmdbTvEpisodeQuery
{
    [JsonIgnore]
    public int SeriesId { get; set; }

    [JsonIgnore]
    public int SeasonNumber { get; set; }

    [JsonIgnore]
    public int EpisodeNumber { get; set; }

    [JsonPropertyName("language")]
    public string? Language { get; set; }
}