using MovieRaterApi.Features.Groups.DTOs;

namespace MovieRaterApi.Features.Groups.Interfaces;

public interface IGroupService
{
    public Task<GroupDto> CreateGroup(CreateGroupRequest request);
    public Task<GroupDto> ChangeGroupName(Guid groupId, string newName);
    public Task DeleteGroup(Guid groupId);
    public Task<ICollection<GroupDto>> GetGroups();
    public Task<GroupDto?> GetGroup(Guid groupId);
}
