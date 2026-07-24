using System.Net;
using Microsoft.Extensions.Options;
using MovieRaterApi.Infrastructure.Tmdb.Exceptions;
using MovieRaterApi.Infrastructure.Tmdb.Options;

namespace MovieRaterApi.Infrastructure.Tmdb.Handlers;

public interface ITmdbDelay
{
    Task DelayAsync(int millisecondsDelay, CancellationToken ct);
}

public class TmdbSystemDelay : ITmdbDelay
{
    public Task DelayAsync(int millisecondsDelay, CancellationToken ct) =>
        Task.Delay(millisecondsDelay, ct);
}

public class TmdbRateLimitHandler : DelegatingHandler
{
    private readonly TmdbOptions _options;
    private readonly ILogger<TmdbRateLimitHandler> _logger;
    private readonly ITmdbDelay _delay;
    private static readonly Random Jitter = new();

    public TmdbRateLimitHandler(
        IOptions<TmdbOptions> options,
        ILogger<TmdbRateLimitHandler> logger,
        ITmdbDelay? delay = null
    )
    {
        _options = options.Value;
        _logger = logger;
        _delay = delay ?? new TmdbSystemDelay();
    }

    // this method needs mock in unit testing
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken
    )
    {
        var attempts = 0;

        while (true)
        {
            attempts++;

            var response = await base.SendAsync(request, cancellationToken);

            if (response.StatusCode != HttpStatusCode.TooManyRequests)
                return response;

            if (attempts >= _options.MaxRetries)
            {
                _logger.LogWarning(
                    "TMDB rate limit exhausted for {Method} {Uri} after {Attempts} attempts",
                    request.Method,
                    request.RequestUri,
                    attempts
                );

                throw new TmdbRateLimitExhaustedException(
                    attempts,
                    $"TMDB rate limit exhausted after {attempts} attempts."
                );
            }

            var delayMs = CalculateDelay(response, attempts);

            _logger.LogWarning(
                "TMDB rate limited on {Method} {Uri}, retrying in {DelayMs}ms (attempt {Attempt}/{MaxRetries})",
                request.Method,
                request.RequestUri,
                delayMs,
                attempts,
                _options.MaxRetries
            );

            await _delay.DelayAsync(delayMs, cancellationToken);

            request = await CloneRequestAsync(request);
        }
    }

    private int CalculateDelay(HttpResponseMessage response, int attempts)
    {
        if (response.Headers.RetryAfter?.Delta is not null)
            return (int)response.Headers.RetryAfter.Delta.Value.TotalMilliseconds;

        var exponentialDelay = _options.RetryBaseDelayMs * (int)Math.Pow(2, attempts - 1);
        var jitterMs = Jitter.Next(0, 251);
        return exponentialDelay + jitterMs;
    }

    private static async Task<HttpRequestMessage> CloneRequestAsync(HttpRequestMessage request)
    {
        var clone = new HttpRequestMessage(request.Method, request.RequestUri);
        clone.Version = request.Version;

        foreach (var header in request.Headers)
            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);

        if (request.Content is not null)
        {
            var body = await request.Content.ReadAsByteArrayAsync();
            clone.Content = new ByteArrayContent(body);
            foreach (var header in request.Content.Headers)
                clone.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        return clone;
    }
}
