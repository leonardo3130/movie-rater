using System.Text.Json.Serialization;

namespace MovieRaterApi.Infrastructure.Tmdb.Dtos.Requests;

public class TmdbTvDetailsQuery
{
    [JsonIgnore]
    public int SeriesId { get; set; }

    [JsonPropertyName("append_to_response")]
    public string? AppendToResponse { get; set; }

    [JsonPropertyName("language")]
    public string? Language { get; set; }
}