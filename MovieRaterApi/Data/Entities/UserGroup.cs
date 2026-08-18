namespace MovieRaterApi.Data.Entities;

public class UserGroup
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }
    public Guid GroupId { get; set; }

    public User User { get; set; } = null!;
    public Group Group { get; set; } = null!;
}
