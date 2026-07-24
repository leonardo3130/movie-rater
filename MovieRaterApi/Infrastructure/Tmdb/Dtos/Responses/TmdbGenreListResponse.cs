using System.Text.Json.Serialization;

namespace MovieRaterApi.Infrastructure.Tmdb.Dtos.Responses;

public class TmdbGenreListResponse
{
    [JsonPropertyName("genres")]
    public List<TmdbGenre> Genres { get; set; } = [];
}