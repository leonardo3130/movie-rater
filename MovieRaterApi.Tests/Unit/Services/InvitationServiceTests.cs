using System.Security.Cryptography;
using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using MovieRaterApi.Data;
using MovieRaterApi.Data.Entities;
using MovieRaterApi.Features.Groups.DTOs;
using MovieRaterApi.Features.Groups.Services;
using MovieRaterApi.Infrastructure.Exceptions;

namespace MovieRaterApi.Tests.Unit.Services;

public class InvitationServiceTests
{
    private readonly ApplicationDbContext _db;
    private readonly Mock<ILogger<InvitationService>> _loggerMock;
    private readonly InvitationService _sut;

    public InvitationServiceTests()
    {
        _db = TestHelpers.CreateInMemoryDbContext();
        _loggerMock = new Mock<ILogger<InvitationService>>();
        _sut = new InvitationService(_db, _loggerMock.Object);
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
        var groupId = Guid.NewGuid();

        _db.Users.AddRange(
            new User
            {
                Id = inviterId,
                Username = "user1",
                Email = "user1@example.com",
            },
            new User
            {
                Id = inviteeId,
                Username = "user2",
                Email = "user2@example.com",
            }
        );
        await _db.SaveChangesAsync();

        var request = new InvitationRequestDto
        {
            GroupId = groupId,
            InviteeEmail = "user2@example.com",
        };

        var result = await _sut.InviteAsync(inviterId, request);

        result.InvitationId.Should().NotBeEmpty();
        result.InviteToken.Should().NotBeNullOrWhiteSpace();
        result.ExpiresAt.Should().BeCloseTo(DateTime.UtcNow.AddDays(7), TimeSpan.FromSeconds(10));
    }

    [Fact]
    public async Task InviteAsync_ShouldThrow_WhenInviterNotFound()
    {
        var request = new InvitationRequestDto { InviteeEmail = "partner@example.com" };

        await FluentActions
            .Awaiting(() => _sut.InviteAsync(Guid.NewGuid(), request))
            .Should()
            .ThrowAsync<NotFoundException>()
            .WithMessage("Inviter user not found.");
    }

    [Fact]
    public async Task InviteAsync_ShouldThrow_WhenInviteeNotFound()
    {
        var inviter = new User
        {
            Id = Guid.NewGuid(),
            Username = "user1",
            Email = "user1@example.com",
        };
        _db.Users.Add(inviter);
        await _db.SaveChangesAsync();

        var request = new InvitationRequestDto { InviteeEmail = "nonexistent@example.com" };

        await FluentActions
            .Awaiting(() => _sut.InviteAsync(inviter.Id, request))
            .Should()
            .ThrowAsync<NotFoundException>()
            .WithMessage("No user found with this email address.");
    }

    [Fact]
    public async Task InviteAsync_ShouldThrow_WhenInvitingYourself()
    {
        var userId = Guid.NewGuid();
        _db.Users.Add(
            new User
            {
                Id = userId,
                Username = "self",
                Email = "self@example.com",
            }
        );
        await _db.SaveChangesAsync();

        var request = new InvitationRequestDto { InviteeEmail = "self@example.com" };

        await FluentActions
            .Awaiting(() => _sut.InviteAsync(userId, request))
            .Should()
            .ThrowAsync<BadRequestException>()
            .WithMessage("You cannot invite yourself.");
    }

    [Fact]
    public async Task InviteAsync_ShouldThrow_WhenAlreadyInTheGroup()
    {
        var inviterId = Guid.NewGuid();
        var inviteeId = Guid.NewGuid();
        var groupId = Guid.NewGuid();
        _db.Users.AddRange(
            new User
            {
                Id = inviterId,
                Username = "user1",
                Email = "user1@example.com",
            },
            new User
            {
                Id = inviteeId,
                Username = "user2",
                Email = "user2@example.com",
            }
        );
        _db.Groups.Add(
            new Group
            {
                Id = groupId,
                Name = "g",
                CreatedAt = DateTime.UtcNow,
            }
        );
        _db.UserGroups.AddRange(
            new UserGroup
            {
                Id = Guid.NewGuid(),
                UserId = inviterId,
                GroupId = groupId,
            },
            new UserGroup
            {
                Id = Guid.NewGuid(),
                UserId = inviteeId,
                GroupId = groupId,
            }
        );

        await _db.SaveChangesAsync();

        var request = new InvitationRequestDto
        {
            InviteeEmail = "user2@example.com",
            GroupId = groupId,
        };

        await FluentActions
            .Awaiting(() => _sut.InviteAsync(inviterId, request))
            .Should()
            .ThrowAsync<ConflictException>()
            .WithMessage("Invited user is already in the group");
    }

    [Fact]
    public async Task InviteAsync_ShouldThrow_WhenPendingInvitationExists()
    {
        var inviterId = Guid.NewGuid();
        var inviteeId = Guid.NewGuid();
        _db.Users.AddRange(
            new User
            {
                Id = inviterId,
                Username = "user1",
                Email = "user1@example.com",
            },
            new User
            {
                Id = inviteeId,
                Username = "user2",
                Email = "user2@example.com",
            }
        );
        _db.Set<Invitation>()
            .Add(
                new Invitation
                {
                    InviterUserId = inviterId,
                    InviteeEmail = "user2@example.com",
                    Status = InvitationStatus.Pending,
                }
            );
        await _db.SaveChangesAsync();

        var request = new InvitationRequestDto { InviteeEmail = "user2@example.com" };

        await FluentActions
            .Awaiting(() => _sut.InviteAsync(inviterId, request))
            .Should()
            .ThrowAsync<ConflictException>()
            .WithMessage("A pending invitation already exists for this user.");
    }

