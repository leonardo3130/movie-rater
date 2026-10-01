using System.Text.Json.Serialization;

namespace MovieRaterApi.Infrastructure.Tmdb.Dtos.Requests;

public class TmdbSearchTvQuery
{
    [JsonPropertyName("query")]
    public string Query { get; set; } = string.Empty;

    [JsonPropertyName("first_air_date_year")]
    public string? FirstAirDateYear { get; set; }

    [JsonPropertyName("include_adult")]
    public bool? IncludeAdult { get; set; }

    [JsonPropertyName("language")]
    public string? Language { get; set; }

    [JsonPropertyName("page")]
    public int? Page { get; set; }

    [JsonPropertyName("year")]
    public string? Year { get; set; }
}