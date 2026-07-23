using MovieRaterApi.Features.Authentication.DTOs;

namespace MovieRaterApi.Features.Authentication.Interfaces;

public interface ICoupleInvitationService
{
    Task<InviteResponseDto> InviteAsync(Guid inviterUserId, InvitePartnerRequestDto request);
    Task AcceptInvitationAsync(Guid acceptedByUserId, AcceptInvitationRequestDto request);
}