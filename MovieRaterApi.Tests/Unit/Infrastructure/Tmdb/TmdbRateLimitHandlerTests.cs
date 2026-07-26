using System.Net;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using MovieRaterApi.Infrastructure.Tmdb.Exceptions;
using MovieRaterApi.Infrastructure.Tmdb.Handlers;
using MovieRaterApi.Infrastructure.Tmdb.Options;

namespace MovieRaterApi.Tests.Unit.Infrastructure.Tmdb;

public class TmdbRateLimitHandlerTests
{
    private readonly TmdbOptions _options = new() { MaxRetries = 3, RetryBaseDelayMs = 500 };
    private readonly Mock<ILogger<TmdbRateLimitHandler>> _loggerMock = new();
    private readonly TestDelay _delay = new();

    [Fact]
    public async Task SendAsync_ShouldReturnResponse_WhenFirstAttemptSucceeds()
    {
        using var innerHandler = new SequenceHandler(HttpStatusCode.OK);
        using var httpClient = CreateHttpClient(innerHandler);

        var response = await httpClient.GetAsync("http://localhost/test");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        innerHandler.CallCount.Should().Be(1);
    }

    [Fact]
    public async Task SendAsync_ShouldRetry_When429Received()
    {
        using var innerHandler = new SequenceHandler(
            HttpStatusCode.TooManyRequests,
            HttpStatusCode.TooManyRequests,
            HttpStatusCode.OK
        );
        using var httpClient = CreateHttpClient(innerHandler);

        var response = await httpClient.GetAsync("http://localhost/test");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        innerHandler.CallCount.Should().Be(3);
    }

    [Fact]
    public async Task SendAsync_ShouldThrow_WhenAllAttemptsReturn429()
    {
        using var innerHandler = new SequenceHandler(
            HttpStatusCode.TooManyRequests,
            HttpStatusCode.TooManyRequests,
            HttpStatusCode.TooManyRequests
        );
        using var httpClient = CreateHttpClient(innerHandler);

        var act = async () => await httpClient.GetAsync("http://localhost/test");

        var exception = await act.Should().ThrowAsync<TmdbRateLimitExhaustedException>();
        exception.Which.Attempts.Should().Be(3);
        innerHandler.CallCount.Should().Be(3);
    }

    [Fact]
    public async Task SendAsync_ShouldRespectRetryAfterHeader()
    {
        _options.MaxRetries = 2;

        using var innerHandler = new SequenceHandler(
            (HttpStatusCode.TooManyRequests, TimeSpan.FromSeconds(5)),
            HttpStatusCode.OK
        );
        using var httpClient = CreateHttpClient(innerHandler);

        var response = await httpClient.GetAsync("http://localhost/test");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        _delay.Delays.Should().Contain(d => d == 5000);
    }

    [Fact]
    public async Task SendAsync_ShouldNotRetry_WhenNon429StatusCode()
    {
        using var innerHandler = new SequenceHandler(HttpStatusCode.NotFound);
        using var httpClient = CreateHttpClient(innerHandler);

        var response = await httpClient.GetAsync("http://localhost/test");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        innerHandler.CallCount.Should().Be(1);
    }

    [Fact]
    public async Task SendAsync_ShouldHonorCancellationToken_DuringDelay()
    {
        _options.MaxRetries = 2;
        _delay.CancelOnNextDelay = true;

        using var innerHandler = new SequenceHandler(
            HttpStatusCode.TooManyRequests,
            HttpStatusCode.TooManyRequests
        );
        using var httpClient = CreateHttpClient(innerHandler);

        using var cts = new CancellationTokenSource();

        var act = async () => await httpClient.GetAsync("http://localhost/test", cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task SendAsync_ShouldUseExponentialBackoff_WhenNoRetryAfterHeader()
    {
        _options.MaxRetries = 3;
        _options.RetryBaseDelayMs = 100;

        using var innerHandler = new SequenceHandler(
            HttpStatusCode.TooManyRequests,
            HttpStatusCode.TooManyRequests,
            HttpStatusCode.OK
        );
        using var httpClient = CreateHttpClient(innerHandler);

        var response = await httpClient.GetAsync("http://localhost/test");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        innerHandler.CallCount.Should().Be(3);

        _delay.Delays.Should().HaveCount(2);
        _delay.Delays[0].Should().BeGreaterThanOrEqualTo(100);
        _delay.Delays[1].Should().BeGreaterThanOrEqualTo(200);
    }

    private HttpClient CreateHttpClient(HttpMessageHandler innerHandler)
    {
        var optionsMock = new Mock<IOptions<TmdbOptions>>();
        optionsMock.Setup(o => o.Value).Returns(_options);

        var handler = new TmdbRateLimitHandler(optionsMock.Object, _loggerMock.Object, _delay)
        {
            InnerHandler = innerHandler,
        };

        return new HttpClient(handler);
    }

    private class TestDelay : ITmdbDelay
    {
        public List<int> Delays { get; } = [];
        public bool CancelOnNextDelay { get; set; }

        public Task DelayAsync(int millisecondsDelay, CancellationToken ct)
        {
            Delays.Add(millisecondsDelay);

            if (CancelOnNextDelay)
                throw new OperationCanceledException(ct);

            return Task.CompletedTask;
        }
    }

    private class SequenceHandler : HttpMessageHandler
    {
        private readonly Queue<Func<HttpResponseMessage>> _responses;
        private int _callCount;

        public int CallCount => _callCount;

        public SequenceHandler(params HttpStatusCode[] statusCodes)
        {
            _responses = new Queue<Func<HttpResponseMessage>>();
            foreach (var code in statusCodes)
                _responses.Enqueue(() => new HttpResponseMessage(code));
        }

        public SequenceHandler(params object[] responses)
        {
            _responses = new Queue<Func<HttpResponseMessage>>();
            foreach (var item in responses)
            {
                if (item is HttpStatusCode code)
                {
                    // simula  retry senza retryAfter
                    _responses.Enqueue(() => new HttpResponseMessage(code));
                }
                else if (item is ValueTuple<HttpStatusCode, TimeSpan> tuple)
                {
                    // simula anche la presenza dell'header RetryAfter
                    var (statusCode, retryAfter) = tuple;
                    _responses.Enqueue(() =>
                        new HttpResponseMessage(statusCode)
                        {
                            Headers =
                            {
                                RetryAfter = new System.Net.Http.Headers.RetryConditionHeaderValue(
                                    retryAfter
                                ),
                            },
                        }
                    );
                }
            }
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken
        )
        {
            Interlocked.Increment(ref _callCount);
            var response = _responses.Dequeue().Invoke();
            response.RequestMessage = request;
            return Task.FromResult(response);
        }
    }
}
