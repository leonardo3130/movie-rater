using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using MovieRaterApi.Data;
using MovieRaterApi.Data.Entities;
using MovieRaterApi.Features.MovieLists.DTOs;
using MovieRaterApi.Features.MovieLists.Services;
using MovieRaterApi.Infrastructure.Exceptions;
using MovieRaterApi.Infrastructure.Tmdb;
using MovieRaterApi.Infrastructure.Tmdb.Dtos.Responses;

namespace MovieRaterApi.Tests.Unit.Services;

public class MovieListServiceTests
{
    private readonly ApplicationDbContext _db;
    private readonly Mock<ILogger<MovieListService>> _loggerMock;
    private readonly Mock<ITmdbClient> _tmdbMock;
    private readonly IMemoryCache _cache;
    private readonly MovieListService _sut;

    public MovieListServiceTests()
    {
        _db = TestHelpers.CreateInMemoryDbContext();
        _loggerMock = new Mock<ILogger<MovieListService>>();
        _tmdbMock = new Mock<ITmdbClient>();
        _cache = new MemoryCache(new MemoryCacheOptions());

        _tmdbMock
            .Setup(x => x.GetConfigurationAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                new TmdbConfiguration
                {
                    Images = new TmdbImageConfig { SecureBaseUrl = "https://image.tmdb.org/t/p/" },
                }
            );

        _sut = new MovieListService(_db, _tmdbMock.Object, _cache, _loggerMock.Object);
    }

    [Fact]
    public async Task CreateAsync_CreatesPrivateList()
    {
        var ownerId = SeedUser("owner");

        var result = await _sut.CreateAsync(
            new CreateMovieListRequestDto { Name = "My list", Description = "desc" },
            ownerId
        );

        result.Name.Should().Be("My list");
        result.Description.Should().Be("desc");
        result.IsPrivate.Should().BeTrue();
        result.IsOwner.Should().BeTrue();
        result.Movies.Should().BeEmpty();

        var row = _db.MovieLists.Single();
        row.OwnerUserId.Should().Be(ownerId);
        row.LastUpdatedAt.Should().Be(row.CreatedAt);
    }

    [Fact]
    public async Task CreateAsync_CreatesSharedList_WithGroupIds()
    {
        var ownerId = SeedUser("owner");
        var groupId = SeedGroup(ownerId);

        var result = await _sut.CreateAsync(
            new CreateMovieListRequestDto
            {
                Name = "Shared",
                IsPrivate = false,
                CanGroupEdit = true,
                GroupIds = [groupId],
            },
            ownerId
        );

        result.IsPrivate.Should().BeFalse();
        result.CanGroupEdit.Should().BeTrue();
        result.GroupIds.Should().Contain(groupId);
        _db.MovieListGroups.Count().Should().Be(1);
    }

    [Fact]
    public async Task CreateAsync_SharedList_NotMemberOfGroup_ThrowsBadRequest()
    {
        var ownerId = SeedUser("owner");
        var otherGroupId = SeedGroup(Guid.NewGuid());

        await FluentActions
            .Awaiting(() =>
                _sut.CreateAsync(
                    new CreateMovieListRequestDto
                    {
                        Name = "Shared",
                        IsPrivate = false,
                        GroupIds = [otherGroupId],
                    },
                    ownerId
                )
            )
            .Should()
            .ThrowAsync<BadRequestException>()
            .WithMessage("Can only share a list with groups you are a member of.");
    }

    [Fact]
    public async Task CreateAsync_CanGroupEditWithPrivateList_ThrowsBadRequest()
    {
        var ownerId = SeedUser("owner");

        await FluentActions
            .Awaiting(() =>
                _sut.CreateAsync(
                    new CreateMovieListRequestDto { Name = "List", CanGroupEdit = true },
                    ownerId
                )
            )
            .Should()
            .ThrowAsync<BadRequestException>()
            .WithMessage("CanGroupEdit requires the list to be shared.");
    }

    [Fact]
    public async Task UpdateAsync_UpdatesNameAndDescription_AndBumpsLastUpdatedAt()
    {
        var ownerId = SeedUser("owner");
        var created = await _sut.CreateAsync(
            new CreateMovieListRequestDto { Name = "Old" },
            ownerId
        );
        await Task.Delay(10);

        var result = await _sut.UpdateAsync(
            created.Id,
            new UpdateMovieListRequestDto { Name = "New", Description = "Updated" },
            ownerId
        );

        result.Name.Should().Be("New");
        result.Description.Should().Be("Updated");
        result.LastUpdatedAt.Should().BeAfter(created.LastUpdatedAt);

        var row = _db.MovieLists.Single();
        row.Name.Should().Be("New");
    }

