using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using MovieRaterApi.Data;
using MovieRaterApi.Data.Entities;
using MovieRaterApi.Features.Authentication.DTOs;
using MovieRaterApi.Features.Authentication.Interfaces;
using MovieRaterApi.Features.Authentication.Services;
using MovieRaterApi.Infrastructure.Exceptions;

namespace MovieRaterApi.Tests.Unit.Services;

public class AuthServiceTests
{
    private readonly ApplicationDbContext _db;
    private readonly Mock<IPasswordHasher> _passwordHasherMock;
    private readonly Mock<ITokenService> _tokenServiceMock;
    private readonly Mock<ILogger<AuthService>> _loggerMock;
    private readonly AuthService _sut;

    public AuthServiceTests()
    {
        _db = TestHelpers.CreateInMemoryDbContext();
        _passwordHasherMock = new Mock<IPasswordHasher>();
        _tokenServiceMock = new Mock<ITokenService>();
        _loggerMock = new Mock<ILogger<AuthService>>();

        _sut = new AuthService(
            _db,
            _passwordHasherMock.Object,
            _tokenServiceMock.Object,
            _loggerMock.Object
        );
    }

    [Fact]
    public async Task RegisterAsync_ShouldCreateUserAndReturnTokens()
    {
        _passwordHasherMock.Setup(p => p.Hash("ValidPass1!")).Returns("hashed-password");
        _tokenServiceMock
            .Setup(t => t.GenerateAccessToken(It.IsAny<User>()))
            .Returns("access-token");
        _tokenServiceMock
            .Setup(t => t.GenerateRefreshToken())
            .Returns(("raw-refresh-token", "hashed-refresh-token"));

        var request = new RegisterRequestDto
        {
            Username = "testuser",
            Email = "test@example.com",
            Password = "ValidPass1!",
        };

        var result = await _sut.RegisterAsync(request);

        result.AccessToken.Should().Be("access-token");
        result.RefreshToken.Should().Be("raw-refresh-token");
        result.User.Username.Should().Be("testuser");
        result.User.Email.Should().Be("test@example.com");
        _passwordHasherMock.Verify(p => p.Hash("ValidPass1!"), Times.Once);
        _db.Users.Should().Contain(u => u.Username == "testuser");
    }

    [Fact]
    public async Task RegisterAsync_ShouldThrow_WhenUsernameAlreadyExists()
    {
        _db.Users.Add(
            new User
            {
                Id = Guid.NewGuid(),
                Username = "existing",
                Email = "other@example.com",
                PasswordHash = "hash",
            }
        );
        await _db.SaveChangesAsync();

        var request = new RegisterRequestDto
        {
            Username = "existing",
            Email = "test@example.com",
            Password = "ValidPass1!",
        };

        await FluentActions
            .Awaiting(() => _sut.RegisterAsync(request))
            .Should()
            .ThrowAsync<ConflictException>()
            .WithMessage("A user with this username or email already exists.");
    }

    [Fact]
    public async Task RegisterAsync_ShouldThrow_WhenEmailAlreadyExists()
    {
        _db.Users.Add(
            new User
            {
                Id = Guid.NewGuid(),
                Username = "other",
                Email = "test@example.com",
                PasswordHash = "hash",
            }
        );
        await _db.SaveChangesAsync();

        var request = new RegisterRequestDto
        {
            Username = "newuser",
            Email = "test@example.com",
            Password = "ValidPass1!",
        };

        await FluentActions
            .Awaiting(() => _sut.RegisterAsync(request))
            .Should()
            .ThrowAsync<ConflictException>()
            .WithMessage("A user with this username or email already exists.");
    }

    [Fact]
    public async Task LoginAsync_ShouldReturnTokens_WhenCredentialsAreValid()
    {
        var userId = Guid.NewGuid();
        _db.Users.Add(
            new User
            {
                Id = userId,
                Username = "testuser",
                Email = "test@example.com",
                PasswordHash = "hashed-password",
            }
        );
        await _db.SaveChangesAsync();

        _passwordHasherMock.Setup(p => p.Verify("ValidPass1!", "hashed-password")).Returns(true);
        _tokenServiceMock
            .Setup(t => t.GenerateAccessToken(It.IsAny<User>()))
            .Returns("access-token");
        _tokenServiceMock
            .Setup(t => t.GenerateRefreshToken())
            .Returns(("raw-refresh", "hashed-refresh"));

        var request = new LoginRequestDto { Email = "test@example.com", Password = "ValidPass1!" };

        var result = await _sut.LoginAsync(request);

        result.AccessToken.Should().Be("access-token");
        result.RefreshToken.Should().Be("raw-refresh");
        result.User.Username.Should().Be("testuser");
    }

    [Fact]
    public async Task LoginAsync_ShouldThrow_WhenEmailNotFound()
    {
        var request = new LoginRequestDto { Email = "unknown@example.com", Password = "password" };

        await FluentActions
            .Awaiting(() => _sut.LoginAsync(request))
            .Should()
            .ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("Invalid credentials.");
    }

    [Fact]
    public async Task LoginAsync_ShouldThrow_WhenPasswordIsWrong()
    {
        _db.Users.Add(
            new User
            {
                Id = Guid.NewGuid(),
                Username = "testuser",
                Email = "test@example.com",
                PasswordHash = "hashed-password",
            }
        );
        await _db.SaveChangesAsync();

        _passwordHasherMock
            .Setup(p => p.Verify("wrong-password", "hashed-password"))
            .Returns(false);

        var request = new LoginRequestDto
        {
            Email = "test@example.com",
            Password = "wrong-password",
        };

        await FluentActions
            .Awaiting(() => _sut.LoginAsync(request))
            .Should()
            .ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("Invalid credentials.");
    }

