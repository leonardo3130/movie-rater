using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using MovieRaterApi.Data;
using MovieRaterApi.Data.Entities;
using MovieRaterApi.Features.Users.DTOs;
using MovieRaterApi.Features.Users.Services;

namespace MovieRaterApi.Tests.Unit.Services;

public class UserSuggestionServiceTests
{
    private readonly ApplicationDbContext _db;
    private readonly Mock<ILogger<UserSuggestionService>> _loggerMock;
    private readonly IMemoryCache _cache;
    private readonly UserSuggestionService _sut;

    public UserSuggestionServiceTests()
    {
        _db = TestHelpers.CreateInMemoryDbContext();
        _loggerMock = new Mock<ILogger<UserSuggestionService>>();
        _cache = new MemoryCache(new MemoryCacheOptions());
        _sut = new UserSuggestionService(_db, _cache, _loggerMock.Object);
    }

    private Guid AddUser(string username, string? picture = null)
    {
        var id = Guid.NewGuid();
        _db.Users.Add(
            new User
            {
                Id = id,
                Username = username,
                Email = $"{username}@example.com",
                ProfilePictureUrl = picture,
            }
        );
        return id;
    }

    [Fact]
    public async Task SuggestAsync_ShouldReturnMatchingUsernames_WhenPrefixMatches()
    {
        AddUser("leonardo");
        AddUser("leonora");
        AddUser("marco");
        await _db.SaveChangesAsync();

        var result = await _sut.SuggestAsync(
            Guid.NewGuid(),
            new UserSuggestionQuery { Prefix = "leo" }
        );

        result.Should().HaveCount(2);
        result.Should().OnlyContain(u => u.Username.ToLower().StartsWith("leo"));
    }

    [Fact]
    public async Task SuggestAsync_ShouldMatchCaseInsensitively_WhenPrefixCaseDiffers()
    {
        AddUser("Leonardo");
        var currentUserId = AddUser("someone");
        await _db.SaveChangesAsync();

        var result = await _sut.SuggestAsync(
            currentUserId,
            new UserSuggestionQuery { Prefix = "leo" }
        );

        result.Should().HaveCount(1);
        result.Single().Username.Should().Be("Leonardo");
    }

    [Fact]
    public async Task SuggestAsync_ShouldExcludeCurrentUser_FromResults()
    {
        AddUser("alice");
        var currentUserId = AddUser("alpaca");
        await _db.SaveChangesAsync();

        var result = await _sut.SuggestAsync(
            currentUserId,
            new UserSuggestionQuery { Prefix = "al" }
        );

        result.Should().HaveCount(1);
        result.Single().Username.Should().Be("alice");
    }

    [Fact]
    public async Task SuggestAsync_ShouldReturnEmpty_WhenNoMatches()
    {
        var currentUserId = AddUser("alice");
        await _db.SaveChangesAsync();

        var result = await _sut.SuggestAsync(
            currentUserId,
            new UserSuggestionQuery { Prefix = "zzz" }
        );

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task SuggestAsync_ShouldReturnEmpty_WhenPrefixIsBlank()
    {
        var currentUserId = AddUser("alice");
        await _db.SaveChangesAsync();

        var result = await _sut.SuggestAsync(
            currentUserId,
            new UserSuggestionQuery { Prefix = "   " }
        );

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task SuggestAsync_ShouldLimitResults_ToRequestedLimit()
    {
        var currentUserId = Guid.NewGuid();
        for (var i = 0; i < 10; i++)
        {
            AddUser($"alpha{i}");
        }
        await _db.SaveChangesAsync();

        var result = await _sut.SuggestAsync(
            currentUserId,
            new UserSuggestionQuery { Prefix = "alpha", Limit = 3 }
        );

        result.Should().HaveCount(3);
    }

    [Fact]
    public async Task SuggestAsync_ShouldClampLimit_ToMaximum()
    {
        var currentUserId = Guid.NewGuid();
        for (var i = 0; i < 10; i++)
        {
            AddUser($"beta{i}");
        }
        await _db.SaveChangesAsync();

        var result = await _sut.SuggestAsync(
            currentUserId,
            new UserSuggestionQuery { Prefix = "beta", Limit = 100 }
        );

        result.Should().HaveCount(10);
    }

    [Fact]
    public async Task SuggestAsync_ShouldReturnResults_OrderedByUsername()
    {
        var currentUserId = Guid.NewGuid();
        AddUser("carol");
        AddUser("alpha");
        AddUser("aan");
        await _db.SaveChangesAsync();

        var result = await _sut.SuggestAsync(
            currentUserId,
            new UserSuggestionQuery { Prefix = "a" }
        );

        result.Select(u => u.Username).Should().Equal("aan", "alpha");
    }

    [Fact]
    public async Task SuggestAsync_ShouldServeResultsFromCache_OnSecondCall()
    {
        var currentUserId = Guid.NewGuid();
        AddUser("delta_one");
        AddUser("delta_two");
        await _db.SaveChangesAsync();

        var first = await _sut.SuggestAsync(
            currentUserId,
            new UserSuggestionQuery { Prefix = "delta", Limit = 5 }
        );
        first.Should().HaveCount(2);

        AddUser("delta_three");
        await _db.SaveChangesAsync();

        var second = await _sut.SuggestAsync(
            currentUserId,
            new UserSuggestionQuery { Prefix = "DELTA", Limit = 5 }
        );

        second.Should().HaveCount(2);
        second.Should().Contain(u => u.Username == "delta_one");
        second.Should().Contain(u => u.Username == "delta_two");
    }

    [Fact]
    public async Task SuggestAsync_ShouldIncludeProfilePictureUrl()
    {
        AddUser("echo", "https://example.com/pic.png");
        var currentUserId = AddUser("other");
        await _db.SaveChangesAsync();

        var result = await _sut.SuggestAsync(
            currentUserId,
            new UserSuggestionQuery { Prefix = "echo" }
        );

        result.Single().ProfilePictureUrl.Should().Be("https://example.com/pic.png");
    }
}