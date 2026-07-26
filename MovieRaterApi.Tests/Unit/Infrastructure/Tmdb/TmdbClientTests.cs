using System.Net;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using MovieRaterApi.Infrastructure.Tmdb;
using MovieRaterApi.Infrastructure.Tmdb.Dtos.Requests;
using MovieRaterApi.Infrastructure.Tmdb.Dtos.Responses;
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

        var responseJson = JsonSerializer.Serialize(
            new TmdbPagedResponse<TmdbSearchMovieItem>
            {
                Page = 1,
                TotalPages = 1,
                TotalResults = 1,
                Results =
                [
                    new TmdbSearchMovieItem
                    {
                        Id = 550,
                        Title = "Fight Club",
                        Overview = "A movie.",
                        ReleaseDate = "1999-10-15",
                        VoteAverage = 8.433,
                        VoteCount = 26279,
                        GenreIds = [18, 53, 35],
                    },
                ],
            }
        );

        SetupHandler(responseJson);

        var result = await _sut.SearchMoviesAsync(query);

        result.Should().NotBeNull();
        result.Page.Should().Be(1);
        result.Results.Should().HaveCount(1);
        result.Results[0].Title.Should().Be("Fight Club");
        result.Results[0].Id.Should().Be(550);
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

        var responseJson = JsonSerializer.Serialize(
            new TmdbPagedResponse<TmdbSearchMovieItem>
            {
                Page = 1,
                TotalPages = 0,
                TotalResults = 0,
                Results = [],
            }
        );

        SetupHandler(responseJson);

        var result = await _sut.SearchMoviesAsync(query);

        result.Should().NotBeNull();
        _capturedRequestUri!.Query.Should().Contain("language=en-US");
    }

    [Fact]
    public async Task GetMovieDetailsAsync_ShouldSendCorrectUrl()
    {
        var query = new TmdbMovieDetailsQuery { MovieId = 11, Language = "en-US" };

        var responseJson = JsonSerializer.Serialize(
            new TmdbMovieDetails
            {
                Id = 11,
                Title = "Star Wars",
                Runtime = 121,
                ReleaseDate = "1977-05-25",
                VoteAverage = 8.2,
                Genres = [new TmdbGenre { Id = 12, Name = "Adventure" }],
            }
        );

        SetupHandler(responseJson);

        var result = await _sut.GetMovieDetailsAsync(query);

        result.Should().NotBeNull();
        result.Title.Should().Be("Star Wars");
        result.Runtime.Should().Be(121);
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

        var responseJson = JsonSerializer.Serialize(
            new TmdbMovieDetails { Id = 11, Title = "Star Wars" }
        );

        SetupHandler(responseJson);

        var result = await _sut.GetMovieDetailsAsync(query);

        result.Should().NotBeNull();
        _capturedRequestUri!.Query.Should().Contain("append_to_response=credits%2Cvideos");
    }

    [Fact]
    public async Task GetMovieCreditsAsync_ShouldSendCorrectUrl()
    {
        var query = new TmdbMovieCreditsQuery { MovieId = 550, Language = "en-US" };

        var responseJson = JsonSerializer.Serialize(
            new TmdbCreditsResponse
            {
                Id = 550,
                Cast =
                [
                    new TmdbCastMember
                    {
                        Id = 819,
                        Name = "Edward Norton",
                        Character = "The Narrator",
                        Order = 0,
                    },
                ],
                Crew = [],
            }
        );

        SetupHandler(responseJson);

        var result = await _sut.GetMovieCreditsAsync(query);

        result.Should().NotBeNull();
        result.Cast.Should().HaveCount(1);
        result.Cast[0].Name.Should().Be("Edward Norton");
        result.Cast[0].Character.Should().Be("The Narrator");
        _capturedRequestUri!.AbsolutePath.Should().Be("/3/movie/550/credits");
    }

    [Fact]
    public async Task GetMovieVideosAsync_ShouldSendCorrectUrl()
    {
        var query = new TmdbMovieVideosQuery { MovieId = 550, Language = "en-US" };

        var responseJson = JsonSerializer.Serialize(
            new TmdbVideosResponse
            {
                Id = 550,
                Results =
                [
                    new TmdbVideo
                    {
                        Key = "O-b2VfmmbyA",
                        Site = "YouTube",
                        Type = "Trailer",
                        Name = "Fight Club Trailer",
                    },
                ],
            }
        );

        SetupHandler(responseJson);

        var result = await _sut.GetMovieVideosAsync(query);

        result.Should().NotBeNull();
        result.Results.Should().HaveCount(1);
        result.Results[0].Key.Should().Be("O-b2VfmmbyA");
        _capturedRequestUri!.AbsolutePath.Should().Be("/3/movie/550/videos");
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

        var responseJson = JsonSerializer.Serialize(
            new TmdbPagedResponse<TmdbMovieDetails>
            {
                Page = 1,
                TotalPages = 1,
                TotalResults = 1,
                Results = [new TmdbMovieDetails { Id = 680, Title = "Pulp Fiction" }],
            }
        );

        SetupHandler(responseJson);

        var result = await _sut.GetMovieRecommendationsAsync(query);

        result.Should().NotBeNull();
        result.Results.Should().HaveCount(1);
        result.Results[0].Title.Should().Be("Pulp Fiction");
        _capturedRequestUri!.AbsolutePath.Should().Be("/3/movie/550/recommendations");
    }

    [Fact]
    public async Task GetMovieGenresAsync_ShouldSendCorrectUrl()
    {
        var responseJson = JsonSerializer.Serialize(
            new TmdbGenreListResponse
            {
                Genres =
                [
                    new TmdbGenre { Id = 28, Name = "Action" },
                    new TmdbGenre { Id = 12, Name = "Adventure" },
                ],
            }
        );

        SetupHandler(responseJson);

        var result = await _sut.GetMovieGenresAsync();

        result.Should().NotBeNull();
        result.Genres.Should().HaveCount(2);
        result.Genres[0].Name.Should().Be("Action");
        _capturedRequestUri!.AbsolutePath.Should().Be("/3/genre/movie/list");
    }

    [Fact]
    public async Task GetConfigurationAsync_ShouldSendCorrectUrl()
    {
        var responseJson = JsonSerializer.Serialize(
            new TmdbConfiguration
            {
                Images = new TmdbImageConfig
                {
                    SecureBaseUrl = "https://image.tmdb.org/t/p/",
                    PosterSizes = ["w92", "w154", "w342"],
                },
            }
        );

        SetupHandler(responseJson);

        var result = await _sut.GetConfigurationAsync();

        result.Should().NotBeNull();
        result.Images.SecureBaseUrl.Should().Be("https://image.tmdb.org/t/p/");
        result.Images.PosterSizes.Should().Contain("w342");
        _capturedRequestUri!.AbsolutePath.Should().Be("/3/configuration");
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

        var responseJson = JsonSerializer.Serialize(
            new TmdbPagedResponse<TmdbSearchMovieItem>
            {
                Page = 1,
                TotalPages = 0,
                TotalResults = 0,
                Results = [],
            }
        );

        SetupHandler(responseJson);

        var result = await _sut.SearchMoviesAsync(query);

        result.Should().NotBeNull();
        _capturedRequestUri!.Query.Should().Contain("year=1999");
        _capturedRequestUri.Query.Should().Contain("primary_release_year=1999");
        _capturedRequestUri.Query.Should().Contain("region=US");
        _capturedRequestUri.Query.Should().Contain("include_adult=true");
    }

    private void SetupHandler(string responseJson)
    {
        _handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync", // nome stringato perché è protected e non accessibili dall'esterno
                ItExpr.IsAny<HttpRequestMessage>(), // wildcard
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
