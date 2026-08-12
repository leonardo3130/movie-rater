using MovieRaterApi.Features.Groups.DTOs;

namespace MovieRaterApi.Features.Groups.Interfaces;

public interface IInvitationService
{
    Task<InvitationResponseDto> InviteAsync(Guid inviterUserId, InvitationRequestDto request);
    Task<AcceptInvitationResponseDto> AcceptInvitationAsync(
        Guid acceptedByUserId,
        AcceptInvitationRequestDto request
    );
}
