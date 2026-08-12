using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using MovieRaterApi.Data;
using MovieRaterApi.Data.Entities;
using MovieRaterApi.Features.Authentication.Interfaces;
using MovieRaterApi.Features.Authentication.Options;

namespace MovieRaterApi.Features.Authentication.Services;

public class TokenService : ITokenService
{
    private readonly ApplicationDbContext _db;
    private readonly JwtOptions _jwtOptions;
    private readonly ILogger<TokenService> _logger;

    public TokenService(
        ApplicationDbContext db,
        IOptions<JwtOptions> jwtOptions,
        ILogger<TokenService> logger
    )
    {
        _db = db;
        _jwtOptions = jwtOptions.Value;
        _logger = logger;
    }

    public string GenerateAccessToken(User user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.SigningKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Username),
            new(ClaimTypes.Email, user.Email),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };

        var token = new JwtSecurityToken(
            issuer: _jwtOptions.Issuer,
            audience: _jwtOptions.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_jwtOptions.AccessTokenMinutes),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public (string rawToken, string tokenHash) GenerateRefreshToken()
    {
        var rawToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        var tokenHash = HashToken(rawToken);
        return (rawToken, tokenHash);
    }

    public string HashToken(string rawToken)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawToken));
        return Convert.ToBase64String(bytes);
    }

    public async Task<bool> IsRefreshTokenValidAsync(string tokenHash)
    {
        return await _db.Set<RefreshToken>()
            .AnyAsync(rt =>
                rt.TokenHash == tokenHash
                && !rt.RevokedAt.HasValue
                && rt.ExpiresAt > DateTime.UtcNow
            );
    }

    public async Task RevokeRefreshTokenAsync(string tokenHash, string? replacedByTokenHash = null)
    {
        var token = await _db.Set<RefreshToken>()
            .FirstOrDefaultAsync(rt => rt.TokenHash == tokenHash);
        if (token is not null)
        {
            token.RevokedAt = DateTime.UtcNow;
            token.ReplacedByTokenHash = replacedByTokenHash;
            await _db.SaveChangesAsync();
        }
    }

    public async Task RevokeTokenFamilyAsync(string tokenHash)
    {
        var tokens = await _db.Set<RefreshToken>()
            .Where(rt => rt.TokenHash == tokenHash || rt.ReplacedByTokenHash == tokenHash)
            .ToListAsync();

        foreach (var token in tokens)
        {
            token.RevokedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();

        _logger.LogWarning("Revoked refresh token family for hash {TokenHash}", tokenHash);
    }

    public async Task RevokeTokensFromUser(Guid userId)
    {
        List<RefreshToken> userTokens = await _db
            .RefreshTokens.Where(rt => rt.UserId == userId && rt.RevokedAt != null)
            .ToListAsync();

        foreach (var token in userTokens)
        {
            token.RevokedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();
    }
}