    [Fact]
    public async Task UpdateAsync_ClearsDescription_WhenNull()
    {
        var ownerId = SeedUser("owner");
        var created = await _sut.CreateAsync(
            new CreateMovieListRequestDto { Name = "List", Description = "desc" },
            ownerId
        );

        var result = await _sut.UpdateAsync(
            created.Id,
            new UpdateMovieListRequestDto { Name = "List", Description = null },
            ownerId
        );

        result.Description.Should().BeNull();
    }

    [Fact]
    public async Task UpdateAsync_ChangesSharing_FromPrivateToShared()
    {
        var ownerId = SeedUser("owner");
        var groupId = SeedGroup(ownerId);
        var created = await _sut.CreateAsync(
            new CreateMovieListRequestDto { Name = "List" },
            ownerId
        );

        var result = await _sut.UpdateAsync(
            created.Id,
            new UpdateMovieListRequestDto
            {
                Name = "List",
                IsPrivate = false,
                GroupIds = [groupId],
            },
            ownerId
        );

        result.IsPrivate.Should().BeFalse();
        result.GroupIds.Should().Contain(groupId);
        _db.MovieListGroups.Count().Should().Be(1);
    }

    [Fact]
    public async Task UpdateAsync_NonOwner_ThrowsForbidden()
    {
        var ownerId = SeedUser("owner");
        var otherId = SeedUser("other");
        var created = await _sut.CreateAsync(
            new CreateMovieListRequestDto { Name = "List" },
            ownerId
        );

        await FluentActions
            .Awaiting(() =>
                _sut.UpdateAsync(
                    created.Id,
                    new UpdateMovieListRequestDto { Name = "Hijacked" },
                    otherId
                )
            )
            .Should()
            .ThrowAsync<ForbiddenException>()
            .WithMessage("You can only edit your own movie lists.");
    }

    [Fact]
    public async Task UpdateAsync_ListNotFound_ThrowsNotFound()
    {
        var ownerId = SeedUser("owner");

        await FluentActions
            .Awaiting(() =>
                _sut.UpdateAsync(
                    Guid.NewGuid(),
                    new UpdateMovieListRequestDto { Name = "List" },
                    ownerId
                )
            )
            .Should()
            .ThrowAsync<NotFoundException>()
            .WithMessage("Movie list not found.");
    }

    [Fact]
    public async Task DeleteAsync_RemovesList_AndCascadesAssociations()
    {
        var ownerId = SeedUser("owner");
        var movieId = SeedMovie();
        var created = await _sut.CreateAsync(
            new CreateMovieListRequestDto { Name = "List" },
            ownerId
        );
        await _sut.AddMovieAsync(created.Id, movieId, ownerId);
        _db.MovieListMovies.Count().Should().Be(1);

        await _sut.DeleteAsync(created.Id, ownerId);

        _db.MovieLists.Count().Should().Be(0);
        _db.MovieListMovies.Count().Should().Be(0);
    }

    [Fact]
    public async Task DeleteAsync_NonOwner_ThrowsForbidden()
    {
        var ownerId = SeedUser("owner");
        var otherId = SeedUser("other");
        var created = await _sut.CreateAsync(
            new CreateMovieListRequestDto { Name = "List" },
            ownerId
        );

        await FluentActions
            .Awaiting(() => _sut.DeleteAsync(created.Id, otherId))
            .Should()
            .ThrowAsync<ForbiddenException>()
            .WithMessage("You can only delete your own movie lists.");
    }

    [Fact]
    public async Task AddMovieAsync_AddsMovie_AndBumpsLastUpdatedAt()
    {
        var ownerId = SeedUser("owner");
        var movieId = SeedMovie();
        var created = await _sut.CreateAsync(
            new CreateMovieListRequestDto { Name = "List" },
            ownerId
        );
        await Task.Delay(10);

        await _sut.AddMovieAsync(created.Id, movieId, ownerId);

        var row = _db.MovieListMovies.Single();
        row.MovieListId.Should().Be(created.Id);
        row.MovieId.Should().Be(movieId);

        var list = _db.MovieLists.Single();
        list.LastUpdatedAt.Should().BeAfter(created.LastUpdatedAt);
    }

    [Fact]
    public async Task AddMovieAsync_Duplicate_ThrowsConflict()
    {
        var ownerId = SeedUser("owner");
        var movieId = SeedMovie();
        var created = await _sut.CreateAsync(
            new CreateMovieListRequestDto { Name = "List" },
            ownerId
        );
        await _sut.AddMovieAsync(created.Id, movieId, ownerId);

        await FluentActions
            .Awaiting(() => _sut.AddMovieAsync(created.Id, movieId, ownerId))
            .Should()
            .ThrowAsync<ConflictException>()
            .WithMessage("Movie is already in this list.");
    }

