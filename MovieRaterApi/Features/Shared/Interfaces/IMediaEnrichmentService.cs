namespace MovieRaterApi.Features.Shared.Interfaces;

public interface IMediaEnrichmentService
{
    Task EnrichAsync(IEnumerable<IEnrichableMediaDto> items, CancellationToken ct = default);

    Task EnrichAsync(IEnrichableMediaDto item, CancellationToken ct = default);
}