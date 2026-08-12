namespace MovieRaterApi.Data.Entities;

public enum InvitationStatus
{
    Pending,
    Accepted,
    Expired,
    Revoked,
}

public class Invitation
{
    public Guid Id { get; set; }
    public Guid GroupId { get; set; }
    public Guid InviterUserId { get; set; }
    public string InviteeEmail { get; set; } = string.Empty;
    public string InviteTokenHash { get; set; } = string.Empty;
    public InvitationStatus Status { get; set; }
    public DateTime ExpiresAt { get; set; }
    public Guid? AcceptedByUserId { get; set; }
    public DateTime CreatedAt { get; set; }

    public User InviterUser { get; set; } = null!;
    public User? AcceptedByUser { get; set; }
}
