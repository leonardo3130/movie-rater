using Microsoft.EntityFrameworkCore;
using MovieRaterApi.Data;
using MovieRaterApi.Data.Entities;
using MovieRaterApi.Features.Authentication.DTOs;
using MovieRaterApi.Features.Authentication.Interfaces;
using MovieRaterApi.Infrastructure.Exceptions;

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
        ILogger<AuthService> logger
    )
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
        _logger = logger;
    }

    public async Task<AuthResponseDto> RegisterAsync(RegisterRequestDto request)
    {
        var existingUser = await _db.Users.FirstOrDefaultAsync(u =>
            u.Username == request.Username || u.Email == request.Email
        );

        if (existingUser is not null)
        {
            throw new ConflictException("A user with this username or email already exists.");
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = request.Username,
            Email = request.Email,
            PasswordHash = _passwordHasher.Hash(request.Password),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        var accessToken = _tokenService.GenerateAccessToken(user);
        var (rawRefreshToken, refreshTokenHash) = _tokenService.GenerateRefreshToken();

        var refreshTokenEntity = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = refreshTokenHash,
            ExpiresAt = DateTime.UtcNow.AddDays(30),
            CreatedAt = DateTime.UtcNow,
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
                ProfilePictureUrl = user.ProfilePictureUrl,
            },
        };
    }

    public async Task<AuthResponseDto> LoginAsync(LoginRequestDto request)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == request.Email);

        if (user is null || !_passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            _logger.LogWarning("Invalid login attempt for {Email}", request.Email);
            throw new UnauthorizedAccessException("Invalid credentials.");
        }

        var accessToken = _tokenService.GenerateAccessToken(user);
        var (rawRefreshToken, refreshTokenHash) = _tokenService.GenerateRefreshToken();

        var refreshTokenEntity = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = refreshTokenHash,
            ExpiresAt = DateTime.UtcNow.AddDays(30),
            CreatedAt = DateTime.UtcNow,
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
                ProfilePictureUrl = user.ProfilePictureUrl,
            },
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
            _logger.LogWarning(
                "Refresh token reuse detected for user {UserId}",
                storedToken.UserId
            );
            await _tokenService.RevokeTokenFamilyAsync(tokenHash);
            throw new UnauthorizedAccessException("Refresh token has been revoked.");
        }

        if (storedToken.ExpiresAt <= DateTime.UtcNow)
        {
            storedToken.RevokedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            throw new UnauthorizedAccessException("Refresh token has expired.");
        }

        var newAccessToken = _tokenService.GenerateAccessToken(storedToken.User);
        var (newRawRefreshToken, newRefreshTokenHash) = _tokenService.GenerateRefreshToken();

        storedToken.RevokedAt = DateTime.UtcNow;
        storedToken.ReplacedByTokenHash = newRefreshTokenHash;

        var newRefreshTokenEntity = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = storedToken.UserId,
            TokenHash = newRefreshTokenHash,
            ExpiresAt = DateTime.UtcNow.AddDays(30),
            CreatedAt = DateTime.UtcNow,
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
                ProfilePictureUrl = storedToken.User.ProfilePictureUrl,
            },
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
            throw new NotFoundException("User not found.");
        }

        return new CurrentUserResponseDto
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            ProfilePictureUrl = user.ProfilePictureUrl,
        };
    }
}
