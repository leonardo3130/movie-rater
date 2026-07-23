namespace MovieRaterApi.Features.Authentication.DTOs;

public class RegisterRequestDto
{
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class LoginRequestDto
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class RefreshRequestDto
{
    public string RefreshToken { get; set; } = string.Empty;
}

public class InvitePartnerRequestDto
{
    public string InviteeEmail { get; set; } = string.Empty;
}

public class AcceptInvitationRequestDto
{
    public string InviteToken { get; set; } = string.Empty;
}

public class AuthResponseDto
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public UserResponseDto User { get; set; } = null!;
}

public class UserResponseDto
{
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? ProfilePictureUrl { get; set; }
}

public class CurrentUserResponseDto
{
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? ProfilePictureUrl { get; set; }
    public Guid? CoupleId { get; set; }
    public UserResponseDto? Partner { get; set; }
}

public class InviteResponseDto
{
    public Guid InvitationId { get; set; }
    public string InviteToken { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
}