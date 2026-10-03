using System.Net;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using MovieRaterApi.Infrastructure.Tmdb;
using MovieRaterApi.Infrastructure.Tmdb.Dtos.Requests;
using MovieRaterApi.Infrastructure.Tmdb.Exceptions;
using MovieRaterApi.Infrastructure.Tmdb.Options;

namespace MovieRaterApi.Tests.Unit.Infrastructure.Tmdb;

public class TmdbClientTests
{
    private readonly Mock<HttpMessageHandler> _handlerMock = new(MockBehavior.Strict);
    private readonly TmdbOptions _options = new()
    {
        ApiKey = "test-api-key",
        BaseUrl = "https://api.themoviedb.org/3/",
        DefaultLanguage = "en-US",
        RequestTimeoutSeconds = 30,
    };
    private readonly Mock<ILogger<TmdbClient>> _loggerMock = new();
    private readonly HttpClient _httpClient;
    private readonly TmdbClient _sut;
    private Uri? _capturedRequestUri;

    public TmdbClientTests()
    {
        _httpClient = new HttpClient(_handlerMock.Object)
        {
            BaseAddress = new Uri(_options.BaseUrl),
        };
        var optionsMock = new Mock<IOptions<TmdbOptions>>();
        optionsMock.Setup(o => o.Value).Returns(_options);

        _sut = new TmdbClient(_httpClient, optionsMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task SearchMoviesAsync_ShouldSendCorrectUrl()
    {
        var query = new TmdbSearchMovieQuery
        {
            Query = "fight club",
            Page = 1,
            IncludeAdult = false,
            Language = "en-US",
        };

        SetupHandler("{}");

        var result = await _sut.SearchMoviesAsync(query);

        result.Should().NotBeNull();
        _capturedRequestUri!.AbsolutePath.Should().Be("/3/search/movie");
        _capturedRequestUri.Query.Should().Contain("query=fight%20club");
        _capturedRequestUri.Query.Should().Contain("include_adult=false");
        _capturedRequestUri.Query.Should().Contain("page=1");
        _capturedRequestUri.Query.Should().Contain("language=en-US");
    }

    [Fact]
    public async Task SearchMoviesAsync_ShouldApplyDefaultLanguage_WhenNull()
    {
        var query = new TmdbSearchMovieQuery { Query = "test", Language = null };

        SetupHandler("{}");

        var result = await _sut.SearchMoviesAsync(query);

        result.Should().NotBeNull();
        _capturedRequestUri!.Query.Should().Contain("language=en-US");
    }

    [Fact]
    public async Task SearchMoviesAsync_ShouldIncludeOptionalParams_WhenProvided()
    {
        var query = new TmdbSearchMovieQuery
        {
            Query = "test",
            Year = "1999",
            PrimaryReleaseYear = "1999",
            Region = "US",
            IncludeAdult = true,
            Language = "en-US",
        };

        SetupHandler("{}");

        var result = await _sut.SearchMoviesAsync(query);

        result.Should().NotBeNull();
        _capturedRequestUri!.Query.Should().Contain("year=1999");
        _capturedRequestUri.Query.Should().Contain("primary_release_year=1999");
        _capturedRequestUri.Query.Should().Contain("region=US");
        _capturedRequestUri.Query.Should().Contain("include_adult=true");
    }

    [Fact]
    public async Task GetMovieDetailsAsync_ShouldSendCorrectUrl()
    {
        var query = new TmdbMovieDetailsQuery { MovieId = 11, Language = "en-US" };

        SetupHandler("{}");

        var result = await _sut.GetMovieDetailsAsync(query);

        result.Should().NotBeNull();
        _capturedRequestUri!.AbsolutePath.Should().Be("/3/movie/11");
        _capturedRequestUri.Query.Should().Contain("language=en-US");
    }

    [Fact]
    public async Task GetMovieDetailsAsync_ShouldIncludeAppendToResponse_WhenProvided()
    {
        var query = new TmdbMovieDetailsQuery
        {
            MovieId = 11,
            Language = "en-US",
            AppendToResponse = "credits,videos",
        };

        SetupHandler("{}");

        var result = await _sut.GetMovieDetailsAsync(query);

        result.Should().NotBeNull();
        _capturedRequestUri!.Query.Should().Contain("append_to_response=credits%2Cvideos");
    }

    [Fact]
    public async Task GetMovieCreditsAsync_ShouldSendCorrectUrl()
    {
        var query = new TmdbMovieCreditsQuery { MovieId = 550, Language = "en-US" };

        SetupHandler("{}");

        var result = await _sut.GetMovieCreditsAsync(query);

        result.Should().NotBeNull();
        _capturedRequestUri!.AbsolutePath.Should().Be("/3/movie/550/credits");
        _capturedRequestUri.Query.Should().Contain("language=en-US");
    }

    [Fact]
    public async Task GetMovieVideosAsync_ShouldSendCorrectUrl()
    {
        var query = new TmdbMovieVideosQuery { MovieId = 550, Language = "en-US" };

        SetupHandler("{}");

        var result = await _sut.GetMovieVideosAsync(query);

        result.Should().NotBeNull();
        _capturedRequestUri!.AbsolutePath.Should().Be("/3/movie/550/videos");
        _capturedRequestUri.Query.Should().Contain("language=en-US");
    }

    [Fact]
    public async Task GetMovieRecommendationsAsync_ShouldSendCorrectUrl()
    {
        var query = new TmdbMovieRecommendationsQuery
        {
            MovieId = 550,
            Page = 1,
            Language = "en-US",
        };

        SetupHandler("{}");

        var result = await _sut.GetMovieRecommendationsAsync(query);

        result.Should().NotBeNull();
        _capturedRequestUri!.AbsolutePath.Should().Be("/3/movie/550/recommendations");
        _capturedRequestUri.Query.Should().Contain("page=1");
        _capturedRequestUri.Query.Should().Contain("language=en-US");
    }

    [Fact]
    public async Task GetMovieGenresAsync_ShouldSendCorrectUrl()
    {
        SetupHandler("{}");

        var result = await _sut.GetMovieGenresAsync();

        result.Should().NotBeNull();
        _capturedRequestUri!.AbsolutePath.Should().Be("/3/genre/movie/list");
    }

    [Fact]
    public async Task GetConfigurationAsync_ShouldSendCorrectUrl()
    {
        SetupHandler("{}");

        var result = await _sut.GetConfigurationAsync();

        result.Should().NotBeNull();
        _capturedRequestUri!.AbsolutePath.Should().Be("/3/configuration");
    }

    [Fact]
    public async Task SearchTvShowsAsync_ShouldSendCorrectUrl()
    {
        var query = new TmdbSearchTvQuery
        {
            Query = "breaking bad",
            Page = 1,
            IncludeAdult = false,
            FirstAirDateYear = "2008",
            Year = "2008",
            Language = "en-US",
        };

        SetupHandler("{}");

        var result = await _sut.SearchTvShowsAsync(query);

        result.Should().NotBeNull();
        _capturedRequestUri!.AbsolutePath.Should().Be("/3/search/tv");
        _capturedRequestUri.Query.Should().Contain("query=breaking%20bad");
        _capturedRequestUri.Query.Should().Contain("page=1");
        _capturedRequestUri.Query.Should().Contain("include_adult=false");
        _capturedRequestUri.Query.Should().Contain("first_air_date_year=2008");
        _capturedRequestUri.Query.Should().Contain("year=2008");
        _capturedRequestUri.Query.Should().Contain("language=en-US");
    }

    [Fact]
    public async Task SearchTvShowsAsync_ShouldApplyDefaultLanguage_WhenNull()
    {
        var query = new TmdbSearchTvQuery { Query = "test", Language = null };

        SetupHandler("{}");

        var result = await _sut.SearchTvShowsAsync(query);

        result.Should().NotBeNull();
        _capturedRequestUri!.Query.Should().Contain("language=en-US");
    }

    [Fact]
    public async Task GetDiscoverTvShowsAsync_ShouldSendCorrectUrl()
    {
        var query = new TmdbDiscoverTvQuery
        {
            Page = 1,
            WithGenres = "10765",
            FirstAirDateYear = "2022",
            SortBy = "popularity.desc",
            Language = "en-US",
        };

        SetupHandler("{}");

        var result = await _sut.GetDiscoverTvShowsAsync(query);

        result.Should().NotBeNull();
        _capturedRequestUri!.AbsolutePath.Should().Be("/3/discover/tv");
        _capturedRequestUri.Query.Should().Contain("with_genres=10765");
        _capturedRequestUri.Query.Should().Contain("first_air_date_year=2022");
        _capturedRequestUri.Query.Should().Contain("sort_by=popularity.desc");
        _capturedRequestUri.Query.Should().Contain("page=1");
        _capturedRequestUri.Query.Should().Contain("language=en-US");
    }

    [Fact]
    public async Task GetDiscoverTvShowsAsync_ShouldApplyDefaultLanguage_WhenNull()
    {
        var query = new TmdbDiscoverTvQuery { WithGenres = "10765", Language = null };

        SetupHandler("{}");

        var result = await _sut.GetDiscoverTvShowsAsync(query);

        result.Should().NotBeNull();
        _capturedRequestUri!.Query.Should().Contain("language=en-US");
    }

    [Fact]
    public async Task GetTvShowDetailsAsync_ShouldSendCorrectUrl()
    {
        var query = new TmdbTvDetailsQuery { SeriesId = 1396, Language = "en-US" };

        SetupHandler("{}");

        var result = await _sut.GetTvShowDetailsAsync(query);

        result.Should().NotBeNull();
        _capturedRequestUri!.AbsolutePath.Should().Be("/3/tv/1396");
        _capturedRequestUri.Query.Should().Contain("language=en-US");
    }

    [Fact]
    public async Task GetTvShowDetailsAsync_ShouldIncludeAppendToResponse_WhenProvided()
    {
        var query = new TmdbTvDetailsQuery
        {
            SeriesId = 1396,
            Language = "en-US",
            AppendToResponse = "credits,videos",
        };

        SetupHandler("{}");

        var result = await _sut.GetTvShowDetailsAsync(query);

        result.Should().NotBeNull();
        _capturedRequestUri!.Query.Should().Contain("append_to_response=credits%2Cvideos");
    }

    [Fact]
    public async Task GetTvSeasonAsync_ShouldSendCorrectUrl()
    {
        var query = new TmdbTvSeasonQuery { SeriesId = 1396, SeasonNumber = 1, Language = "en-US" };

        SetupHandler("{}");

        var result = await _sut.GetTvSeasonAsync(query);

        result.Should().NotBeNull();
        _capturedRequestUri!.AbsolutePath.Should().Be("/3/tv/1396/season/1");
        _capturedRequestUri.Query.Should().Contain("language=en-US");
    }

    [Fact]
    public async Task GetTvEpisodeAsync_ShouldSendCorrectUrl()
    {
        var query = new TmdbTvEpisodeQuery
        {
            SeriesId = 1396,
            SeasonNumber = 1,
            EpisodeNumber = 1,
            Language = "en-US",
        };

        SetupHandler("{}");

        var result = await _sut.GetTvEpisodeAsync(query);

        result.Should().NotBeNull();
        _capturedRequestUri!.AbsolutePath.Should().Be("/3/tv/1396/season/1/episode/1");
        _capturedRequestUri.Query.Should().Contain("language=en-US");
    }

    [Fact]
    public async Task GetTvGenresAsync_ShouldSendCorrectUrl()
    {
        SetupHandler("{}");

        var result = await _sut.GetTvGenresAsync();

        result.Should().NotBeNull();
        _capturedRequestUri!.AbsolutePath.Should().Be("/3/genre/tv/list");
    }

    [Fact]
    public async Task SendAsync_ShouldThrowTmdbException_WhenNonSuccessStatusCode()
    {
        var query = new TmdbSearchMovieQuery { Query = "nonexistent", Language = "en-US" };

        var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(
                new HttpResponseMessage(HttpStatusCode.NotFound)
                {
                    Content = new StringContent(
                        "{\"status_code\":34,\"status_message\":\"The resource you requested could not be found.\"}"
                    ),
                }
            );

        var httpClient = new HttpClient(handlerMock.Object)
        {
            BaseAddress = new Uri(_options.BaseUrl),
        };
        var optionsMock = new Mock<IOptions<TmdbOptions>>();
        optionsMock.Setup(o => o.Value).Returns(_options);
        var client = new TmdbClient(httpClient, optionsMock.Object, _loggerMock.Object);

        var act = async () => await client.SearchMoviesAsync(query);

        var exception = await act.Should().ThrowAsync<TmdbException>();
        exception.Which.StatusCode.Should().Be(404);
    }

    private void SetupHandler(string responseJson)
    {
        _handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(
                (HttpRequestMessage request, CancellationToken _) =>
                {
                    _capturedRequestUri = request.RequestUri;
                    return new HttpResponseMessage
                    {
                        StatusCode = HttpStatusCode.OK,
                        Content = new StringContent(
                            responseJson,
                            System.Text.Encoding.UTF8,
                            "application/json"
                        ),
                        RequestMessage = request,
                    };
                }
            );
    }
}