using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using MovieRaterApi.Data;
using MovieRaterApi.Data.Entities;
using MovieRaterApi.Features.MovieLists.DTOs;
using MovieRaterApi.Features.MovieLists.Interfaces;
using MovieRaterApi.Features.Movies.Mapping;
using MovieRaterApi.Infrastructure.Exceptions;
using MovieRaterApi.Infrastructure.Tmdb;
using MovieRaterApi.Infrastructure.Tmdb.Dtos.Responses;

namespace MovieRaterApi.Features.MovieLists.Services;

public class MovieListService : IMovieListService
{
    private static readonly string ImageConfigCacheKey = "TmdbImageConfig";

    private readonly ApplicationDbContext _db;
    private readonly ITmdbClient _tmdb;
    private readonly IMemoryCache _cache;
    private readonly ILogger<MovieListService> _logger;

    public MovieListService(
        ApplicationDbContext db,
        ITmdbClient tmdb,
        IMemoryCache cache,
        ILogger<MovieListService> logger
    )
    {
        _db = db;
        _tmdb = tmdb;
        _cache = cache;
        _logger = logger;
    }

    public async Task<MovieListResponseDto> CreateAsync(
        CreateMovieListRequestDto request,
        Guid userId
    )
    {
        if (request.CanGroupEdit && request.IsPrivate)
            throw new BadRequestException("CanGroupEdit requires the list to be shared.");

        var groupIds = request.IsPrivate
            ? []
            : await ValidateShareGroupsAsync(request.GroupIds, userId);

        var now = DateTime.UtcNow;
        var list = new MovieList
        {
            Id = Guid.NewGuid(),
            OwnerUserId = userId,
            Name = request.Name,
            Description = request.Description,
            IsPrivate = request.IsPrivate,
            CanGroupEdit = request.CanGroupEdit,
            CreatedAt = now,
            LastUpdatedAt = now,
        };

        _db.MovieLists.Add(list);

        foreach (var groupId in groupIds)
        {
            _db.MovieListGroups.Add(
                new MovieListGroup { MovieListId = list.Id, GroupId = groupId }
            );
        }

        await _db.SaveChangesAsync();

        _logger.LogInformation(
            "Movie list {MovieListId} created by user {UserId} (isPrivate={IsPrivate}, canGroupEdit={CanGroupEdit})",
            list.Id,
            userId,
            list.IsPrivate,
            list.CanGroupEdit
        );

        return ToResponseDto(list, true, [], groupIds);
    }

    public async Task<MovieListResponseDto> UpdateAsync(
        Guid listId,
        UpdateMovieListRequestDto request,
        Guid userId
    )
    {
        var list = await _db
            .MovieLists.Include(l => l.Movies)
                .ThenInclude(m => m.Movie)
            .Include(l => l.SharedGroups)
            .FirstOrDefaultAsync(l => l.Id == listId);

        if (list is null)
            throw new NotFoundException("Movie list not found.");

        if (list.OwnerUserId != userId)
            throw new ForbiddenException("You can only edit your own movie lists.");

        if (request.CanGroupEdit && request.IsPrivate)
            throw new BadRequestException("CanGroupEdit requires the list to be shared.");

        var groupIds = request.IsPrivate
            ? []
            : await ValidateShareGroupsAsync(request.GroupIds, userId);

        list.Name = request.Name;
        list.Description = request.Description;
        list.IsPrivate = request.IsPrivate;
        list.CanGroupEdit = request.CanGroupEdit;
        list.LastUpdatedAt = DateTime.UtcNow;

        foreach (var sharedGroup in list.SharedGroups.ToList())
            _db.MovieListGroups.Remove(sharedGroup);

        foreach (var groupId in groupIds)
        {
            _db.MovieListGroups.Add(new MovieListGroup { MovieListId = listId, GroupId = groupId });
        }

        await _db.SaveChangesAsync();

        _logger.LogInformation("Movie list {MovieListId} updated by user {UserId}", listId, userId);

        var items = await BuildMovieItemsAsync(list);

        return ToResponseDto(list, true, items, groupIds);
    }

