namespace MovieRaterApi.Features.Groups.DTOs;

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
