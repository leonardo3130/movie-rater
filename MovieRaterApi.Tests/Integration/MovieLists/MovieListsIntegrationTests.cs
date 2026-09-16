using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MovieRaterApi.Data;
using MovieRaterApi.Data.Entities;
using MovieRaterApi.Features.Authentication.DTOs;
using MovieRaterApi.Features.MovieLists.DTOs;
using Testcontainers.PostgreSql;

namespace MovieRaterApi.Tests.Integration.MovieLists;

public class MovieListsIntegrationTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgresContainer;
    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _client = null!;
    private ApplicationDbContext _db = null!;

    public MovieListsIntegrationTests()
    {
        _postgresContainer = new PostgreSqlBuilder("postgres:17")
            .WithCleanUp(true)
            .WithDatabase("movierater_test")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .Build();
    }

    public async Task InitializeAsync()
    {
        await _postgresContainer.StartAsync();

        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseSetting(
                "ConnectionStrings:DefaultConnection",
                _postgresContainer.GetConnectionString()
            );
            builder.UseSetting("Jwt:Issuer", "MovieRaterApi");
            builder.UseSetting("Jwt:Audience", "MovieRaterWeb");
            builder.UseSetting("Jwt:AccessTokenMinutes", "15");
            builder.UseSetting("Jwt:RefreshTokenDays", "30");
            builder.UseSetting(
                "Jwt:SigningKey",
                "test-signing-key-that-is-at-least-32-characters-long-for-testing"
            );

            builder.ConfigureServices(services =>
            {
                var descriptor = services.SingleOrDefault(d =>
                    d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>)
                );
                if (descriptor is not null)
                    services.Remove(descriptor);

                services.AddDbContext<ApplicationDbContext>(options =>
                    options.UseNpgsql(_postgresContainer.GetConnectionString())
                );
            });
        });

        _client = _factory.CreateClient(
            new WebApplicationFactoryClientOptions
            {
                HandleCookies = true,
                BaseAddress = new Uri("https://localhost"),
            }
        );

        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        optionsBuilder.UseNpgsql(_postgresContainer.GetConnectionString());
        _db = new ApplicationDbContext(optionsBuilder.Options);
        _db.Database.EnsureCreated();
    }

    public async Task DisposeAsync()
    {
        await _db.DisposeAsync();
        _client.Dispose();
        _factory.Dispose();
        await _postgresContainer.DisposeAsync();
    }

    [Fact]
    public async Task CreateList_ShouldReturn201()
    {
        var (token, _) = await SeedUserAndTokenAsync("create1");
        _client.DefaultRequestHeaders.Authorization = Bearer(token);

        var response = await _client.PostAsJsonAsync(
            "/api/movie-lists",
            new CreateMovieListRequestDto { Name = "My list", Description = "desc" }
        );

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<MovieListResponseDto>();
        result.Should().NotBeNull();
        result!.Name.Should().Be("My list");
        result.IsPrivate.Should().BeTrue();
        result.IsOwner.Should().BeTrue();
        result.Movies.Should().BeEmpty();
    }

    [Fact]
    public async Task CreateList_SharedWithoutGroups_ShouldReturn400()
    {
        var (token, _) = await SeedUserAndTokenAsync("create2");
        _client.DefaultRequestHeaders.Authorization = Bearer(token);

        var response = await _client.PostAsJsonAsync(
            "/api/movie-lists",
            new CreateMovieListRequestDto { Name = "Shared", IsPrivate = false }
        );

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetAllLists_ShouldReturnOwnedLists_WithoutFilms()
    {
        var (token, userId) = await SeedUserAndTokenAsync("lists1");
        _client.DefaultRequestHeaders.Authorization = Bearer(token);
        var movieId = SeedMovie();

        var createResponse = await _client.PostAsJsonAsync(
            "/api/movie-lists",
            new CreateMovieListRequestDto { Name = "My list" }
        );
        createResponse.EnsureSuccessStatusCode();
        var created = await createResponse.Content.ReadFromJsonAsync<MovieListResponseDto>();

        await _client.PostAsync($"/api/movie-lists/{created!.Id}/movies/{movieId}", null);

        var response = await _client.GetAsync("/api/movie-lists");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<List<MovieListSummaryDto>>();
        result.Should().HaveCount(1);
        result![0].Id.Should().Be(created.Id);
        result[0].MovieCount.Should().Be(1);
        result[0].IsOwner.Should().BeTrue();
        result[0].GroupIds.Should().BeEmpty();
    }

    [Fact]
    public async Task GetList_ShouldReturnListWithFilms()
    {
        var (token, _) = await SeedUserAndTokenAsync("detail1");
        _client.DefaultRequestHeaders.Authorization = Bearer(token);
        var movieA = SeedMovie();
        var movieB = SeedMovie();

        var createResponse = await _client.PostAsJsonAsync(
            "/api/movie-lists",
            new CreateMovieListRequestDto { Name = "My list" }
        );
        createResponse.EnsureSuccessStatusCode();
        var created = await createResponse.Content.ReadFromJsonAsync<MovieListResponseDto>();

        await _client.PostAsync($"/api/movie-lists/{created!.Id}/movies/{movieA}", null);
        await _client.PostAsync($"/api/movie-lists/{created.Id}/movies/{movieB}", null);

        var response = await _client.GetAsync($"/api/movie-lists/{created.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<MovieListResponseDto>();
        result.Should().NotBeNull();
        result!.Movies.Should().HaveCount(2);
        result.Movies[0].Id.Should().Be(movieA);
        result.Movies[1].Id.Should().Be(movieB);
    }

    [Fact]
    public async Task UpdateList_ShouldRenameAndReturn200()
    {
        var (token, _) = await SeedUserAndTokenAsync("update1");
        _client.DefaultRequestHeaders.Authorization = Bearer(token);
        var listId = await SeedListAsync("Old name");

        var response = await _client.PatchAsJsonAsync(
            $"/api/movie-lists/{listId}",
            new UpdateMovieListRequestDto { Name = "New name", Description = "updated" }
        );

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<MovieListResponseDto>();
        result!.Name.Should().Be("New name");
        result.Description.Should().Be("updated");
    }

    [Fact]
    public async Task UpdateList_NonOwner_ShouldReturn403()
    {
        var (ownerToken, ownerId) = await SeedUserAndTokenAsync("update2");
        _client.DefaultRequestHeaders.Authorization = Bearer(ownerToken);
        var listId = await SeedListAsync("My list");

        var (otherToken, _) = await SeedUserAndTokenAsync("update3");
        _client.DefaultRequestHeaders.Authorization = Bearer(otherToken);

        var response = await _client.PatchAsJsonAsync(
            $"/api/movie-lists/{listId}",
            new UpdateMovieListRequestDto { Name = "Hijacked" }
        );

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task DeleteList_ShouldReturn204_AndRemoveAssociations()
    {
        var (token, _) = await SeedUserAndTokenAsync("delete1");
        _client.DefaultRequestHeaders.Authorization = Bearer(token);
        var movieId = SeedMovie();
        var listId = await SeedListAsync("My list");
        await _client.PostAsync($"/api/movie-lists/{listId}/movies/{movieId}", null);

        var response = await _client.DeleteAsync($"/api/movie-lists/{listId}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        _db.MovieLists.Count().Should().Be(0);
        _db.MovieListMovies.Count().Should().Be(0);
    }

    [Fact]
    public async Task AddMovie_ShouldReturn204()
    {
        var (token, _) = await SeedUserAndTokenAsync("add1");
        _client.DefaultRequestHeaders.Authorization = Bearer(token);
        var movieId = SeedMovie();
        var listId = await SeedListAsync("My list");

        var response = await _client.PostAsync($"/api/movie-lists/{listId}/movies/{movieId}", null);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        _db.MovieListMovies.Count().Should().Be(1);
    }

    [Fact]
    public async Task AddMovie_Duplicate_ShouldReturn409()
    {
        var (token, _) = await SeedUserAndTokenAsync("add2");
        _client.DefaultRequestHeaders.Authorization = Bearer(token);
        var movieId = SeedMovie();
        var listId = await SeedListAsync("My list");
        await _client.PostAsync($"/api/movie-lists/{listId}/movies/{movieId}", null);

        var response = await _client.PostAsync($"/api/movie-lists/{listId}/movies/{movieId}", null);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task AddMovie_MovieNotFound_ShouldReturn404()
    {
        var (token, _) = await SeedUserAndTokenAsync("add3");
        _client.DefaultRequestHeaders.Authorization = Bearer(token);
        var listId = await SeedListAsync("My list");

        var response = await _client.PostAsync(
            $"/api/movie-lists/{listId}/movies/{Guid.NewGuid()}",
            null
        );

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task RemoveMovie_ShouldReturn204()
    {
        var (token, _) = await SeedUserAndTokenAsync("remove1");
        _client.DefaultRequestHeaders.Authorization = Bearer(token);
        var movieId = SeedMovie();
        var listId = await SeedListAsync("My list");
        await _client.PostAsync($"/api/movie-lists/{listId}/movies/{movieId}", null);

        var response = await _client.DeleteAsync($"/api/movie-lists/{listId}/movies/{movieId}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        _db.MovieListMovies.Count().Should().Be(0);
    }

    [Fact]
    public async Task SharedList_GroupMember_CanView()
    {
        var (ownerToken, ownerId) = await SeedUserAndTokenAsync("share1");
        var (memberToken, memberId) = await SeedUserAndTokenAsync("share2");
        var groupId = SeedGroup(ownerId, memberId);

        _client.DefaultRequestHeaders.Authorization = Bearer(ownerToken);
        var createResponse = await _client.PostAsJsonAsync(
            "/api/movie-lists",
            new CreateMovieListRequestDto
            {
                Name = "Shared",
                IsPrivate = false,
                GroupIds = [groupId],
            }
        );
        createResponse.EnsureSuccessStatusCode();
        var created = await createResponse.Content.ReadFromJsonAsync<MovieListResponseDto>();

        _client.DefaultRequestHeaders.Authorization = Bearer(memberToken);
        var response = await _client.GetAsync($"/api/movie-lists/{created!.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<MovieListResponseDto>();
        result!.IsOwner.Should().BeFalse();
        result.Name.Should().Be("Shared");
    }

    [Fact]
    public async Task SharedList_GroupEditor_CanAddMovie()
    {
        var (ownerToken, ownerId) = await SeedUserAndTokenAsync("share3");
        var (memberToken, memberId) = await SeedUserAndTokenAsync("share4");
        var groupId = SeedGroup(ownerId, memberId);
        var movieId = SeedMovie();

        _client.DefaultRequestHeaders.Authorization = Bearer(ownerToken);
        var createResponse = await _client.PostAsJsonAsync(
            "/api/movie-lists",
            new CreateMovieListRequestDto
            {
                Name = "Shared",
                IsPrivate = false,
                CanGroupEdit = true,
                GroupIds = [groupId],
            }
        );
        createResponse.EnsureSuccessStatusCode();
        var created = await createResponse.Content.ReadFromJsonAsync<MovieListResponseDto>();

        _client.DefaultRequestHeaders.Authorization = Bearer(memberToken);
        var response = await _client.PostAsync(
            $"/api/movie-lists/{created!.Id}/movies/{movieId}",
            null
        );

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task SharedList_NonEditor_AddMovie_ShouldReturn403()
    {
        var (ownerToken, ownerId) = await SeedUserAndTokenAsync("share5");
        var (memberToken, memberId) = await SeedUserAndTokenAsync("share6");
        var groupId = SeedGroup(ownerId, memberId);
        var movieId = SeedMovie();

        _client.DefaultRequestHeaders.Authorization = Bearer(ownerToken);
        var createResponse = await _client.PostAsJsonAsync(
            "/api/movie-lists",
            new CreateMovieListRequestDto
            {
                Name = "Shared",
                IsPrivate = false,
                CanGroupEdit = false,
                GroupIds = [groupId],
            }
        );
        createResponse.EnsureSuccessStatusCode();
        var created = await createResponse.Content.ReadFromJsonAsync<MovieListResponseDto>();

        _client.DefaultRequestHeaders.Authorization = Bearer(memberToken);
        var response = await _client.PostAsync(
            $"/api/movie-lists/{created!.Id}/movies/{movieId}",
            null
        );

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task PrivateList_OtherUser_ShouldReturn404()
    {
        var (ownerToken, _) = await SeedUserAndTokenAsync("priv1");
        _client.DefaultRequestHeaders.Authorization = Bearer(ownerToken);
        var listId = await SeedListAsync("My list");

        var (otherToken, _) = await SeedUserAndTokenAsync("priv2");
        _client.DefaultRequestHeaders.Authorization = Bearer(otherToken);

        var response = await _client.GetAsync($"/api/movie-lists/{listId}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Unauthenticated_ShouldReturn401()
    {
        _client.DefaultRequestHeaders.Authorization = null;

        var response = await _client.GetAsync($"/api/movie-lists/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private async Task<(string token, Guid userId)> SeedUserAndTokenAsync(string username)
    {
        var registerResponse = await _client.PostAsJsonAsync(
            "/api/auth/register",
            new RegisterRequestDto
            {
                Username = username,
                Email = $"{username}@test.com",
                Password = "Password123!",
            }
        );
        registerResponse.EnsureSuccessStatusCode();

        var loginResponse = await _client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequestDto { Email = $"{username}@test.com", Password = "Password123!" }
        );
        loginResponse.EnsureSuccessStatusCode();
        var loginResult = await loginResponse.Content.ReadFromJsonAsync<AuthResponseDto>();

        return (loginResult!.AccessToken, loginResult.User.Id);
    }

    private Guid SeedMovie()
    {
        var movieId = Guid.NewGuid();
        _db.Movies.Add(
            new Movie
            {
                Id = movieId,
                TmdbId = new Random().Next(10000, 99999),
                Title = "Test Movie",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            }
        );
        _db.SaveChanges();
        return movieId;
    }

    private Guid SeedGroup(Guid ownerId, Guid memberId)
    {
        var groupId = Guid.NewGuid();
        _db.Groups.Add(
            new Group
            {
                Id = groupId,
                Name = "Test Group",
                CreatedAt = DateTime.UtcNow,
            }
        );
        _db.UserGroups.AddRange(
            new UserGroup { Id = Guid.NewGuid(), GroupId = groupId, UserId = ownerId },
            new UserGroup { Id = Guid.NewGuid(), GroupId = groupId, UserId = memberId }
        );
        _db.SaveChanges();
        return groupId;
    }

    private async Task<Guid> SeedListAsync(string name)
    {
        var response = await _client.PostAsJsonAsync(
            "/api/movie-lists",
            new CreateMovieListRequestDto { Name = name }
        );
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<MovieListResponseDto>();
        return result!.Id;
    }

    private static System.Net.Http.Headers.AuthenticationHeaderValue Bearer(string token)
    {
        return new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
    }
}