    public async Task<MovieListResponseDto> GetAsync(Guid listId, Guid userId)
    {
        var list = await _db
            .MovieLists.Include(l => l.Movies)
                .ThenInclude(m => m.Movie)
            .Include(l => l.SharedGroups)
            .FirstOrDefaultAsync(l => l.Id == listId);

        if (list is null)
            throw new NotFoundException("Movie list not found.");

        var userGroupIds = await GetUserGroupIdsAsync(userId);

        if (!IsVisibleTo(list, userId, userGroupIds))
            throw new NotFoundException("Movie list not found.");

        var isOwner = list.OwnerUserId == userId;

        var items = await BuildMovieItemsAsync(list);

        _logger.LogInformation(
            "Retrieved movie list {MovieListId} for user {UserId} with {MovieCount} movies",
            listId,
            userId,
            items.Count
        );

        return ToResponseDto(list, isOwner, items);
    }

    public async Task<List<MovieListSummaryDto>> GetAllAsync(Guid userId)
    {
        var userGroupIds = await GetUserGroupIdsAsync(userId);

        var lists = await _db
            .MovieLists.Include(l => l.SharedGroups)
            .Where(l =>
                l.OwnerUserId == userId
                || (!l.IsPrivate && l.SharedGroups.Any(sg => userGroupIds.Contains(sg.GroupId)))
            )
            .OrderByDescending(l => l.LastUpdatedAt)
            .ToListAsync();

        var listIds = lists.Select(l => l.Id).ToList();

        var counts = listIds.ToDictionary(id => id, id => 0);
        if (listIds.Count > 0)
        {
            var rows = await _db
                .MovieListMovies.Where(m => listIds.Contains(m.MovieListId))
                .GroupBy(m => m.MovieListId)
                .Select(g => new { MovieListId = g.Key, Count = g.Count() })
                .ToListAsync();

            foreach (var row in rows)
                counts[row.MovieListId] = row.Count;
        }

        var items = lists
            .Select(l => new MovieListSummaryDto
            {
                Id = l.Id,
                Name = l.Name,
                Description = l.Description,
                IsPrivate = l.IsPrivate,
                CanGroupEdit = l.CanGroupEdit,
                IsOwner = l.OwnerUserId == userId,
                MovieCount = counts[l.Id],
                GroupIds = l.SharedGroups.Select(sg => sg.GroupId).ToList(),
                CreatedAt = l.CreatedAt,
                LastUpdatedAt = l.LastUpdatedAt,
            })
            .ToList();

        _logger.LogInformation(
            "Retrieved {ListCount} movie lists for user {UserId}",
            items.Count,
            userId
        );

        return items;
    }

    public async Task DeleteAsync(Guid listId, Guid userId)
    {
        var list = await _db.MovieLists.FirstOrDefaultAsync(l => l.Id == listId);

        if (list is null)
            throw new NotFoundException("Movie list not found.");

        if (list.OwnerUserId != userId)
            throw new ForbiddenException("You can only delete your own movie lists.");

        _db.MovieLists.Remove(list);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Movie list {MovieListId} deleted by user {UserId}", listId, userId);
    }

    public async Task AddMovieAsync(Guid listId, Guid movieId, Guid userId)
    {
        var list = await GetEditableListAsync(listId, userId);

        var movieExists = await _db.Movies.AnyAsync(m => m.Id == movieId);
        if (!movieExists)
            throw new NotFoundException("Movie not found.");

        var alreadyInList = await _db.MovieListMovies.AnyAsync(m =>
            m.MovieListId == listId && m.MovieId == movieId
        );
        if (alreadyInList)
            throw new ConflictException("Movie is already in this list.");

        _db.MovieListMovies.Add(
            new MovieListMovie
            {
                MovieListId = listId,
                MovieId = movieId,
                CreatedAt = DateTime.UtcNow,
            }
        );

        list.LastUpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        _logger.LogInformation(
            "User {UserId} added movie {MovieId} to list {MovieListId}",
            userId,
            movieId,
            listId
        );
    }

    public async Task RemoveMovieAsync(Guid listId, Guid movieId, Guid userId)
    {
        var list = await GetEditableListAsync(listId, userId);

        var association = await _db.MovieListMovies.FirstOrDefaultAsync(m =>
            m.MovieListId == listId && m.MovieId == movieId
        );
        if (association is null)
            throw new NotFoundException("Movie is not in this list.");

        _db.MovieListMovies.Remove(association);

        list.LastUpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        _logger.LogInformation(
            "User {UserId} removed movie {MovieId} from list {MovieListId}",
            userId,
            movieId,
            listId
        );
    }

