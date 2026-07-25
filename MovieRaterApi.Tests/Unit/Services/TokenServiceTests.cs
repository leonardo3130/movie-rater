using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using MovieRaterApi.Data;
using MovieRaterApi.Data.Entities;
using MovieRaterApi.Features.Authentication.Options;
using MovieRaterApi.Features.Authentication.Services;

namespace MovieRaterApi.Tests.Unit.Services;

public class TokenServiceTests
{
    private const string TestSigningKey =
        "this-is-a-test-key-that-is-at-least-32-characters-long-for-testing";
    private readonly JwtOptions _jwtOptions;
    private readonly Mock<ILogger<TokenService>> _loggerMock;
    private readonly TokenService _sut;
    private readonly ApplicationDbContext _db;

    public TokenServiceTests()
    {
        _jwtOptions = new JwtOptions
        {
            Issuer = "MovieRaterApi",
            Audience = "MovieRaterWeb",
            AccessTokenMinutes = 15,
            RefreshTokenDays = 30,
            SigningKey = TestSigningKey,
        };

        _db = TestHelpers.CreateInMemoryDbContext();
        _loggerMock = new Mock<ILogger<TokenService>>();

        _sut = new TokenService(_db, Options.Create(_jwtOptions), _loggerMock.Object);
    }

