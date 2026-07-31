using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using MovieRaterApi.Data;
using MovieRaterApi.Data.Entities;
using MovieRaterApi.Features.Authentication.DTOs;
using MovieRaterApi.Features.Authentication.Interfaces;
using MovieRaterApi.Infrastructure.Exceptions;

namespace MovieRaterApi.Features.Authentication.Services;

public class CoupleInvitationService : ICoupleInvitationService
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<CoupleInvitationService> _logger;

    public CoupleInvitationService(ApplicationDbContext db, ILogger<CoupleInvitationService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<InviteResponseDto> InviteAsync(
        Guid inviterUserId,
        InvitePartnerRequestDto request
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

        var existingCouple = await _db.Couples.AnyAsync(c =>
            (c.User1Id == inviterUserId || c.User2Id == invitee.Id)
            || (c.User2Id == inviterUserId || c.User1Id == invitee.Id)
        );
        if (existingCouple)
        {
            throw new ConflictException("You or the other user is already in a couple");
        }

        var existingInvitation = await _db.Set<CoupleInvitation>()
            .FirstOrDefaultAsync(ci =>
                ci.InviterUserId == inviterUserId
                && ci.InviteeEmail == request.InviteeEmail
                && ci.Status == InvitationStatus.Pending
            );
        if (existingInvitation is not null)
        {
            throw new ConflictException("A pending invitation already exists for this user.");
        }

        var rawToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        var tokenHash = HashToken(rawToken);

        var invitation = new CoupleInvitation
        {
            Id = Guid.NewGuid(),
            InviterUserId = inviterUserId,
            InviteeEmail = request.InviteeEmail,
            InviteTokenHash = tokenHash,
            Status = InvitationStatus.Pending,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow,
        };

        _db.Set<CoupleInvitation>().Add(invitation);
        await _db.SaveChangesAsync();

        _logger.LogInformation(
            "Invitation {InvitationId} created by {InviterUserId} for {InviteeEmail}",
            invitation.Id,
            inviterUserId,
            request.InviteeEmail
        );

        var encoded = Uri.EscapeDataString(rawToken);

        return new InviteResponseDto
        {
            InvitationId = invitation.Id,
            InviteToken = encoded,
            ExpiresAt = invitation.ExpiresAt,
        };
    }

    public async Task AcceptInvitationAsync(
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

        var invitation = await _db.Set<CoupleInvitation>()
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

        var couple = new Couple
        {
            Id = Guid.NewGuid(),
            User1Id = invitation.InviterUserId,
            User2Id = acceptedByUserId,
            CreatedAt = DateTime.UtcNow,
        };

        invitation.Status = InvitationStatus.Accepted;
        invitation.AcceptedByUserId = acceptedByUserId;

        _db.Couples.Add(couple);
        await _db.SaveChangesAsync();

        _logger.LogInformation(
            "Invitation {InvitationId} accepted by {UserId}, couple {CoupleId} created",
            invitation.Id,
            acceptedByUserId,
            couple.Id
        );
    }

    private static string HashToken(string rawToken)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawToken));
        return Convert.ToBase64String(bytes);
    }
}