    [Fact]
    public async Task AddMovieAsync_MovieNotFound_ThrowsNotFound()
    {
        var ownerId = SeedUser("owner");
        var created = await _sut.CreateAsync(
            new CreateMovieListRequestDto { Name = "List" },
            ownerId
        );

        await FluentActions
            .Awaiting(() => _sut.AddMovieAsync(created.Id, Guid.NewGuid(), ownerId))
            .Should()
            .ThrowAsync<NotFoundException>()
            .WithMessage("Movie not found.");
    }

    [Fact]
    public async Task AddMovieAsync_SharedList_NonEditor_ThrowsForbidden()
    {
        var ownerId = SeedUser("owner");
        var memberId = SeedUser("member");
        var groupId = SeedGroup(ownerId, memberId);
        var movieId = SeedMovie();
        var created = await _sut.CreateAsync(
            new CreateMovieListRequestDto
            {
                Name = "Shared",
                IsPrivate = false,
                CanGroupEdit = false,
                GroupIds = [groupId],
            },
            ownerId
        );

        await FluentActions
            .Awaiting(() => _sut.AddMovieAsync(created.Id, movieId, memberId))
            .Should()
            .ThrowAsync<ForbiddenException>()
            .WithMessage("You are not allowed to edit this movie list.");
    }

    [Fact]
    public async Task AddMovieAsync_SharedList_GroupEditor_Succeeds()
    {
        var ownerId = SeedUser("owner");
        var memberId = SeedUser("member");
        var groupId = SeedGroup(ownerId, memberId);
        var movieId = SeedMovie();
        var created = await _sut.CreateAsync(
            new CreateMovieListRequestDto
            {
                Name = "Shared",
                IsPrivate = false,
                CanGroupEdit = true,
                GroupIds = [groupId],
            },
            ownerId
        );

        await _sut.AddMovieAsync(created.Id, movieId, memberId);

        _db.MovieListMovies.Count().Should().Be(1);
    }

    [Fact]
    public async Task AddMovieAsync_ListNotFound_ThrowsNotFound()
    {
        var ownerId = SeedUser("owner");
        var movieId = SeedMovie();

        await FluentActions
            .Awaiting(() => _sut.AddMovieAsync(Guid.NewGuid(), movieId, ownerId))
            .Should()
            .ThrowAsync<NotFoundException>()
            .WithMessage("Movie list not found.");
    }

    [Fact]
    public async Task RemoveMovieAsync_RemovesMovie_AndBumpsLastUpdatedAt()
    {
        var ownerId = SeedUser("owner");
        var movieId = SeedMovie();
        var created = await _sut.CreateAsync(
            new CreateMovieListRequestDto { Name = "List" },
            ownerId
        );
        await _sut.AddMovieAsync(created.Id, movieId, ownerId);
        var beforeRemove = _db.MovieLists.Single().LastUpdatedAt;
        await Task.Delay(10);

        await _sut.RemoveMovieAsync(created.Id, movieId, ownerId);

        _db.MovieListMovies.Count().Should().Be(0);
        _db.MovieLists.Single().LastUpdatedAt.Should().BeAfter(beforeRemove);
    }

    [Fact]
    public async Task RemoveMovieAsync_NotInList_ThrowsNotFound()
    {
        var ownerId = SeedUser("owner");
        var movieId = SeedMovie();
        var created = await _sut.CreateAsync(
            new CreateMovieListRequestDto { Name = "List" },
            ownerId
        );

        await FluentActions
            .Awaiting(() => _sut.RemoveMovieAsync(created.Id, movieId, ownerId))
            .Should()
            .ThrowAsync<NotFoundException>()
            .WithMessage("Movie is not in this list.");
    }

    [Fact]
    public async Task GetAsync_Owner_ReturnsMoviesOrderedByAddedAt()
    {
        var ownerId = SeedUser("owner");
        var movieA = SeedMovie("A");
        var movieB = SeedMovie("B");
        var created = await _sut.CreateAsync(
            new CreateMovieListRequestDto { Name = "List" },
            ownerId
        );
        await _sut.AddMovieAsync(created.Id, movieA, ownerId);
        await Task.Delay(10);
        await _sut.AddMovieAsync(created.Id, movieB, ownerId);

        var result = await _sut.GetAsync(created.Id, ownerId);

        result.IsOwner.Should().BeTrue();
        result.Movies.Should().HaveCount(2);
        result.Movies[0].Id.Should().Be(movieA);
        result.Movies[1].Id.Should().Be(movieB);
        result.Movies[0].PosterUrl.Should().NotBeNull();
    }

