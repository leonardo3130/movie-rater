using System.Text.Json.Serialization;

namespace MovieRaterApi.Infrastructure.Tmdb.Dtos.Requests;

public class TmdbDiscoverTvQuery
{
    [JsonPropertyName("first_air_date_year")]
    public string? FirstAirDateYear { get; set; }

    [JsonPropertyName("first_air_date.gte")]
    public string? FirstAirDateGte { get; set; }

    [JsonPropertyName("first_air_date.lte")]
    public string? FirstAirDateLte { get; set; }

    [JsonPropertyName("include_adult")]
    public bool? IncludeAdult { get; set; }

    [JsonPropertyName("language")]
    public string? Language { get; set; }

    [JsonPropertyName("page")]
    public int? Page { get; set; }

    [JsonPropertyName("sort_by")]
    public string? SortBy { get; set; }

    [JsonPropertyName("vote_average.gte")]
    public double? VoteAverageGte { get; set; }

    [JsonPropertyName("vote_average.lte")]
    public double? VoteAverageLte { get; set; }

    [JsonPropertyName("vote_count.gte")]
    public int? VoteCountGte { get; set; }

    [JsonPropertyName("with_genres")]
    public string? WithGenres { get; set; }

    [JsonPropertyName("without_genres")]
    public string? WithoutGenres { get; set; }
}