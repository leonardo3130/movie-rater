namespace MovieRaterApi.Data.Entities;

public class UserGroup
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }
    public Guid GroupId { get; set; }

    public User User { get; set; } = new User();
    public Group Group { get; set; } = new Group();
}