    [Fact]
    public async Task GetAsync_WhenMovieImagePathsAreMissing_DoesNotCallTmdbConfiguration()
    {
        var ownerId = SeedUser("owner");
        var movieId = Guid.NewGuid();
        _db.Movies.Add(
            new Movie
            {
                Id = movieId,
                TmdbId = 12345,
                Title = "No image movie",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            }
        );
        _db.SaveChanges();
        var created = await _sut.CreateAsync(
            new CreateMovieListRequestDto { Name = "List" },
            ownerId
        );
        await _sut.AddMovieAsync(created.Id, movieId, ownerId);
        _tmdbMock.Invocations.Clear();

        var result = await _sut.GetAsync(created.Id, ownerId);

        result.Movies.Should().HaveCount(1);
        result.Movies[0].PosterUrl.Should().BeNull();
        result.Movies[0].BackdropUrl.Should().BeNull();
        _tmdbMock.Verify(t => t.GetConfigurationAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetAsync_SharedList_GroupMember_CanView()
    {
        var ownerId = SeedUser("owner");
        var memberId = SeedUser("member");
        var groupId = SeedGroup(ownerId, memberId);
        var created = await _sut.CreateAsync(
            new CreateMovieListRequestDto
            {
                Name = "Shared",
                IsPrivate = false,
                GroupIds = [groupId],
            },
            ownerId
        );

        var result = await _sut.GetAsync(created.Id, memberId);

        result.Name.Should().Be("Shared");
        result.IsOwner.Should().BeFalse();
    }

    [Fact]
    public async Task GetAsync_PrivateList_OtherUser_ThrowsNotFound()
    {
        var ownerId = SeedUser("owner");
        var otherId = SeedUser("other");
        var created = await _sut.CreateAsync(
            new CreateMovieListRequestDto { Name = "Private" },
            ownerId
        );

        await FluentActions
            .Awaiting(() => _sut.GetAsync(created.Id, otherId))
            .Should()
            .ThrowAsync<NotFoundException>()
            .WithMessage("Movie list not found.");
    }

    [Fact]
    public async Task GetAllAsync_ReturnsOwnedAndSharedLists_WithCountsAndGroupIds()
    {
        var ownerId = SeedUser("owner");
        var memberId = SeedUser("member");
        var groupId = SeedGroup(ownerId, memberId);
        var movieId = SeedMovie();

        var privateList = await _sut.CreateAsync(
            new CreateMovieListRequestDto { Name = "Private" },
            ownerId
        );
        await Task.Delay(10);
        var sharedList = await _sut.CreateAsync(
            new CreateMovieListRequestDto
            {
                Name = "Shared",
                IsPrivate = false,
                GroupIds = [groupId],
            },
            ownerId
        );
        await _sut.AddMovieAsync(sharedList.Id, movieId, ownerId);

        var ownerResult = await _sut.GetAllAsync(ownerId);

        ownerResult.Should().HaveCount(2);
        ownerResult.Should().Contain(l => l.Id == privateList.Id);
        ownerResult.Should().Contain(l => l.Id == sharedList.Id);

        var sharedSummary = ownerResult.Single(l => l.Id == sharedList.Id);
        sharedSummary.MovieCount.Should().Be(1);
        sharedSummary.GroupIds.Should().Contain(groupId);
        sharedSummary.IsOwner.Should().BeTrue();

        var memberResult = await _sut.GetAllAsync(memberId);

        memberResult.Should().HaveCount(1);
        memberResult.Single().Id.Should().Be(sharedList.Id);
        memberResult.Single().IsOwner.Should().BeFalse();
    }

    private Guid SeedUser(string username)
    {
        var id = Guid.NewGuid();
        _db.Users.Add(
            new User
            {
                Id = id,
                Username = username,
                Email = $"{username}@example.com",
            }
        );
        _db.SaveChanges();
        return id;
    }

    private Guid SeedGroup(params Guid[] userIds)
    {
        var groupId = Guid.NewGuid();
        _db.Groups.Add(
            new Group
            {
                Id = groupId,
                Name = "Group",
                CreatedAt = DateTime.UtcNow,
            }
        );
        foreach (var userId in userIds)
        {
            _db.UserGroups.Add(
                new UserGroup
                {
                    Id = Guid.NewGuid(),
                    GroupId = groupId,
                    UserId = userId,
                }
            );
        }
        _db.SaveChanges();
        return groupId;
    }

    private Guid SeedMovie(string title = "Test Movie")
    {
        var movieId = Guid.NewGuid();
        _db.Movies.Add(
            new Movie
            {
                Id = movieId,
                TmdbId = new Random().Next(10000, 99999),
                Title = title,
                PosterUrl = "/poster.jpg",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            }
        );
        _db.SaveChanges();
        return movieId;
    }
}