    private async Task<MovieList> GetEditableListAsync(Guid listId, Guid userId)
    {
        var list = await _db
            .MovieLists.Include(l => l.SharedGroups)
            .FirstOrDefaultAsync(l => l.Id == listId);

        if (list is null)
            throw new NotFoundException("Movie list not found.");

        var userGroupIds = await GetUserGroupIdsAsync(userId);

        if (!CanEdit(list, userId, userGroupIds))
            throw new ForbiddenException("You are not allowed to edit this movie list.");

        return list;
    }

    private async Task<List<Guid>> ValidateShareGroupsAsync(List<Guid> groupIds, Guid userId)
    {
        var uniqueIds = new List<Guid>();
        foreach (var id in groupIds)
        {
            if (!uniqueIds.Contains(id))
                uniqueIds.Add(id);
        }

        if (uniqueIds.Count == 0)
            throw new BadRequestException("At least one group is required when sharing a list.");

        var userGroupIds = await _db
            .UserGroups.Where(ug => ug.UserId == userId && uniqueIds.Contains(ug.GroupId))
            .Select(ug => ug.GroupId)
            .ToListAsync();

        if (uniqueIds.Any(id => !userGroupIds.Contains(id)))
            throw new BadRequestException("Can only share a list with groups you are a member of.");

        return uniqueIds;
    }

    private async Task<List<Guid>> GetUserGroupIdsAsync(Guid userId)
    {
        return await _db
            .UserGroups.Where(ug => ug.UserId == userId)
            .Select(ug => ug.GroupId)
            .ToListAsync();
    }

    private static bool IsVisibleTo(MovieList list, Guid userId, List<Guid> userGroupIds)
    {
        return list.OwnerUserId == userId
            || (!list.IsPrivate && list.SharedGroups.Any(sg => userGroupIds.Contains(sg.GroupId)));
    }

    private static bool CanEdit(MovieList list, Guid userId, List<Guid> userGroupIds)
    {
        return list.OwnerUserId == userId
            || (
                !list.IsPrivate
                && list.CanGroupEdit
                && list.SharedGroups.Any(sg => userGroupIds.Contains(sg.GroupId))
            );
    }

    private async Task<TmdbImageConfig> GetImageConfigAsync(CancellationToken ct = default)
    {
        return await _cache.GetOrCreateAsync(
                ImageConfigCacheKey,
                async entry =>
                {
                    entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24);
                    _logger.LogDebug("Fetching TMDB image configuration");
                    var config = await _tmdb.GetConfigurationAsync(ct);
                    return config.Images;
                }
            ) ?? new TmdbImageConfig { SecureBaseUrl = "https://image.tmdb.org/t/p/" };
    }

    private async Task<List<MovieListItemDto>> BuildMovieItemsAsync(MovieList list)
    {
        var ordered = list.Movies.OrderBy(m => m.CreatedAt).ToList();

        if (ordered.Count == 0)
            return [];

        var baseUrl = ordered.Any(m => m.Movie.PosterUrl is not null || m.Movie.BackdropUrl is not null)
            ? (await GetImageConfigAsync()).SecureBaseUrl
            : "https://image.tmdb.org/t/p/";

        return ordered
            .Select(m => new MovieListItemDto
            {
                Id = m.Movie.Id,
                TmdbId = m.Movie.TmdbId,
                Title = m.Movie.Title,
                PosterUrl = MovieMapper.BuildPosterUrl(
                    m.Movie.PosterUrl,
                    baseUrl
                ),
                BackdropUrl = MovieMapper.BuildBackdropUrl(
                    m.Movie.BackdropUrl,
                    baseUrl
                ),
                ReleaseDate = m.Movie.ReleaseDate?.ToString("yyyy-MM-dd"),
                VoteAverage = m.Movie.AverageTmdbRating,
                AddedAt = m.CreatedAt,
            })
            .ToList();
    }

    private static MovieListResponseDto ToResponseDto(
        MovieList list,
        bool isOwner,
        List<MovieListItemDto> items,
        List<Guid>? groupIds = null
    )
    {
        return new MovieListResponseDto
        {
            Id = list.Id,
            Name = list.Name,
            Description = list.Description,
            IsPrivate = list.IsPrivate,
            CanGroupEdit = list.CanGroupEdit,
            IsOwner = isOwner,
            GroupIds = groupIds ?? list.SharedGroups.Select(sg => sg.GroupId).ToList(),
            CreatedAt = list.CreatedAt,
            LastUpdatedAt = list.LastUpdatedAt,
            Movies = items,
        };
    }
}