    [Fact]
    public async Task RefreshAsync_ShouldReturnNewTokens()
    {
        var userId = Guid.NewGuid();
        _db.Users.Add(
            new User
            {
                Id = userId,
                Username = "testuser",
                Email = "test@example.com",
                PasswordHash = "hash",
            }
        );
        var storedToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TokenHash = "hashed-old-token",
            ExpiresAt = DateTime.UtcNow.AddDays(1),
            RevokedAt = null,
        };
        _db.Set<RefreshToken>().Add(storedToken);
        await _db.SaveChangesAsync();

        _tokenServiceMock.Setup(t => t.HashToken("raw-refresh-cookie")).Returns("hashed-old-token");
        _tokenServiceMock
            .Setup(t => t.GenerateAccessToken(It.IsAny<User>()))
            .Returns("new-access-token");
        _tokenServiceMock
            .Setup(t => t.GenerateRefreshToken())
            .Returns(("new-raw-refresh", "new-hashed-refresh"));

        var result = await _sut.RefreshAsync("raw-refresh-cookie");

        result.AccessToken.Should().Be("new-access-token");
        result.RefreshToken.Should().Be("new-raw-refresh");
        storedToken.RevokedAt.Should().NotBeNull();
        storedToken.ReplacedByTokenHash.Should().Be("new-hashed-refresh");
    }

    [Fact]
    public async Task RefreshAsync_ShouldThrow_WhenCookieIsNull()
    {
        await FluentActions
            .Awaiting(() => _sut.RefreshAsync(null))
            .Should()
            .ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("Refresh token is required.");
    }

    [Fact]
    public async Task RefreshAsync_ShouldThrow_WhenCookieIsEmpty()
    {
        await FluentActions
            .Awaiting(() => _sut.RefreshAsync(""))
            .Should()
            .ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("Refresh token is required.");
    }

    [Fact]
    public async Task RefreshAsync_ShouldThrow_WhenTokenNotFound()
    {
        _tokenServiceMock.Setup(t => t.HashToken("unknown-token")).Returns("unknown-hash");

        await FluentActions
            .Awaiting(() => _sut.RefreshAsync("unknown-token"))
            .Should()
            .ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("Invalid refresh token.");
    }

    [Fact]
    public async Task RefreshAsync_ShouldThrowAndRevokeFamily_WhenTokenIsRevoked()
    {
        var userId = Guid.NewGuid();
        _db.Users.Add(
            new User
            {
                Id = userId,
                Username = "u",
                Email = "u@example.com",
            }
        );
        var storedToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TokenHash = "hashed-revoked-token",
            ExpiresAt = DateTime.UtcNow.AddDays(1),
            RevokedAt = DateTime.UtcNow.AddDays(-1),
        };
        _db.Set<RefreshToken>().Add(storedToken);
        await _db.SaveChangesAsync();

        _tokenServiceMock.Setup(t => t.HashToken("stolen-token")).Returns("hashed-revoked-token");

        await FluentActions
            .Awaiting(() => _sut.RefreshAsync("stolen-token"))
            .Should()
            .ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("Refresh token has been revoked.");

        _tokenServiceMock.Verify(t => t.RevokeTokenFamilyAsync("hashed-revoked-token"), Times.Once);
    }

    [Fact]
    public async Task RefreshAsync_ShouldThrow_WhenTokenIsExpired()
    {
        var userId = Guid.NewGuid();
        _db.Users.Add(
            new User
            {
                Id = userId,
                Username = "u",
                Email = "u@example.com",
            }
        );
        var storedToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TokenHash = "hashed-expired-token",
            ExpiresAt = DateTime.UtcNow.AddDays(-1),
            RevokedAt = null,
        };
        _db.Set<RefreshToken>().Add(storedToken);
        await _db.SaveChangesAsync();

        _tokenServiceMock.Setup(t => t.HashToken("expired-token")).Returns("hashed-expired-token");

        await FluentActions
            .Awaiting(() => _sut.RefreshAsync("expired-token"))
            .Should()
            .ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("Refresh token has expired.");
    }

    [Fact]
    public async Task LogoutAsync_ShouldRevokeAllActiveTokens()
    {
        var userId = Guid.NewGuid();
        _db.Set<RefreshToken>()
            .AddRange(
                new RefreshToken
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    TokenHash = "hash1",
                    RevokedAt = null,
                    ExpiresAt = DateTime.UtcNow.AddDays(1),
                },
                new RefreshToken
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    TokenHash = "hash2",
                    RevokedAt = null,
                    ExpiresAt = DateTime.UtcNow.AddDays(1),
                }
            );
        await _db.SaveChangesAsync();

        await _sut.LogoutAsync(userId);

        _db.Set<RefreshToken>()
            .Where(rt => rt.UserId == userId)
            .All(rt => rt.RevokedAt != null)
            .Should()
            .BeTrue();
    }

    [Fact]
    public async Task GetCurrentUserAsync_ShouldReturnUserInfo()
    {
        var userId = Guid.NewGuid();
        _db.Users.Add(
            new User
            {
                Id = userId,
                Username = "testuser",
                Email = "test@example.com",
                ProfilePictureUrl = null,
            }
        );
        await _db.SaveChangesAsync();

        var result = await _sut.GetCurrentUserAsync(userId);

        result.Username.Should().Be("testuser");
        result.Email.Should().Be("test@example.com");
    }

    [Fact]
    public async Task GetCurrentUserAsync_ShouldThrow_WhenUserNotFound()
    {
        await FluentActions
            .Awaiting(() => _sut.GetCurrentUserAsync(Guid.NewGuid()))
            .Should()
            .ThrowAsync<NotFoundException>()
            .WithMessage("User not found.");
    }
}
