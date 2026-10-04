using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using MovieRaterApi.Data;
using MovieRaterApi.Data.Entities;
using MovieRaterApi.Features.Users.DTOs;
using MovieRaterApi.Features.Users.Interfaces;

namespace MovieRaterApi.Features.Users.Services;

public class UserSuggestionService : IUserSuggestionService
{
    private static readonly string CacheKeyPrefix = "user-suggestions:";
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);
    private static readonly int DefaultLimit = 8;
    private static readonly int MaxLimit = 20;

    private readonly ApplicationDbContext _db;
    private readonly IMemoryCache _cache;
    private readonly ILogger<UserSuggestionService> _logger;

    public UserSuggestionService(
        ApplicationDbContext db,
        IMemoryCache cache,
        ILogger<UserSuggestionService> logger
    )
    {
        _db = db;
        _cache = cache;
        _logger = logger;
    }

    public async Task<ICollection<UserSuggestionDto>> SuggestAsync(
        Guid currentUserId,
        UserSuggestionQuery query
    )
    {
        var prefix = query.Prefix?.Trim() ?? string.Empty;

        if (prefix.Length == 0)
        {
            return new List<UserSuggestionDto>();
        }

        var requestedLimit = query.Limit ?? DefaultLimit;
        var limit = Math.Min(MaxLimit, Math.Max(1, requestedLimit));

        // The cache is shared across users, so the key only depends on the (lowercased) prefix.
        // This way every keystroke of every user hits the database at most once per TTL.
        var cacheKey = $"{CacheKeyPrefix}{prefix.ToLower()}";

        var suggestions = await _cache.GetOrCreateAsync(
                cacheKey,
                async entry =>
                {
                    entry.AbsoluteExpirationRelativeToNow = CacheTtl;
                    _logger.LogInformation(
                        "Username suggestions for prefix {Prefix} not cached, querying database",
                        prefix
                    );
                    return await SearchDatabaseAsync(prefix);
                }
            ) ?? new List<UserSuggestionDto>();

        var filtered = suggestions
            .Where(s => s.Id != currentUserId)
            .Take(limit)
            .ToList();

        _logger.LogInformation(
            "Returning {SuggestionCount} username suggestions for prefix {Prefix}",
            filtered.Count,
            prefix
        );

        return filtered;
    }

    private async Task<List<UserSuggestionDto>> SearchDatabaseAsync(string prefix)
    {
        var lowerPattern = prefix.ToLower();

        return await _db.Users
            .Where(u => u.Username.ToLower().StartsWith(lowerPattern))
            .OrderBy(u => u.Username)
            .Take(MaxLimit)
            .Select(u => new UserSuggestionDto
            {
                Id = u.Id,
                Username = u.Username,
                ProfilePictureUrl = u.ProfilePictureUrl,
            })
            .ToListAsync();
    }
}