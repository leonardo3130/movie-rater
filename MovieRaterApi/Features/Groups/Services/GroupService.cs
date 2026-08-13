using Microsoft.EntityFrameworkCore;
using MovieRaterApi.Data;
using MovieRaterApi.Data.Entities;
using MovieRaterApi.Features.Authentication.DTOs;
using MovieRaterApi.Features.Authentication.Infrastructure;
using MovieRaterApi.Features.Groups.DTOs;
using MovieRaterApi.Features.Groups.Interfaces;
using MovieRaterApi.Infrastructure.Exceptions;

public class GroupService : IGroupService
{
    private readonly ApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<GroupService> _logger;

    public GroupService(
        ApplicationDbContext db,
        ILogger<GroupService> logger,
        ICurrentUser currentUser
    )
    {
        _db = db;

        _currentUser = currentUser;
        _logger = logger;
    }

    private GroupDto MapGroup(Group group)
    {
        var users = group
            .UserGroups.Select(ug => new UserResponseDto
            {
                Id = ug.UserId,
                Username = ug.User.Username,
                Email = ug.User.Email,
                ProfilePictureUrl = ug.User.ProfilePictureUrl,
            })
            .ToList();

        return new GroupDto
        {
            Id = group.Id,
            Name = group.Name,
            CreatedAt = group.CreatedAt,

            Users = users,
            WatchSessions = group.WatchSessions,
        };
    }

    public async Task<GroupDto> CreateGroup(CreateGroupRequest request)
    {
        var group = new Group
        {
            Id = Guid.NewGuid(),
            Name = request.GroupName,
            CreatedAt = DateTime.UtcNow,
        };
        var currentUserGroup = new UserGroup { GroupId = group.Id, UserId = _currentUser.UserId };

        await _db.Groups.AddAsync(group);
        await _db.UserGroups.AddAsync(currentUserGroup);

        await _db.SaveChangesAsync();

        return new GroupDto
        {
            Id = group.Id,
            Name = group.Name,
            CreatedAt = group.CreatedAt,
        };
    }

    public async Task<GroupDto> ChangeGroupName(Guid groupId, string newName)
    {
        var group = await _db.Groups.Where(g => g.Id == groupId).FirstOrDefaultAsync();

        if (group is null)
        {
            _logger.LogWarning("Group with Id {GroupId} was not found", groupId);
            throw new NotFoundException("Group not found");
        }

        group.Name = newName;

        _logger.LogInformation("Group With {GroupId} name updated to {newName}", groupId, newName);
        await _db.SaveChangesAsync();

        return MapGroup(group);
    }

    public async Task DeleteGroup(Guid groupId)
    {
        var group = await _db.Groups.Where(g => g.Id == groupId).FirstOrDefaultAsync();

        if (group is null)
        {
            _logger.LogWarning("Group with Id {GroupId} was not found", groupId);
            throw new NotFoundException("Group not found");
        }

        _logger.LogInformation("Group With {GroupId} deleted successfully", groupId);

        _db.Remove(group);
        await _db.SaveChangesAsync();
    }

    public async Task<ICollection<GroupDto>> GetGroups()
    {
        var currentUserId = _currentUser.UserId;

        var groups = await _db
            .Groups.Where(g => g.UserGroups.Any(ug => ug.UserId == currentUserId))
            .ToListAsync();

        _logger.LogInformation("Returning {GroupCount} groups successfully", groups.Count);

        return groups.Select(g => MapGroup(g)).ToList();
    }

    public async Task<GroupDto?> GetGroup(Guid groupId)
    {
        var currentUserId = _currentUser.UserId;
        var group = await _db
            .Groups.Where(g =>
                g.Id == groupId && g.UserGroups.Any(ug => ug.UserId == currentUserId)
            )
            .FirstOrDefaultAsync();

        if (group is null)
        {
            _logger.LogWarning("Group with Id {GroupId} was not found", groupId);
            throw new NotFoundException("Group not found");
        }

        _logger.LogInformation("Returning group With {GroupId} successfully", groupId);

        return MapGroup(group);
    }
}
