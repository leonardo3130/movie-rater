using System.Text.Json.Serialization;

namespace MovieRaterApi.Infrastructure.Tmdb.Dtos.Requests;

public class TmdbGenreListQuery
{
    [JsonPropertyName("language")]
    public string? Language { get; set; }
}