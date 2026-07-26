using System.Text.Json.Serialization;

namespace MovieRaterApi.Infrastructure.Tmdb.Dtos.Requests;

public class TmdbDiscoverMovieQuery
{
    [JsonPropertyName("include_adult")]
    public bool? IncludeAdult { get; set; }

    [JsonPropertyName("include_video")]
    public bool? IncludeVideo { get; set; }

    [JsonPropertyName("language")]
    public string? Language { get; set; }

    [JsonPropertyName("page")]
    public int? Page { get; set; }

    [JsonPropertyName("primary_release_year")]
    public string? PrimaryReleaseYear { get; set; }

    [JsonPropertyName("primary_release_date.gte")]
    public string? PrimaryReleaseDateGte { get; set; }

    [JsonPropertyName("primary_release_date.lte")]
    public string? PrimaryReleaseDateLte { get; set; }

    [JsonPropertyName("region")]
    public string? Region { get; set; }

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

    [JsonPropertyName("year")]
    public string? Year { get; set; }
}
