using MovieRaterApi.Data.Entities;
using MovieRaterApi.Features.Authentication.DTOs;

namespace MovieRaterApi.Features.Groups.DTOs;

public class CreateGroupRequest
{
    public string GroupName { get; set; } = String.Empty;
}

public class GroupDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = String.Empty;
    public DateTime CreatedAt { get; set; }

    public ICollection<UserResponseDto> Users { get; set; } = new List<UserResponseDto>();
    public ICollection<WatchSession> WatchSessions { get; set; } = new List<WatchSession>();
}

public class InvitationResponseDto
{
    public Guid InvitationId { get; set; }
    public string InviteToken { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
}

public class AcceptInvitationResponseDto
{
    public Guid GroupId { get; set; }
}

public class InvitationRequestDto
{
    public Guid GroupId { get; set; }
    public string InviteeEmail { get; set; } = string.Empty;
}

public class AcceptInvitationRequestDto
{
    public string InviteToken { get; set; } = string.Empty;
}
