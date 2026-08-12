using MovieRaterApi.Data.Entities;

namespace MovieRaterApi.Features.Authentication.Interfaces;

public interface ITokenService
{
    string GenerateAccessToken(User user);
    (string rawToken, string tokenHash) GenerateRefreshToken();
    string HashToken(string rawToken);
    Task<bool> IsRefreshTokenValidAsync(string tokenHash);
    Task RevokeRefreshTokenAsync(string tokenHash, string? replacedByTokenHash = null);
    Task RevokeTokenFamilyAsync(string tokenHash);
    Task RevokeTokensFromUser(Guid userId);
}