    [Fact]
    public void GenerateAccessToken_ShouldIncludeUserIdClaim()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = "testuser",
            Email = "test@example.com",
        };

        var token = _sut.GenerateAccessToken(user, null);
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        jwtToken
            .Claims.First(c => c.Type == ClaimTypes.NameIdentifier)
            .Value.Should()
            .Be(user.Id.ToString());
    }

    [Fact]
    public void GenerateAccessToken_ShouldIncludeUsernameClaim()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = "testuser",
            Email = "test@example.com",
        };

        var token = _sut.GenerateAccessToken(user, null);
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        jwtToken.Claims.First(c => c.Type == ClaimTypes.Name).Value.Should().Be("testuser");
    }

    [Fact]
    public void GenerateAccessToken_ShouldIncludeEmailClaim()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = "testuser",
            Email = "test@example.com",
        };

        var token = _sut.GenerateAccessToken(user, null);
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        jwtToken
            .Claims.First(c => c.Type == ClaimTypes.Email)
            .Value.Should()
            .Be("test@example.com");
    }

    [Fact]
    public void GenerateAccessToken_ShouldIncludeCoupleIdClaim_WhenProvided()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = "testuser",
            Email = "test@example.com",
        };
        var coupleId = Guid.NewGuid();

        var token = _sut.GenerateAccessToken(user, coupleId);
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        jwtToken.Claims.First(c => c.Type == "coupleId").Value.Should().Be(coupleId.ToString());
    }

    [Fact]
    public void GenerateAccessToken_ShouldNotIncludeCoupleIdClaim_WhenNull()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = "testuser",
            Email = "test@example.com",
        };

        var token = _sut.GenerateAccessToken(user, null);
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        jwtToken.Claims.Any(c => c.Type == "coupleId").Should().BeFalse();
    }

    [Fact]
    public void GenerateAccessToken_ShouldHaveExpirationInTheFuture()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = "testuser",
            Email = "test@example.com",
        };

        var token = _sut.GenerateAccessToken(user, null);
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        jwtToken
            .ValidTo.Should()
            .BeCloseTo(DateTime.UtcNow.AddMinutes(15), TimeSpan.FromSeconds(10));
    }

    [Fact]
    public void GenerateAccessToken_ShouldHaveCorrectIssuerAndAudience()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = "testuser",
            Email = "test@example.com",
        };

        var token = _sut.GenerateAccessToken(user, null);
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        jwtToken.Issuer.Should().Be("MovieRaterApi");
        jwtToken.Audiences.Should().Contain("MovieRaterWeb");
    }

    [Fact]
    public void GenerateRefreshToken_ShouldReturnRawTokenAndHash()
    {
        var (rawToken, tokenHash) = _sut.GenerateRefreshToken();

        rawToken.Should().NotBeNullOrWhiteSpace();
        tokenHash.Should().NotBeNullOrWhiteSpace();
        rawToken.Should().NotBe(tokenHash);
    }

    [Fact]
    public void HashToken_ShouldProduceConsistentHash()
    {
        var rawToken = "some-raw-token-value";

        var hash1 = _sut.HashToken(rawToken);
        var hash2 = _sut.HashToken(rawToken);

        hash1.Should().Be(hash2);
    }

    [Fact]
    public void HashToken_ShouldProduceDifferentHashForDifferentInput()
    {
        var hash1 = _sut.HashToken("token-1");
        var hash2 = _sut.HashToken("token-2");

        hash1.Should().NotBe(hash2);
    }

    [Fact]
    public async Task IsRefreshTokenValidAsync_ShouldReturnTrue_WhenTokenExistsAndNotRevokedAndNotExpired()
    {
        var tokenHash = "valid-hash";
        _db.Set<RefreshToken>()
            .Add(
                new RefreshToken
                {
                    TokenHash = tokenHash,
                    ExpiresAt = DateTime.UtcNow.AddDays(1),
                    RevokedAt = null,
                }
            );
        await _db.SaveChangesAsync();

        var result = await _sut.IsRefreshTokenValidAsync(tokenHash);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsRefreshTokenValidAsync_ShouldReturnFalse_WhenTokenIsRevoked()
    {
        var tokenHash = "revoked-hash";
        _db.Set<RefreshToken>()
            .Add(
                new RefreshToken
                {
                    TokenHash = tokenHash,
                    ExpiresAt = DateTime.UtcNow.AddDays(1),
                    RevokedAt = DateTime.UtcNow,
                }
            );
        await _db.SaveChangesAsync();

        var result = await _sut.IsRefreshTokenValidAsync(tokenHash);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsRefreshTokenValidAsync_ShouldReturnFalse_WhenTokenIsExpired()
    {
        var tokenHash = "expired-hash";
        _db.Set<RefreshToken>()
            .Add(
                new RefreshToken
                {
                    TokenHash = tokenHash,
                    ExpiresAt = DateTime.UtcNow.AddDays(-1),
                    RevokedAt = null,
                }
            );
        await _db.SaveChangesAsync();

        var result = await _sut.IsRefreshTokenValidAsync(tokenHash);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsRefreshTokenValidAsync_ShouldReturnFalse_WhenTokenDoesNotExist()
    {
        var result = await _sut.IsRefreshTokenValidAsync("non-existent-hash");

        result.Should().BeFalse();
    }

    [Fact]
    public async Task RevokeRefreshTokenAsync_ShouldSetRevokedAt()
    {
        var tokenHash = "to-revoke";
        var token = new RefreshToken
        {
            TokenHash = tokenHash,
            ExpiresAt = DateTime.UtcNow.AddDays(1),
            RevokedAt = null,
        };
        _db.Set<RefreshToken>().Add(token);
        await _db.SaveChangesAsync();

        await _sut.RevokeRefreshTokenAsync(tokenHash);

        token.RevokedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task RevokeRefreshTokenAsync_ShouldSetReplacedByTokenHash()
    {
        var tokenHash = "to-revoke";
        var replacedByHash = "new-hash";
        var token = new RefreshToken
        {
            TokenHash = tokenHash,
            ExpiresAt = DateTime.UtcNow.AddDays(1),
            RevokedAt = null,
        };
        _db.Set<RefreshToken>().Add(token);
        await _db.SaveChangesAsync();

        await _sut.RevokeRefreshTokenAsync(tokenHash, replacedByHash);

        token.ReplacedByTokenHash.Should().Be(replacedByHash);
    }
}