    [Fact]
    public async Task AcceptInvitationAsync_ShouldJoinGroup_WhenValid()
    {
        var inviterId = Guid.NewGuid();
        var acceptorId = Guid.NewGuid();
        var groupId = Guid.NewGuid();
        _db.Users.AddRange(
            new User
            {
                Id = inviterId,
                Username = "user1",
                Email = "user1@example.com",
            },
            new User
            {
                Id = acceptorId,
                Username = "user2",
                Email = "user2@example.com",
            }
        );
        _db.Groups.Add(
            new Group
            {
                Id = groupId,
                Name = "G",
                CreatedAt = DateTime.UtcNow,
            }
        );
        _db.UserGroups.Add(
            new UserGroup
            {
                Id = Guid.NewGuid(),
                GroupId = groupId,
                UserId = inviterId,
            }
        );
        var rawToken = "valid-raw-token";
        var invitation = new Invitation
        {
            Id = Guid.NewGuid(),
            InviterUserId = inviterId,
            InviteeEmail = "user2@example.com",
            InviteTokenHash = HashToken(rawToken),
            Status = InvitationStatus.Pending,
            ExpiresAt = DateTime.UtcNow.AddDays(1),
            GroupId = groupId,
        };
        _db.Set<Invitation>().Add(invitation);
        await _db.SaveChangesAsync();

        var request = new AcceptInvitationRequestDto { InviteToken = rawToken };

        await _sut.AcceptInvitationAsync(acceptorId, request);

        invitation.Status.Should().Be(InvitationStatus.Accepted);
        invitation.AcceptedByUserId.Should().Be(acceptorId);
        _db.UserGroups.Should().Contain(ug => ug.UserId == acceptorId);
        _db.UserGroups.Should().Contain(ug => ug.UserId == inviterId);
    }

    [Fact]
    public async Task AcceptInvitationAsync_ShouldThrow_WhenUserNotFound()
    {
        var request = new AcceptInvitationRequestDto { InviteToken = "token" };

        await FluentActions
            .Awaiting(() => _sut.AcceptInvitationAsync(Guid.NewGuid(), request))
            .Should()
            .ThrowAsync<NotFoundException>()
            .WithMessage("User not found.");
    }

    [Fact]
    public async Task AcceptInvitationAsync_ShouldThrow_WhenTokenInvalid()
    {
        var userId = Guid.NewGuid();
        _db.Users.Add(
            new User
            {
                Id = userId,
                Username = "user",
                Email = "user@example.com",
            }
        );
        await _db.SaveChangesAsync();

        var request = new AcceptInvitationRequestDto { InviteToken = "invalid-token" };

        await FluentActions
            .Awaiting(() => _sut.AcceptInvitationAsync(userId, request))
            .Should()
            .ThrowAsync<NotFoundException>()
            .WithMessage("Invalid invitation token.");
    }

    [Fact]
    public async Task AcceptInvitationAsync_ShouldThrow_WhenNotPending()
    {
        var acceptor = new User
        {
            Id = Guid.NewGuid(),
            Username = "user",
            Email = "user@example.com",
        };
        _db.Users.Add(acceptor);
        var rawToken = "raw-token";
        var invitation = new Invitation
        {
            InviteTokenHash = HashToken(rawToken),
            Status = InvitationStatus.Accepted,
            ExpiresAt = DateTime.UtcNow.AddDays(1),
        };
        _db.Set<Invitation>().Add(invitation);
        await _db.SaveChangesAsync();

        var request = new AcceptInvitationRequestDto { InviteToken = rawToken };

        await FluentActions
            .Awaiting(() => _sut.AcceptInvitationAsync(acceptor.Id, request))
            .Should()
            .ThrowAsync<BadRequestException>()
            .WithMessage("Invitation is accepted and cannot be accepted.");
    }

    [Fact]
    public async Task AcceptInvitationAsync_ShouldMarkExpired_WhenExpired()
    {
        var acceptor = new User
        {
            Id = Guid.NewGuid(),
            Username = "user",
            Email = "user@example.com",
        };
        _db.Users.Add(acceptor);
        var rawToken = "raw-token";
        var invitation = new Invitation
        {
            InviteTokenHash = HashToken(rawToken),
            Status = InvitationStatus.Pending,
            ExpiresAt = DateTime.UtcNow.AddDays(-1),
        };
        _db.Set<Invitation>().Add(invitation);
        await _db.SaveChangesAsync();

        var request = new AcceptInvitationRequestDto { InviteToken = rawToken };

        await FluentActions
            .Awaiting(() => _sut.AcceptInvitationAsync(acceptor.Id, request))
            .Should()
            .ThrowAsync<BadRequestException>()
            .WithMessage("Invitation has expired.");

        invitation.Status.Should().Be(InvitationStatus.Expired);
    }

    [Fact]
    public async Task AcceptInvitationAsync_ShouldThrow_WhenEmailDoesNotMatch()
    {
        var acceptorId = Guid.NewGuid();
        _db.Users.Add(
            new User
            {
                Id = acceptorId,
                Username = "user",
                Email = "other@example.com",
            }
        );
        var rawToken = "raw-token";
        var invitation = new Invitation
        {
            InviteTokenHash = HashToken(rawToken),
            InviteeEmail = "invited@example.com",
            Status = InvitationStatus.Pending,
            ExpiresAt = DateTime.UtcNow.AddDays(1),
        };
        _db.Set<Invitation>().Add(invitation);
        await _db.SaveChangesAsync();

        var request = new AcceptInvitationRequestDto { InviteToken = rawToken };

        await FluentActions
            .Awaiting(() => _sut.AcceptInvitationAsync(acceptorId, request))
            .Should()
            .ThrowAsync<ForbiddenException>()
            .WithMessage("This invitation was not sent to you.");
    }
}
