using System.Text.Json.Serialization;

namespace MovieRaterApi.Infrastructure.Tmdb.Dtos.Requests;

public class TmdbMovieListQuery
{
    [JsonPropertyName("language")]
    public string? Language { get; set; }

    [JsonPropertyName("page")]
    public int? Page { get; set; }

    [JsonPropertyName("region")]
    public string? Region { get; set; }
}