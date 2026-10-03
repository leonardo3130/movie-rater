using System.Text.Json.Serialization;

namespace MovieRaterApi.Data.Entities;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum MediaType
{
    Movie = 0,
    TvSeries = 1,
    TvSeason = 2,
    TvEpisode = 3,
}