using MovieRaterApi.Features.Authentication.DTOs;

namespace MovieRaterApi.Features.Authentication.Interfaces;

public interface IAuthService
{
    Task<AuthResponseDto> RegisterAsync(RegisterRequestDto request);
    Task<AuthResponseDto> LoginAsync(LoginRequestDto request);
    Task<AuthResponseDto> RefreshAsync(string? refreshTokenCookie);
    Task LogoutAsync(Guid userId);
    Task<UserResponseDto> GetCurrentUserAsync(Guid userId);
}
