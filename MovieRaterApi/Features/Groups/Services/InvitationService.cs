using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using MovieRaterApi.Data;
using MovieRaterApi.Data.Entities;
using MovieRaterApi.Features.Groups.DTOs;
using MovieRaterApi.Features.Groups.Interfaces;
using MovieRaterApi.Infrastructure.Exceptions;

namespace MovieRaterApi.Features.Groups.Services;

public class InvitationService : IInvitationService
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<InvitationService> _logger;

    public InvitationService(ApplicationDbContext db, ILogger<InvitationService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<InvitationResponseDto> InviteAsync(
        Guid inviterUserId,
        InvitationRequestDto request
    )
    {
        var inviter = await _db.Users.FirstOrDefaultAsync(u => u.Id == inviterUserId);
        if (inviter is null)
        {
            throw new NotFoundException("Inviter user not found.");
        }

        var invitee = await _db.Users.FirstOrDefaultAsync(u => u.Email == request.InviteeEmail);
        if (invitee is null)
        {
            throw new NotFoundException("No user found with this email address.");
        }

        if (invitee.Id == inviterUserId)
        {
            throw new BadRequestException("You cannot invite yourself.");
        }

        var existingGroups = await _db.UserGroups.AnyAsync(ug =>
            ug.UserId == invitee.Id && ug.GroupId == request.GroupId
        );
        if (existingGroups)
        {
            throw new ConflictException("Invited user is already in the group");
        }

        var existingInvitation = await _db.Set<Invitation>()
            .FirstOrDefaultAsync(ci =>
                ci.InviterUserId == inviterUserId
                && ci.InviteeEmail == request.InviteeEmail
                && ci.GroupId == request.GroupId
                && ci.Status == InvitationStatus.Pending
            );
        if (existingInvitation is not null)
        {
            throw new ConflictException("A pending invitation already exists for this user.");
        }

        var rawToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        var tokenHash = HashToken(rawToken);

        var invitation = new Invitation
        {
            Id = Guid.NewGuid(),
            GroupId = request.GroupId,
            InviterUserId = inviterUserId,
            InviteeEmail = request.InviteeEmail,
            InviteTokenHash = tokenHash,
            Status = InvitationStatus.Pending,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow,
        };

        _db.Set<Invitation>().Add(invitation);
        await _db.SaveChangesAsync();

        _logger.LogInformation(
            "Invitation {InvitationId} created by {InviterUserId} for {InviteeEmail} to join {GroupId}",
            invitation.Id,
            inviterUserId,
            request.InviteeEmail,
            request.GroupId
        );

        var encoded = Uri.EscapeDataString(rawToken);

        return new InvitationResponseDto
        {
            InvitationId = invitation.Id,
            InviteToken = encoded,
            ExpiresAt = invitation.ExpiresAt,
        };
    }

    public async Task<AcceptInvitationResponseDto> AcceptInvitationAsync(
        Guid acceptedByUserId,
        AcceptInvitationRequestDto request
    )
    {
        var acceptedByUser = await _db.Users.FirstOrDefaultAsync(u => u.Id == acceptedByUserId);
        if (acceptedByUser is null)
        {
            throw new NotFoundException("User not found.");
        }

        var rawToken = Uri.UnescapeDataString(request.InviteToken);
        var tokenHash = HashToken(rawToken);

        var invitation = await _db.Set<Invitation>()
            .FirstOrDefaultAsync(ci => ci.InviteTokenHash == tokenHash);

        if (invitation is null)
        {
            throw new NotFoundException("Invalid invitation token.");
        }

        if (invitation.Status != InvitationStatus.Pending)
        {
            throw new BadRequestException(
                $"Invitation is {invitation.Status.ToString().ToLower()} and cannot be accepted."
            );
        }

        if (invitation.ExpiresAt <= DateTime.UtcNow)
        {
            invitation.Status = InvitationStatus.Expired;
            await _db.SaveChangesAsync();
            throw new BadRequestException("Invitation has expired.");
        }

        if (invitation.InviteeEmail != acceptedByUser.Email)
        {
            throw new ForbiddenException("This invitation was not sent to you.");
        }

        var userGroup = new UserGroup
        {
            Id = Guid.NewGuid(),
            GroupId = invitation.GroupId,
            UserId = acceptedByUserId,
        };

        invitation.Status = InvitationStatus.Accepted;
        invitation.AcceptedByUserId = acceptedByUserId;

        _db.UserGroups.Add(userGroup);
        await _db.SaveChangesAsync();

        _logger.LogInformation(
            "Invitation {InvitationId} accepted by {UserId}, added into group {GroupId}",
            invitation.Id,
            acceptedByUserId,
            userGroup.GroupId
        );

        return new AcceptInvitationResponseDto { GroupId = userGroup.GroupId };
    }

    private static string HashToken(string rawToken)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawToken));
        return Convert.ToBase64String(bytes);
    }
}
