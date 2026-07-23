using System.Security.Cryptography;
using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using MovieRaterApi.Data;
using MovieRaterApi.Data.Entities;
using MovieRaterApi.Features.Authentication.DTOs;
using MovieRaterApi.Features.Authentication.Services;

namespace MovieRaterApi.Tests.Unit.Services;

public class CoupleInvitationServiceTests
{
    private readonly ApplicationDbContext _db;
    private readonly Mock<ILogger<CoupleInvitationService>> _loggerMock;
    private readonly CoupleInvitationService _sut;

    public CoupleInvitationServiceTests()
    {
        _db = TestHelpers.CreateInMemoryDbContext();
        _loggerMock = new Mock<ILogger<CoupleInvitationService>>();
        _sut = new CoupleInvitationService(_db, _loggerMock.Object);
    }

    private static string HashToken(string rawToken)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawToken));
        return Convert.ToBase64String(bytes);
    }

    [Fact]
    public async Task InviteAsync_ShouldCreateInvitation_WhenValid()
    {
        var inviterId = Guid.NewGuid();
        var inviteeId = Guid.NewGuid();
        _db.Users.AddRange(
            new User { Id = inviterId, Username = "user1", Email = "user1@example.com" },
            new User { Id = inviteeId, Username = "user2", Email = "user2@example.com" }
        );
        await _db.SaveChangesAsync();

        var request = new InvitePartnerRequestDto { InviteeEmail = "user2@example.com" };

        var result = await _sut.InviteAsync(inviterId, request);

        result.InvitationId.Should().NotBeEmpty();
        result.InviteToken.Should().NotBeNullOrWhiteSpace();
        result.ExpiresAt.Should().BeCloseTo(DateTime.UtcNow.AddDays(7), TimeSpan.FromSeconds(10));
    }

    [Fact]
    public async Task InviteAsync_ShouldThrow_WhenInviterNotFound()
    {
        var request = new InvitePartnerRequestDto { InviteeEmail = "partner@example.com" };

        await FluentActions
            .Awaiting(() => _sut.InviteAsync(Guid.NewGuid(), request))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Inviter user not found.");
    }

    [Fact]
    public async Task InviteAsync_ShouldThrow_WhenInviteeNotFound()
    {
        var inviter = new User { Id = Guid.NewGuid(), Username = "user1", Email = "user1@example.com" };
        _db.Users.Add(inviter);
        await _db.SaveChangesAsync();

        var request = new InvitePartnerRequestDto { InviteeEmail = "nonexistent@example.com" };

        await FluentActions
            .Awaiting(() => _sut.InviteAsync(inviter.Id, request))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("No user found with this email address.");
    }

    [Fact]
    public async Task InviteAsync_ShouldThrow_WhenInvitingYourself()
    {
        var userId = Guid.NewGuid();
        _db.Users.Add(new User { Id = userId, Username = "self", Email = "self@example.com" });
        await _db.SaveChangesAsync();

        var request = new InvitePartnerRequestDto { InviteeEmail = "self@example.com" };

        await FluentActions
            .Awaiting(() => _sut.InviteAsync(userId, request))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("You cannot invite yourself.");
    }

    [Fact]
    public async Task InviteAsync_ShouldThrow_WhenAlreadyCoupled()
    {
        var inviterId = Guid.NewGuid();
        var inviteeId = Guid.NewGuid();
        _db.Users.AddRange(
            new User { Id = inviterId, Username = "user1", Email = "user1@example.com" },
            new User { Id = inviteeId, Username = "user2", Email = "user2@example.com" }
        );
        _db.Couples.Add(new Couple { Id = Guid.NewGuid(), User1Id = inviterId, User2Id = inviteeId });
        await _db.SaveChangesAsync();

        var request = new InvitePartnerRequestDto { InviteeEmail = "user2@example.com" };

        await FluentActions
            .Awaiting(() => _sut.InviteAsync(inviterId, request))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("You are already connected with this user.");
    }

    [Fact]
    public async Task InviteAsync_ShouldThrow_WhenPendingInvitationExists()
    {
        var inviterId = Guid.NewGuid();
        var inviteeId = Guid.NewGuid();
        _db.Users.AddRange(
            new User { Id = inviterId, Username = "user1", Email = "user1@example.com" },
            new User { Id = inviteeId, Username = "user2", Email = "user2@example.com" }
        );
        _db.Set<CoupleInvitation>().Add(new CoupleInvitation
        {
            InviterUserId = inviterId,
            InviteeEmail = "user2@example.com",
            Status = InvitationStatus.Pending
        });
        await _db.SaveChangesAsync();

        var request = new InvitePartnerRequestDto { InviteeEmail = "user2@example.com" };

        await FluentActions
            .Awaiting(() => _sut.InviteAsync(inviterId, request))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("A pending invitation already exists for this user.");
    }

    [Fact]
    public async Task AcceptInvitationAsync_ShouldCreateCouple_WhenValid()
    {
        var inviterId = Guid.NewGuid();
        var acceptorId = Guid.NewGuid();
        _db.Users.AddRange(
            new User { Id = inviterId, Username = "user1", Email = "user1@example.com" },
            new User { Id = acceptorId, Username = "user2", Email = "user2@example.com" }
        );
        var rawToken = "valid-raw-token";
        var invitation = new CoupleInvitation
        {
            Id = Guid.NewGuid(),
            InviterUserId = inviterId,
            InviteeEmail = "user2@example.com",
            InviteTokenHash = HashToken(rawToken),
            Status = InvitationStatus.Pending,
            ExpiresAt = DateTime.UtcNow.AddDays(1)
        };
        _db.Set<CoupleInvitation>().Add(invitation);
        await _db.SaveChangesAsync();

        var request = new AcceptInvitationRequestDto { InviteToken = rawToken };

        await _sut.AcceptInvitationAsync(acceptorId, request);

        invitation.Status.Should().Be(InvitationStatus.Accepted);
        invitation.AcceptedByUserId.Should().Be(acceptorId);
        _db.Couples.Should().Contain(c => c.User1Id == inviterId && c.User2Id == acceptorId);
    }

    [Fact]
    public async Task AcceptInvitationAsync_ShouldThrow_WhenUserNotFound()
    {
        var request = new AcceptInvitationRequestDto { InviteToken = "token" };

        await FluentActions
            .Awaiting(() => _sut.AcceptInvitationAsync(Guid.NewGuid(), request))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("User not found.");
    }

    [Fact]
    public async Task AcceptInvitationAsync_ShouldThrow_WhenTokenInvalid()
    {
        var userId = Guid.NewGuid();
        _db.Users.Add(new User { Id = userId, Username = "user", Email = "user@example.com" });
        await _db.SaveChangesAsync();

        var request = new AcceptInvitationRequestDto { InviteToken = "invalid-token" };

        await FluentActions
            .Awaiting(() => _sut.AcceptInvitationAsync(userId, request))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Invalid invitation token.");
    }

    [Fact]
    public async Task AcceptInvitationAsync_ShouldThrow_WhenNotPending()
    {
        var acceptor = new User { Id = Guid.NewGuid(), Username = "user", Email = "user@example.com" };
        _db.Users.Add(acceptor);
        var rawToken = "raw-token";
        var invitation = new CoupleInvitation
        {
            InviteTokenHash = HashToken(rawToken),
            Status = InvitationStatus.Accepted,
            ExpiresAt = DateTime.UtcNow.AddDays(1)
        };
        _db.Set<CoupleInvitation>().Add(invitation);
        await _db.SaveChangesAsync();

        var request = new AcceptInvitationRequestDto { InviteToken = rawToken };

        await FluentActions
            .Awaiting(() => _sut.AcceptInvitationAsync(acceptor.Id, request))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Invitation is accepted and cannot be accepted.");
    }

    [Fact]
    public async Task AcceptInvitationAsync_ShouldMarkExpired_WhenExpired()
    {
        var acceptor = new User { Id = Guid.NewGuid(), Username = "user", Email = "user@example.com" };
        _db.Users.Add(acceptor);
        var rawToken = "raw-token";
        var invitation = new CoupleInvitation
        {
            InviteTokenHash = HashToken(rawToken),
            Status = InvitationStatus.Pending,
            ExpiresAt = DateTime.UtcNow.AddDays(-1)
        };
        _db.Set<CoupleInvitation>().Add(invitation);
        await _db.SaveChangesAsync();

        var request = new AcceptInvitationRequestDto { InviteToken = rawToken };

        await FluentActions
            .Awaiting(() => _sut.AcceptInvitationAsync(acceptor.Id, request))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Invitation has expired.");

        invitation.Status.Should().Be(InvitationStatus.Expired);
    }

    [Fact]
    public async Task AcceptInvitationAsync_ShouldThrow_WhenEmailDoesNotMatch()
    {
        var acceptorId = Guid.NewGuid();
        _db.Users.Add(new User { Id = acceptorId, Username = "user", Email = "other@example.com" });
        var rawToken = "raw-token";
        var invitation = new CoupleInvitation
        {
            InviteTokenHash = HashToken(rawToken),
            InviteeEmail = "invited@example.com",
            Status = InvitationStatus.Pending,
            ExpiresAt = DateTime.UtcNow.AddDays(1)
        };
        _db.Set<CoupleInvitation>().Add(invitation);
        await _db.SaveChangesAsync();

        var request = new AcceptInvitationRequestDto { InviteToken = rawToken };

        await FluentActions
            .Awaiting(() => _sut.AcceptInvitationAsync(acceptorId, request))
            .Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("This invitation was not sent to you.");
    }
}