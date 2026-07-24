using System.Text.Json.Serialization;

namespace MovieRaterApi.Infrastructure.Tmdb.Dtos.Responses;

public class TmdbConfiguration
{
    [JsonPropertyName("images")]
    public TmdbImageConfig Images { get; set; } = new();

    [JsonPropertyName("change_keys")]
    public List<string>? ChangeKeys { get; set; }
}

public class TmdbImageConfig
{
    [JsonPropertyName("base_url")]
    public string BaseUrl { get; set; } = string.Empty;

    [JsonPropertyName("secure_base_url")]
    public string SecureBaseUrl { get; set; } = string.Empty;

    [JsonPropertyName("backdrop_sizes")]
    public List<string> BackdropSizes { get; set; } = [];

    [JsonPropertyName("logo_sizes")]
    public List<string> LogoSizes { get; set; } = [];

    [JsonPropertyName("poster_sizes")]
    public List<string> PosterSizes { get; set; } = [];

    [JsonPropertyName("profile_sizes")]
    public List<string> ProfileSizes { get; set; } = [];

    [JsonPropertyName("still_sizes")]
    public List<string> StillSizes { get; set; } = [];
}