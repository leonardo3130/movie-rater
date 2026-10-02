using MovieRaterApi.Data.Entities;

namespace MovieRaterApi.Features.Shared.Interfaces;

public interface IEnrichableMediaDto
{
    Guid Id { get; set; }
    int TmdbId { get; }
    MediaType MediaType { get; }
    bool IsFavorite { get; set; }
    bool IsInWatchlist { get; set; }
    int WatchedCount { get; set; }
}