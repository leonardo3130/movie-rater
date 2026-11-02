using Microsoft.EntityFrameworkCore;
using MovieRaterApi.Data;
using MovieRaterApi.Data.Entities;

namespace MovieRaterApi.Features.Shared.Authorization;

/// <summary>
/// Shared resource-level access rules, expressed as LINQ so every feature filters data
/// the same way and the check runs inside the database query.
/// </summary>
public static class AccessQueryExtensions
{
    /// <summary>
    /// A watch session is visible to its creator and, when it belongs to a group, to every
    /// member of that group. Personal sessions (no group) are visible to the creator only.
    /// </summary>
    public static IQueryable<WatchSession> VisibleTo(
        this IQueryable<WatchSession> sessions,
        Guid userId
    ) =>
        sessions.Where(ws =>
            ws.CreatedByUserId == userId
            || (ws.GroupId != null && ws.Group!.UserGroups.Any(ug => ug.UserId == userId))
        );

    public static Task<bool> IsGroupMemberAsync(
        this ApplicationDbContext db,
        Guid groupId,
        Guid userId,
        CancellationToken ct = default
    ) => db.UserGroups.AnyAsync(ug => ug.GroupId == groupId && ug.UserId == userId, ct);
}
