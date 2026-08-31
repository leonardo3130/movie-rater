using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using MovieRaterApi.Data;
using MovieRaterApi.Data.Entities;
using MovieRaterApi.Features.Authentication.DTOs;
using MovieRaterApi.Features.Authentication.Interfaces;
using MovieRaterApi.Features.Authentication.Services;
using MovieRaterApi.Infrastructure.Email;
using MovieRaterApi.Infrastructure.Email.Options;
using MovieRaterApi.Infrastructure.Exceptions;

namespace MovieRaterApi.Tests.Unit.Services;

public class PasswordResetServiceTests : IDisposable
{
    private readonly ApplicationDbContext _db;
    private readonly Mock<ITokenService> _tokenServiceMock;
    private readonly Mock<IPasswordHasher> _passwordHasherMock;
    private readonly Mock<IEmailSender> _emailSenderMock;
    private readonly EmailConfiguration _emailOptions;
    private readonly Mock<ILogger<AuthService>> _loggerMock;
    private readonly PasswordResetService _sut;

    public PasswordResetServiceTests()
    {
        _db = TestHelpers.CreateInMemoryDbContext();

        _tokenServiceMock = new Mock<ITokenService>();
        _passwordHasherMock = new Mock<IPasswordHasher>();
        _emailSenderMock = new Mock<IEmailSender>();

        _emailOptions = new EmailConfiguration
        {
            FromAddress = "from@example.com",
            FrontendBaseUrl = "https://movie-rater.leopo.dev",
            PasswordResetPath = "reset-password",
            SmtpServer = "smtp.example.com",
        };

        _loggerMock = new Mock<ILogger<AuthService>>();

        _sut = new PasswordResetService(
            _db,
            _tokenServiceMock.Object,
            _passwordHasherMock.Object,
            _emailSenderMock.Object,
            Options.Create(_emailOptions),
            _loggerMock.Object
        );
    }

    public void Dispose()
    {
        _db.Dispose();
    }

    private User SeedUser(string email = "user@example.com", string username = "testuser")
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = username,
            Email = email,
            PasswordHash = "old-hash",
        };
        _db.Users.Add(user);
        _db.SaveChanges();
        return user;
    }

    private PasswordResetToken SeedToken(Guid userId, string tokenHash, DateTime? expiresAt = null)
    {
        var token = new PasswordResetToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TokenHash = tokenHash,
            ExpiresAt = expiresAt ?? DateTime.UtcNow.AddHours(1),
        };
        _db.PasswordResetTokens.Add(token);
        _db.SaveChanges();
        return token;
    }

    [Fact]
    public async Task SendResetPasswordEmail_ShouldPersistTokenAndSendEmail()
    {
        var user = SeedUser();
        _tokenServiceMock
            .Setup(t => t.GeneratePasswordResetToken())
            .Returns(("raw-token", "hashed-token"));

        await _sut.SendResetPasswordEmail(user.Email);

        var stored = _db.PasswordResetTokens.Single();
        stored.UserId.Should().Be(user.Id);
        stored.TokenHash.Should().Be("hashed-token");
        stored.ExpiresAt.Should().BeAfter(DateTime.UtcNow.AddMinutes(59));

        _emailSenderMock.Verify(
            e => e.SendAsync(
                user.Email,
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()
            ),
            Times.Once
        );
    }

    [Fact]
    public async Task SendResetPasswordEmail_ShouldRenderBodyWithAllPlaceholders()
    {
        var user = SeedUser("user@example.com", "testuser");
        _tokenServiceMock
            .Setup(t => t.GeneratePasswordResetToken())
            .Returns(("raw/token+data", "hashed-token"));

        string? capturedBody = null;
        _emailSenderMock
            .Setup(e => e.SendAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()
            ))
            .Callback<string, string, string, CancellationToken>(
                (to, subject, body, ct) => capturedBody = body
            )
            .Returns(Task.CompletedTask);

        await _sut.SendResetPasswordEmail(user.Email);

        capturedBody.Should().NotBeNull();
        capturedBody!.Should().Contain("testuser");
        capturedBody.Should().Contain("user@example.com");
        capturedBody.Should().Contain("1 hour");
        capturedBody.Should().Contain("reset-password?token=");
        capturedBody.Should().NotContain("{{");
    }

    [Fact]
    public async Task SendResetPasswordEmail_ShouldDoNothing_WhenEmailUnknown()
    {
        await _sut.SendResetPasswordEmail("unknown@example.com");

        _db.PasswordResetTokens.Should().BeEmpty();
        _emailSenderMock.Verify(
            e => e.SendAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()
            ),
            Times.Never
        );
    }

    [Fact]
    public async Task ResetPassword_ShouldUpdatePasswordAndConsumeToken()
    {
        var user = SeedUser();
        SeedToken(user.Id, "hashed-token");

        _tokenServiceMock.Setup(t => t.HashToken("raw-token")).Returns("hashed-token");
        _passwordHasherMock.Setup(p => p.Hash("NewPass1!")).Returns("new-hash");

        var request = new ResetPasswordRequest { Token = "raw-token", Password = "NewPass1!" };

        await _sut.ResetPassword(request);

        _db.Users.Single(u => u.Id == user.Id).PasswordHash.Should().Be("new-hash");
        _db.PasswordResetTokens.Should().BeEmpty();
    }

    [Fact]
    public async Task ResetPassword_ShouldThrow_WhenTokenInvalid()
    {
        SeedUser();
        _tokenServiceMock.Setup(t => t.HashToken("raw-token")).Returns("unknown-hash");

        var request = new ResetPasswordRequest { Token = "raw-token", Password = "NewPass1!" };

        await FluentActions
            .Awaiting(() => _sut.ResetPassword(request))
            .Should()
            .ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("Password reset token is invalid");
    }

    [Fact]
    public async Task ResetPassword_ShouldThrow_WhenTokenExpired()
    {
        var user = SeedUser();
        SeedToken(user.Id, "hashed-token", DateTime.UtcNow.AddMinutes(-1));

        _tokenServiceMock.Setup(t => t.HashToken("raw-token")).Returns("hashed-token");

        var request = new ResetPasswordRequest { Token = "raw-token", Password = "NewPass1!" };

        await FluentActions
            .Awaiting(() => _sut.ResetPassword(request))
            .Should()
            .ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("Password reset token is invalid");
    }
}
