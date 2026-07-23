using Microsoft.EntityFrameworkCore;
using MovieRaterApi.Data;
using MovieRaterApi.Data.Entities;
using MovieRaterApi.Features.Authentication.DTOs;
using MovieRaterApi.Features.Authentication.Interfaces;

namespace MovieRaterApi.Features.Authentication.Services;

public class AuthService : IAuthService
{
    private readonly ApplicationDbContext _db;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        ApplicationDbContext db,
        IPasswordHasher passwordHasher,
        ITokenService tokenService,
        ILogger<AuthService> logger)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
        _logger = logger;
    }

    public async Task<AuthResponseDto> RegisterAsync(RegisterRequestDto request)
    {
        var existingUser = await _db.Users
            .FirstOrDefaultAsync(u => u.Username == request.Username || u.Email == request.Email);

        if (existingUser is not null)
        {
            throw new InvalidOperationException("A user with this username or email already exists.");
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = request.Username,
            Email = request.Email,
            PasswordHash = _passwordHasher.Hash(request.Password),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        var accessToken = _tokenService.GenerateAccessToken(user, coupleId: null);
        var (rawRefreshToken, refreshTokenHash) = _tokenService.GenerateRefreshToken();

        var refreshTokenEntity = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = refreshTokenHash,
            ExpiresAt = DateTime.UtcNow.AddDays(30),
            CreatedAt = DateTime.UtcNow
        };

        _db.Set<RefreshToken>().Add(refreshTokenEntity);
        await _db.SaveChangesAsync();

        _logger.LogInformation("User {UserId} registered", user.Id);

        return new AuthResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = rawRefreshToken,
            User = new UserResponseDto
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                ProfilePictureUrl = user.ProfilePictureUrl
            }
        };
    }

    public async Task<AuthResponseDto> LoginAsync(LoginRequestDto request)
    {
        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Email == request.Email);

        if (user is null || !_passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            _logger.LogWarning("Invalid login attempt for {Email}", request.Email);
            throw new UnauthorizedAccessException("Invalid credentials.");
        }

        var couple = await _db.Couples
            .FirstOrDefaultAsync(c => c.User1Id == user.Id || c.User2Id == user.Id);

        var accessToken = _tokenService.GenerateAccessToken(user, couple?.Id);
        var (rawRefreshToken, refreshTokenHash) = _tokenService.GenerateRefreshToken();

        var refreshTokenEntity = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = refreshTokenHash,
            ExpiresAt = DateTime.UtcNow.AddDays(30),
            CreatedAt = DateTime.UtcNow
        };

        _db.Set<RefreshToken>().Add(refreshTokenEntity);
        await _db.SaveChangesAsync();

        _logger.LogInformation("User {UserId} logged in", user.Id);

        return new AuthResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = rawRefreshToken,
            User = new UserResponseDto
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                ProfilePictureUrl = user.ProfilePictureUrl
            }
        };
    }

    public async Task<AuthResponseDto> RefreshAsync(string? refreshTokenCookie)
    {
        if (string.IsNullOrWhiteSpace(refreshTokenCookie))
        {
            throw new UnauthorizedAccessException("Refresh token is required.");
        }

        var tokenHash = _tokenService.HashToken(refreshTokenCookie);
        var storedToken = await _db.Set<RefreshToken>()
            .Include(rt => rt.User)
            .FirstOrDefaultAsync(rt => rt.TokenHash == tokenHash);

        if (storedToken is null)
        {
            throw new UnauthorizedAccessException("Invalid refresh token.");
        }

        if (storedToken.RevokedAt.HasValue)
        {
            _logger.LogWarning("Refresh token reuse detected for user {UserId}", storedToken.UserId);
            await _tokenService.RevokeTokenFamilyAsync(tokenHash);
            throw new UnauthorizedAccessException("Refresh token has been revoked.");
        }

        if (storedToken.ExpiresAt <= DateTime.UtcNow)
        {
            storedToken.RevokedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            throw new UnauthorizedAccessException("Refresh token has expired.");
        }

        var couple = await _db.Couples
            .FirstOrDefaultAsync(c => c.User1Id == storedToken.UserId || c.User2Id == storedToken.UserId);

        var newAccessToken = _tokenService.GenerateAccessToken(storedToken.User, couple?.Id);
        var (newRawRefreshToken, newRefreshTokenHash) = _tokenService.GenerateRefreshToken();

        storedToken.RevokedAt = DateTime.UtcNow;
        storedToken.ReplacedByTokenHash = newRefreshTokenHash;

        var newRefreshTokenEntity = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = storedToken.UserId,
            TokenHash = newRefreshTokenHash,
            ExpiresAt = DateTime.UtcNow.AddDays(30),
            CreatedAt = DateTime.UtcNow
        };

        _db.Set<RefreshToken>().Add(newRefreshTokenEntity);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Refresh rotated for user {UserId}", storedToken.UserId);

        return new AuthResponseDto
        {
            AccessToken = newAccessToken,
            RefreshToken = newRawRefreshToken,
            User = new UserResponseDto
            {
                Id = storedToken.User.Id,
                Username = storedToken.User.Username,
                Email = storedToken.User.Email,
                ProfilePictureUrl = storedToken.User.ProfilePictureUrl
            }
        };
    }

    public async Task LogoutAsync(Guid userId)
    {
        var activeTokens = await _db.Set<RefreshToken>()
            .Where(rt => rt.UserId == userId && !rt.RevokedAt.HasValue)
            .ToListAsync();

        var now = DateTime.UtcNow;
        foreach (var token in activeTokens)
        {
            token.RevokedAt = now;
        }

        await _db.SaveChangesAsync();

        _logger.LogInformation("User {UserId} logged out", userId);
    }

    public async Task<CurrentUserResponseDto> GetCurrentUserAsync(Guid userId)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId);

        if (user is null)
        {
            throw new InvalidOperationException("User not found.");
        }

        var couple = await _db.Couples
            .FirstOrDefaultAsync(c => c.User1Id == userId || c.User2Id == userId);

        UserResponseDto? partner = null;
        if (couple is not null)
        {
            var partnerId = couple.User1Id == userId ? couple.User2Id : couple.User1Id;
            var partnerUser = await _db.Users.FirstOrDefaultAsync(u => u.Id == partnerId);
            if (partnerUser is not null)
            {
                partner = new UserResponseDto
                {
                    Id = partnerUser.Id,
                    Username = partnerUser.Username,
                    Email = partnerUser.Email,
                    ProfilePictureUrl = partnerUser.ProfilePictureUrl
                };
            }
        }

        return new CurrentUserResponseDto
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            ProfilePictureUrl = user.ProfilePictureUrl,
            CoupleId = couple?.Id,
            Partner = partner
        };
    }
}