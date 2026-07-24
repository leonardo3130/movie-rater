using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Microsoft.Extensions.Options;
using MovieRaterApi.Infrastructure.Tmdb.Dtos.Requests;
using MovieRaterApi.Infrastructure.Tmdb.Dtos.Responses;
using MovieRaterApi.Infrastructure.Tmdb.Exceptions;
using MovieRaterApi.Infrastructure.Tmdb.Options;

namespace MovieRaterApi.Infrastructure.Tmdb;

public class TmdbClient : ITmdbClient
{
    private readonly HttpClient _httpClient;
    private readonly TmdbOptions _options;
    private readonly ILogger<TmdbClient> _logger;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public TmdbClient(
        HttpClient httpClient,
        IOptions<TmdbOptions> options,
        ILogger<TmdbClient> logger
    )
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<TmdbPagedResponse<TmdbSearchMovieItem>> SearchMoviesAsync(
        TmdbSearchMovieQuery query,
        CancellationToken ct = default
    )
    {
        ApplyDefaultLanguage(query);
        var url = $"search/movie{BuildQueryString(query)}";
        return await SendAsync<TmdbPagedResponse<TmdbSearchMovieItem>>(url, ct);
    }

    public async Task<TmdbMovieDetails> GetMovieDetailsAsync(
        TmdbMovieDetailsQuery query,
        CancellationToken ct = default
    )
    {
        ApplyDefaultLanguage(query);
        var url = $"movie/{query.MovieId}{BuildQueryString(query)}";
        return await SendAsync<TmdbMovieDetails>(url, ct);
    }

    public async Task<TmdbCreditsResponse> GetMovieCreditsAsync(
        TmdbMovieCreditsQuery query,
        CancellationToken ct = default
    )
    {
        ApplyDefaultLanguage(query);
        var url = $"movie/{query.MovieId}/credits{BuildQueryString(query)}";
        return await SendAsync<TmdbCreditsResponse>(url, ct);
    }

    public async Task<TmdbVideosResponse> GetMovieVideosAsync(
        TmdbMovieVideosQuery query,
        CancellationToken ct = default
    )
    {
        ApplyDefaultLanguage(query);
        var url = $"movie/{query.MovieId}/videos{BuildQueryString(query)}";
        return await SendAsync<TmdbVideosResponse>(url, ct);
    }

    public async Task<TmdbPagedResponse<TmdbMovieDetails>> GetMovieRecommendationsAsync(
        TmdbMovieRecommendationsQuery query,
        CancellationToken ct = default
    )
    {
        ApplyDefaultLanguage(query);
        var url = $"movie/{query.MovieId}/recommendations{BuildQueryString(query)}";
        return await SendAsync<TmdbPagedResponse<TmdbMovieDetails>>(url, ct);
    }

    public async Task<TmdbGenreListResponse> GetMovieGenresAsync(
        TmdbGenreListQuery? query = null,
        CancellationToken ct = default
    )
    {
        query ??= new TmdbGenreListQuery();
        ApplyDefaultLanguage(query);
        var url = $"genre/movie/list{BuildQueryString(query)}";
        return await SendAsync<TmdbGenreListResponse>(url, ct);
    }

    public async Task<TmdbConfiguration> GetConfigurationAsync(CancellationToken ct = default)
    {
        return await SendAsync<TmdbConfiguration>("configuration", ct);
    }

    private async Task<TResponse> SendAsync<TResponse>(string relativeUrl, CancellationToken ct)
    {
        _logger.LogDebug("Requesting TMDB endpoint: {Url}", relativeUrl);

        var response = await _httpClient.GetAsync(relativeUrl, ct);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            _logger.LogWarning(
                "TMDB request to {Url} failed with {StatusCode}",
                relativeUrl,
                (int)response.StatusCode
            );

            throw new TmdbException(
                (int)response.StatusCode,
                body,
                $"TMDB request to '{relativeUrl}' failed with status {(int)response.StatusCode}."
            );
        }

        var content = await response.Content.ReadAsStringAsync(ct);
        var result = JsonSerializer.Deserialize<TResponse>(content, JsonOptions);

        return result
            ?? throw new InvalidOperationException(
                $"TMDB response deserialized to null for {typeof(TResponse).Name}."
            );
    }

    private static string BuildQueryString<T>(T query)
    {
        var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);
        var pairs = new List<string>();

        foreach (var prop in properties)
        {
            if (prop.GetCustomAttribute<JsonIgnoreAttribute>() is not null)
                continue;

            var value = prop.GetValue(query);
            if (value is null)
                continue;

            var name = prop.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name ?? prop.Name;
            var stringValue = value switch
            {
                bool b => b ? "true" : "false",
                _ => value.ToString(),
            };

            pairs.Add($"{Uri.EscapeDataString(name)}={Uri.EscapeDataString(stringValue!)}");
        }

        return pairs.Count > 0 ? "?" + string.Join("&", pairs) : "";
    }

    private void ApplyDefaultLanguage(TmdbSearchMovieQuery query)
    {
        if (string.IsNullOrWhiteSpace(query.Language))
            query.Language = _options.DefaultLanguage;
    }

    private void ApplyDefaultLanguage(TmdbMovieDetailsQuery query)
    {
        if (string.IsNullOrWhiteSpace(query.Language))
            query.Language = _options.DefaultLanguage;
    }

    private void ApplyDefaultLanguage(TmdbMovieCreditsQuery query)
    {
        if (string.IsNullOrWhiteSpace(query.Language))
            query.Language = _options.DefaultLanguage;
    }

    private void ApplyDefaultLanguage(TmdbMovieVideosQuery query)
    {
        if (string.IsNullOrWhiteSpace(query.Language))
            query.Language = _options.DefaultLanguage;
    }

    private void ApplyDefaultLanguage(TmdbMovieRecommendationsQuery query)
    {
        if (string.IsNullOrWhiteSpace(query.Language))
            query.Language = _options.DefaultLanguage;
    }

    private void ApplyDefaultLanguage(TmdbGenreListQuery query)
    {
        if (string.IsNullOrWhiteSpace(query.Language))
            query.Language = _options.DefaultLanguage;
    }
}
