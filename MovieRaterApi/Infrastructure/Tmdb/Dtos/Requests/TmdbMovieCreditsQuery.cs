using System.Text.Json.Serialization;

namespace MovieRaterApi.Infrastructure.Tmdb.Dtos.Requests;

public class TmdbMovieCreditsQuery
{
    [JsonIgnore]
    public int MovieId { get; set; }

    [JsonPropertyName("language")]
    public string? Language { get; set; }
}