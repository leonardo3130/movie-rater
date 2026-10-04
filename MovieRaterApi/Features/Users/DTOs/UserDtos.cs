namespace MovieRaterApi.Features.Users.DTOs;

public class UserSuggestionQuery
{
    public string? Prefix { get; set; }
    public int? Limit { get; set; }
}

public class UserSuggestionDto
{
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string? ProfilePictureUrl { get; set; }
}